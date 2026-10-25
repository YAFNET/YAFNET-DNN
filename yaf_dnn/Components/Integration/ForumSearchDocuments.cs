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

namespace YAF.DotNetNuke.Components.Integration;

using System.Collections.Generic;

using global::DotNetNuke.Entities.Modules.Definitions;
using global::DotNetNuke.Services.Search.Entities;
using global::DotNetNuke.Services.Search.Internals;

/// <summary>
/// Creates the DNN search documents of the forum posts (the posts are indexed with the DNN search, DNN uses its own
/// Lucene.Net version).
/// </summary>
public static class ForumSearchDocuments
{
    /// <summary>
    /// The module name of the forum module.
    /// </summary>
    public const string ModuleName = "YetAnotherForumDotNet";

    /// <summary>
    /// The friendly name of the module definition of the forum module.
    /// </summary>
    public const string DefinitionName = "YAF";

    /// <summary>
    /// The module setting with the board of the forum module.
    /// </summary>
    public const string BoardIdSetting = "forumboardid";

    /// <summary>
    /// The numeric key of the board id.
    /// </summary>
    public const string BoardIdKey = "BoardId";

    /// <summary>
    /// The numeric key of the forum id.
    /// </summary>
    public const string ForumIdKey = "ForumId";

    /// <summary>
    /// The numeric key of the topic id.
    /// </summary>
    public const string TopicIdKey = "TopicId";

    /// <summary>
    /// The numeric key of the message id.
    /// </summary>
    public const string MessageIdKey = "MessageId";

    /// <summary>
    /// The numeric key of the (YAF) user id.
    /// </summary>
    public const string UserIdKey = "UserId";

    /// <summary>
    /// The keyword of the user name.
    /// </summary>
    public const string UserNameKey = "UserName";

    /// <summary>
    /// The keyword of the user display name.
    /// </summary>
    public const string UserDisplayNameKey = "UserDisplayName";

    /// <summary>
    /// Gets the search type id of the module search documents.
    /// </summary>
    public static int ModuleSearchTypeId => SearchHelper.Instance.GetSearchTypeByName("module").SearchTypeId;

    /// <summary>
    /// Gets the unique key of the search document of a message.
    /// </summary>
    /// <param name="messageId">The message id.</param>
    /// <returns>Returns the unique key.</returns>
    public static string GetUniqueKey(int messageId)
    {
        return $"yaf_message_{messageId}";
    }

    /// <summary>
    /// Gets the query string of the search documents of a topic (all documents of a topic can be deleted with it).
    /// </summary>
    /// <param name="topicId">The topic id.</param>
    /// <returns>Returns the query string.</returns>
    public static string GetTopicQueryString(int topicId)
    {
        return $"g={ForumPages.Posts}&t={topicId}";
    }

    /// <summary>
    /// Gets the board of a forum module.
    /// </summary>
    /// <param name="module">The module.</param>
    /// <returns>Returns the board id, or -1 if the module shows no board.</returns>
    public static int GetBoardId(ModuleInfo module)
    {
        var boardId = module.ModuleSettings[BoardIdSetting];

        return boardId?.ToString().IsSet() == true ? boardId.ToType<int>() : -1;
    }

    /// <summary>
    /// Gets all (not deleted) forum modules of all portals.
    /// </summary>
    /// <returns>Returns the forum modules.</returns>
    public static List<ModuleInfo> GetForumModules()
    {
        var moduleDefinition = ModuleDefinitionController.GetModuleDefinitionByFriendlyName(DefinitionName);

        if (moduleDefinition is null)
        {
            return [];
        }

        // a module on several pages is listed for every page
        return ModuleController.Instance.GetModulesByDesktopModuleId(moduleDefinition.DesktopModuleID)
            .Cast<ModuleInfo>().Where(module => !module.IsDeleted && module.ModuleDefID == moduleDefinition.ModuleDefID)
            .GroupBy(module => module.ModuleID).Select(group => group.First()).ToList();
    }

