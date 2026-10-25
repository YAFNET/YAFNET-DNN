/* Yet Another Forum.NET
 * Copyright (C) 2003-2005 Bjørnar Henden
 * Copyright (C) 2006-2013 Jaben Cargman
 * Copyright (C) 2014-2026 Ingo Herbote
 * https://www.yetanotherforum.net/
 *
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at

 * https://www.apache.org/licenses/LICENSE-2.0

 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 */

namespace YAF.DotNetNuke.Components.Controllers;

using System.Collections.Generic;

using global::DotNetNuke.Services.Search.Controllers;
using global::DotNetNuke.Services.Search.Entities;

using YAF.Core.Services.Import;
using YAF.DotNetNuke.Components.Integration;
using YAF.Types.EventProxies;
using YAF.Types.Interfaces.Events;

/// <summary>
/// The upgrade controller, and the search controller of the forum module (the posts are indexed with the DNN search).
/// </summary>
public class UpgradeController : ModuleSearchBase, IUpgradeable, IModuleSearchResultController, IHaveServiceLocator
{
    /// <summary>
    ///     Gets or sets the service locator.
    /// </summary>
    public IServiceLocator ServiceLocator => BoardContext.Current.ServiceLocator;

    /// <summary>
    ///     The BBCode extensions import xml file.
    /// </summary>
    private const string BbcodeImport = "~/DesktopModules/YetAnotherForumDotNet/Install/BBCodeExtensions.xml";

    /// <summary>
    ///     The Spam Words list import xml file.
    /// </summary>
    private const string SpamWordsImport = "~/DesktopModules/YetAnotherForumDotNet/Install/SpamWords.xml";

    /// <summary>
    /// Upgrades the module.
    /// </summary>
    /// <param name="version">The version.</param>
    /// <returns>Returns nothing</returns>
    public string UpgradeModule(string version)
    {
        var versionType = this.GetRepository<Registry>().ValidateVersion(BoardInfo.AppVersion);

        switch (versionType)
        {
            case DbVersionType.Upgrade:
                // Run Auto Upgrade
                this.Get<UpgradeService>().Upgrade();
                this.AddOrUpdateExtensions();
                this.UpdateProviderKeys();
                return string.Empty;
            case DbVersionType.NewInstall:
                this.Get<InstallService>().InitializeDatabase();
                this.AddOrUpdateExtensions();
                return string.Empty;
            case DbVersionType.Current:
                return string.Empty;
            default:
                return string.Empty;
        }
    }

    /// <summary>
    /// Gets the search documents of the posts which were added or changed since the last indexing
    /// (all posts when the DNN search index is rebuilt).
    /// </summary>
    /// <param name="moduleInfo">The module.</param>
    /// <param name="beginDateUtc">The date of the last indexing.</param>
    /// <returns>Returns the search documents.</returns>
    public override IList<SearchDocument> GetModifiedSearchDocuments(ModuleInfo moduleInfo, DateTime beginDateUtc)
    {
        var documents = new List<SearchDocument>();

        // the controller is also the business controller of the what's new module
        if (moduleInfo.DesktopModule?.ModuleName != ForumSearchDocuments.ModuleName)
        {
            return documents;
        }

        var boardId = ForumSearchDocuments.GetBoardId(moduleInfo);

        if (boardId < 0)
        {
            return documents;
        }

        // all posts on the first indexing or a rebuild of the index
        DateTime? changedSince = beginDateUtc.Year > 1900 ? beginDateUtc : null;

        var forums = this.GetRepository<Forum>().ListAll(boardId).Select(forum => forum.Item2);
        var topicTags = this.GetRepository<TopicTag>().ListAll();
        var users = new Dictionary<int, int>();

        foreach (var forum in forums.Where(
                     forum => !changedSince.HasValue
                              || forum.LastPosted.HasValue && forum.LastPosted.Value > changedSince.Value))
        {
            documents.AddRange(
                this.GetRepository<Message>().GetAllSearchMessagesByForum(forum.ID, topicTags, changedSince)
                    .Select(
                        message => ForumSearchDocuments.CreateDocument(
                            message,
                            moduleInfo,
                            boardId,
                            ForumSearchDocuments.GetDnnUserId(message.UserId, users))));
        }

        return documents;
    }

    /// <summary>
    /// Checks if the current user can read the forum of the post.
    /// </summary>
    /// <param name="searchResult">The search result.</param>
    /// <returns>Returns if the user can see the result.</returns>
    public bool HasViewPermission(SearchResult searchResult)
    {
        // not a post (e.g. the title of the module)
        if (!searchResult.NumericKeys.TryGetValue(ForumSearchDocuments.BoardIdKey, out var boardId)
            || !searchResult.NumericKeys.TryGetValue(ForumSearchDocuments.ForumIdKey, out var forumId))
        {
            return true;
        }

        return this.GetUserAccess(boardId).Exists(access => access.ForumID == forumId && access.ReadAccess > 0);
    }