    /// <summary>
    /// Gets the forum modules that show a board.
    /// </summary>
    /// <param name="boardId">The board id.</param>
    /// <returns>Returns the forum modules.</returns>
    public static List<ModuleInfo> GetForumModules(int boardId)
    {
        return GetForumModules().FindAll(module => GetBoardId(module) == boardId);
    }

    /// <summary>
    /// Gets the DNN user id of a YAF user.
    /// </summary>
    /// <param name="yafUserId">The YAF user id.</param>
    /// <param name="cache">The users which were already looked up.</param>
    /// <returns>Returns the DNN user id, or 0 for guests and unknown users.</returns>
    public static int GetDnnUserId(int yafUserId, Dictionary<int, int> cache)
    {
        if (cache.TryGetValue(yafUserId, out var dnnUserId))
        {
            return dnnUserId;
        }

        var user = BoardContext.Current.GetRepository<User>().GetById(yafUserId);

        // the provider key of the YAF users is the id of the DNN user
        dnnUserId = user?.ProviderUserKey.IsSet() == true && int.TryParse(user.ProviderUserKey, out var id) ? id : 0;

        cache[yafUserId] = dnnUserId;

        return dnnUserId;
    }

    /// <summary>
    /// Creates the search document of a message.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <param name="module">The forum module.</param>
    /// <param name="boardId">The board id.</param>
    /// <param name="authorUserId">The DNN user id of the author.</param>
    /// <returns>Returns the search document.</returns>
    public static SearchDocument CreateDocument(
        SearchMessage message,
        ModuleInfo module,
        int boardId,
        int authorUserId)
    {
        var flags = new MessageFlags(message.Flags);

        // hidden content (hide, hide-reply, hide-thanks, group-hide...) is only shown to some users, it is not
        // indexed: otherwise every user could find posts by it and see it in the snippet of the result
        var text = BoardContext.Current.Get<IFormatMessage>().RemoveHiddenBBCodeContent(message.Message ?? string.Empty);

        var document = new SearchDocument
                           {
                               UniqueKey = GetUniqueKey(message.MessageId.Value),
                               SearchTypeId = ModuleSearchTypeId,
                               PortalId = module.PortalID,
                               ModuleId = module.ModuleID,
                               ModuleDefId = module.ModuleDefID,
                               CultureCode = module.CultureCode ?? string.Empty,
                               AuthorUserId = authorUserId,
                               QueryString = GetTopicQueryString(message.TopicId ?? 0),
                               Title = message.Topic ?? string.Empty,
                               Description = message.Description ?? string.Empty,
                               Body = HtmlTagHelper.StripHtml(BBCodeHelper.StripBBCode(text)),
                               ModifiedTimeUtc = GetPostedUtc(message.Posted),
                               Tags = message.TopicTags.IsSet()
                                          ? message.TopicTags.Split(',').Select(tag => tag.Trim())
                                              .Where(tag => tag.IsSet()).ToList()
                                          : [],

                               // posts that wait for approval (e.g. spam) or are deleted are removed from the index
                               IsActive = flags.IsApproved && !flags.IsDeleted
                           };

        document.NumericKeys.Add(BoardIdKey, boardId);
        document.NumericKeys.Add(ForumIdKey, message.ForumId ?? 0);
        document.NumericKeys.Add(TopicIdKey, message.TopicId ?? 0);
        document.NumericKeys.Add(MessageIdKey, message.MessageId.Value);
        document.NumericKeys.Add(UserIdKey, message.UserId);

        // the keywords are searched too (the author of a post)
        document.Keywords.Add(UserNameKey, message.UserName ?? string.Empty);
        document.Keywords.Add(UserDisplayNameKey, message.UserDisplayName ?? string.Empty);

        return document;
    }

    /// <summary>
    /// Parses the posted date of a message (utc).
    /// </summary>
    /// <param name="posted">The posted date.</param>
    /// <returns>Returns the posted date, or now if the date could not be parsed.</returns>
    private static DateTime GetPostedUtc(string posted)
    {
        return DateTime.TryParse(
                   posted,
                   CultureInfo.InvariantCulture,
                   DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                   out var postedUtc) && postedUtc > DateTime.MinValue
                   ? postedUtc
                   : DateTime.UtcNow;
    }
}