    /// <summary>
    /// Gets the url of the post.
    /// </summary>
    /// <param name="searchResult">The search result.</param>
    /// <returns>Returns the url.</returns>
    public string GetDocUrl(SearchResult searchResult)
    {
        // not a post, the url of the page
        if (!searchResult.NumericKeys.TryGetValue(ForumSearchDocuments.BoardIdKey, out var boardId)
            || !searchResult.NumericKeys.TryGetValue(ForumSearchDocuments.MessageIdKey, out var messageId))
        {
            return string.Empty;
        }

        // the url to the page of the board (not the current board of the request)
        var boardSettings = this.Get<BoardSettingsService>().LoadBoardSettings(boardId, null);

        var url = $"g={ForumPages.Posts}&m={messageId}&name={UrlRewriteHelper.CleanStringForURL(searchResult.Title)}";

        return this.Get<IUrlBuilder>() is BaseUrlBuilder urlBuilder
                   ? urlBuilder.BuildUrl(boardSettings, url)
                   : this.Get<IUrlBuilder>().BuildUrl(url);
    }

    /// <summary>
    /// Gets the forum access of the current DNN user to a board (cached for the request).
    /// </summary>
    /// <param name="boardId">The board id.</param>
    /// <returns>Returns the forum access.</returns>
    private List<VAccess> GetUserAccess(int boardId)
    {
        var dnnUser = UserController.Instance.GetCurrentUserInfo();

        var cacheKey = $"YafSearchAccess_{boardId}_{dnnUser?.UserID ?? -1}";

        if (HttpContext.Current?.Items[cacheKey] is List<VAccess> cachedAccess)
        {
            return cachedAccess;
        }

        var yafUser = dnnUser is { UserID: > 0 }
                          ? this.GetRepository<User>().GetUserByProviderKey(boardId, dnnUser.UserID.ToString())
                          : null;

        // users which were not imported yet can see the posts the guests can see
        yafUser ??= this.GetRepository<User>().GetSingle(u => u.BoardID == boardId && (u.Flags & 4) == 4);

        var userAccess = yafUser is null
                             ? []
                             : this.GetRepository<VAccess>().Get(access => access.UserID == yafUser.ID);

        if (HttpContext.Current != null)
        {
            HttpContext.Current.Items[cacheKey] = userAccess;
        }

        return userAccess;
    }

    private void UpdateProviderKeys()
    {
        var prevVersion = this.GetRepository<Registry>().GetDbVersion();

        if (prevVersion > 87)
        {
            return;
        }

        var dnnUsers = UserController.GetUsers(PortalController.Instance.GetCurrentSettings().PortalId);

        if (dnnUsers == null)
        {
            return;
        }

        foreach (UserInfo user in dnnUsers)
        {
            // Migrate from Yaf < 3
            var yafUser = this.GetRepository<User>()
                .GetSingle(u => u.Name == user.Username && u.Email == user.Email);

            if (yafUser != null)
            {
                // update provider Key
                this.GetRepository<User>().UpdateOnly(
                    () => new User { ProviderUserKey = user.UserID.ToString() },
                    u => u.ID == yafUser.ID);
            }
        }
    }

    /// <summary>
    ///    Add or Update BBCode Extensions and Spam Words
    /// </summary>
    private void AddOrUpdateExtensions()
    {
        var loadWrapper = new Action<string, Action<Stream>>(
            (file, streamAction) =>
                {
                    var fullFile = this.Get<HttpRequestBase>().MapPath(file);

                    if (!File.Exists(fullFile))
                    {
                        return;
                    }

                    // import into board...
                    using var stream = new StreamReader(fullFile);
                    streamAction(stream.BaseStream);
                    stream.Close();
                });

        // get all boards...
        var boardIds = this.GetRepository<Board>().GetAll().Select(x => x.ID);

        // Upgrade all Boards
        boardIds.ForEach(
            boardId =>
                {
                    this.Get<IRaiseEvent>().Raise(new ImportStaticDataEvent(boardId));

                    // load default bbcode if available...
                    loadWrapper(BbcodeImport, s => DataImport.BBCodeExtensionImport(boardId, s));

                    // load default spam word if available...
                    loadWrapper(SpamWordsImport, s => DataImport.SpamWordsImport(boardId, s));
                });
    }
}