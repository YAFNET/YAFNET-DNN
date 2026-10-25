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

using global::DotNetNuke.Common.Extensions;
using global::DotNetNuke.Services.Search.Controllers;
using global::DotNetNuke.Services.Search.Entities;
using global::DotNetNuke.Services.Search.Internals;

/// <summary>
/// The YAF search inside DNN: the posts are indexed and searched with the DNN search instead of the YAF Lucene
/// index (DNN uses its own Lucene.Net version).
/// </summary>
/// <seealso cref="ISearch" />
public class DnnSearch : ISearch, IHaveServiceLocator
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DnnSearch"/> class.
    /// </summary>
    /// <param name="serviceLocator">The service locator.</param>
    public DnnSearch(IServiceLocator serviceLocator)
    {
        this.ServiceLocator = serviceLocator;
    }

    /// <summary>
    /// Gets the ServiceLocator.
    /// </summary>
    public IServiceLocator ServiceLocator { get; }

    /// <summary>
    /// Commits the changes of the DNN search index.
    /// </summary>
    public void Optimize()
    {
        InternalSearchController.Instance.Commit();
    }

    /// <summary>
    /// Clears the search Index (the documents of all forum modules).
    /// </summary>
    /// <returns>
    /// Returns if clearing was successful
    /// </returns>
    public bool ClearSearchIndex()
    {
        try
        {
            ForumSearchDocuments.GetForumModules().ForEach(
                module => InternalSearchController.Instance.DeleteSearchDocumentsByModule(
                    module.PortalID,
                    module.ModuleID,
                    module.ModuleDefID));

            this.Optimize();
        }
        catch (Exception exception)
        {
            this.Get<ILoggerService>().Error(exception, "Error clearing the DNN search index");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Delete Search Index Record by Message Id.
    /// </summary>
    /// <param name="messageId">
    /// The message id.
    /// </param>
    public void DeleteSearchIndexRecordByMessageId(int messageId)
    {
        this.DeleteDocuments(
            new SearchDocument { UniqueKey = ForumSearchDocuments.GetUniqueKey(messageId) });
    }

    /// <summary>
    /// Delete Search Index Record by Topic Id.
    /// </summary>
    /// <param name="topicId">
    /// The topic Id.
    /// </param>
    public void DeleteSearchIndexRecordByTopicId(int topicId)
    {
        this.DeleteDocuments(
            new SearchDocument { QueryString = ForumSearchDocuments.GetTopicQueryString(topicId) });
    }

    /// <summary>
    /// Adds the search index
    /// </summary>
    /// <param name="messageList">
    /// The message list.
    /// </param>
    /// <returns>
    /// The <see cref="Task"/>.
    /// </returns>
    public Task AddSearchIndexAsync(IEnumerable<SearchMessage> messageList)
    {
        this.IndexMessages(messageList);

        return Task.CompletedTask;
    }

    /// <summary>
    /// The add search index item.
    /// </summary>
    /// <param name="message">
    /// The message.
    /// </param>
    public void AddSearchIndexItem(SearchMessage message)
    {
        this.IndexMessages([message]);
    }

    /// <summary>
    /// Updates the Search Index Item or if not found adds it.
    /// </summary>
    /// <param name="message">
    /// The message.
    /// </param>
    /// <param name="dispose">
    /// Not used, the changes are always committed.
    /// </param>
    public void UpdateSearchIndexItem(SearchMessage message, bool dispose = false)
    {
        // the document with the same key is replaced
        this.IndexMessages([message]);
    }

    /// <summary>
    /// Only Get Number of Search Results (Hits)
    /// </summary>
    /// <param name="input">
    /// The input.
    /// </param>
    /// <returns>
    /// The <see cref="int"/>.
    /// </returns>
    public int CountHits(string input)
    {
        return input.IsNotSet() ? 0 : this.Search(0, input, 0, 1)?.TotalHits ?? 0;
    }

    /// <summary>
    /// Searches for similar words
    /// </summary>
    /// <param name="filter">
    /// The filter (the id of the topic which is not returned).
    /// </param>
    /// <param name="input">
    /// The input.
    /// </param>
    /// <param name="fieldName">
    /// Name of the field (the DNN search always searches all fields).
    /// </param>
    /// <returns>
    /// Returns the list of search results.
    /// </returns>
    public List<SearchMessage> SearchSimilar(string filter, string input, string fieldName = "")
    {
        if (input.IsNotSet())
        {
            return [];
        }

        var results = this.Search(0, input, 0, this.GetHitsLimit(20));

        if (results is null)
        {
            return [];
        }

        var excludedTopicId = filter.IsSet() ? filter.ToType<int>() : 0;

        return results.Results.Where(result => GetNumericKey(result, ForumSearchDocuments.TopicIdKey) != excludedTopicId)
            .GroupBy(result => GetNumericKey(result, ForumSearchDocuments.TopicIdKey))
            .Select(group => this.MapResult(group.First(), false)).ToList();
    }

    /// <summary>
    /// Searches the paged.
    /// </summary>
    /// <param name="totalHits">The total hits.</param>
    /// <param name="forumId">The forum identifier.</param>
    /// <param name="input">The input.</param>
    /// <param name="pageIndex">Index of the page.</param>
    /// <param name="pageSize">Size of the page.</param>
    /// <param name="fieldName">Name of the field (the DNN search always searches all fields).</param>
    /// <returns>
    /// Returns the search results
    /// </returns>
    public List<SearchMessage> SearchPaged(
        out int totalHits,
        int forumId,
        string input,
        int pageIndex,
        int pageSize,
        string fieldName = "")
    {
        totalHits = 0;

        if (input.IsNotSet())
        {
            return [];
        }

        var results = this.Search(forumId, input, pageIndex, pageSize);

        if (results is null)
        {
            return [];
        }

        totalHits = results.TotalHits;

        return [.. results.Results.Select(result => this.MapResult(result, true))];
    }

    /// <summary>
    /// Searches the default.
    /// </summary>
    /// <param name="forumId">The forum identifier.</param>
    /// <param name="input">The input.</param>
    /// <param name="fieldName">Name of the field (the DNN search always searches all fields).</param>
    /// <returns>
    /// Returns the search results
    /// </returns>
    public List<SearchMessage> SearchDefault(int forumId, string input, string fieldName = "")
    {
        return this.SearchPaged(out _, forumId, input, 0, this.GetHitsLimit(1000), fieldName);
    }

    /// <summary>
    /// Gets a numeric key of a search result.
    /// </summary>
    /// <param name="result">The search result.</param>
    /// <param name="key">The key.</param>
    /// <returns>Returns the value, or 0 if the result has no such key.</returns>
    private static int GetNumericKey(SearchResult result, string key)
    {
        return result.NumericKeys.TryGetValue(key, out var value) ? value : 0;
    }

    /// <summary>
    /// Gets a keyword of a search result.
    /// </summary>
    /// <param name="result">The search result.</param>
    /// <param name="key">The key.</param>
    /// <returns>Returns the value, or an empty string if the result has no such keyword.</returns>
    private static string GetKeyword(SearchResult result, string key)
    {
        return result.Keywords.TryGetValue(key, out var value) ? value : string.Empty;
    }

    /// <summary>
    /// Gets the maximum number of the results.
    /// </summary>
    /// <param name="defaultLimit">The limit if the board has no limit.</param>
    /// <returns>Returns the limit.</returns>
    private int GetHitsLimit(int defaultLimit)
    {
        var hitsLimit = this.Get<BoardSettings>().ReturnSearchMax;

        return hitsLimit > 0 ? hitsLimit : defaultLimit;
    }

    /// <summary>
    /// Indexes the messages in all forum modules of the current board.
    /// </summary>
    /// <param name="messageList">The message list.</param>
    private void IndexMessages(IEnumerable<SearchMessage> messageList)
    {
        try
        {
            var boardId = BoardContext.Current.PageBoardID;
            var modules = ForumSearchDocuments.GetForumModules(boardId);

            if (modules.Count == 0)
            {
                return;
            }

            var users = new Dictionary<int, int>();

            var documents = messageList.Where(message => message.MessageId.HasValue).SelectMany(
                message => modules.Select(
                    module => ForumSearchDocuments.CreateDocument(
                        message,
                        module,
                        boardId,
                        ForumSearchDocuments.GetDnnUserId(message.UserId, users)))).ToList();

            // the documents with the same key are replaced, inactive documents are only removed
            InternalSearchController.Instance.AddSearchDocuments(documents);

            this.Optimize();
        }
        catch (Exception exception)
        {
            this.Get<ILoggerService>().Error(exception, "Error updating the DNN search index");
        }
    }

    /// <summary>
    /// Deletes the documents of all forum modules.
    /// </summary>
    /// <param name="document">The document with the key or the query string of the documents.</param>
    private void DeleteDocuments(SearchDocument document)
    {
        try
        {
            document.SearchTypeId = ForumSearchDocuments.ModuleSearchTypeId;

            foreach (var moduleDefId in ForumSearchDocuments.GetForumModules().Select(module => module.ModuleDefID)
                         .Distinct())
            {
                document.ModuleDefId = moduleDefId;

                InternalSearchController.Instance.DeleteSearchDocument(document);
            }

            this.Optimize();
        }
        catch (Exception exception)
        {
            this.Get<ILoggerService>().Error(exception, "Error deleting from the DNN search index");
        }
    }

    /// <summary>
    /// Searches the posts of the current board with the DNN search.
    /// </summary>
    /// <param name="forumId">The forum id (0 for all forums).</param>
    /// <param name="input">The input.</param>
    /// <param name="pageIndex">The page index (0 based).</param>
    /// <param name="pageSize">The page size.</param>
    /// <returns>Returns the search results, or null if the board is not shown by a forum module.</returns>
    private SearchResults Search(int forumId, string input, int pageIndex, int pageSize)
    {
        try
        {
            var boardId = BoardContext.Current.PageBoardID;

            var moduleDefIds = ForumSearchDocuments.GetForumModules(boardId).Select(module => module.ModuleDefID)
                .Distinct().ToList();

            if (moduleDefIds.Count == 0)
            {
                return null;
            }

            var query = new SearchQuery
                            {
                                KeyWords = input.Trim(),
                                ModuleDefIds = moduleDefIds,
                                PageIndex = pageIndex + 1,
                                PageSize = pageSize > 0 ? pageSize : 20,
                                SortField = SortFields.LastModified,
                                SortDirection = SortDirections.Descending,
                                BodySnippetLength = 300,
                                TitleSnippetLength = 120,
                                WildCardSearch = true
                            };

            query.NumericKeys.Add(ForumSearchDocuments.BoardIdKey, boardId);

            if (forumId > 0)
            {
                query.NumericKeys.Add(ForumSearchDocuments.ForumIdKey, forumId);
            }

            // the search controller of the request
            var searchController = HttpContext.Current?.GetScope()?.ServiceProvider.GetService<ISearchController>();

            // the results are checked by the forum access of the user (UpgradeController.HasViewPermission)
            return searchController?.ModuleSearch(query);
        }
        catch (Exception exception)
        {
            this.Get<ILoggerService>().Error(exception, "Search Error");

            return null;
        }
    }

    /// <summary>
    /// Maps a DNN search result to a search message.
    /// </summary>
    /// <param name="result">The search result.</param>
    /// <param name="withMessage">Format the message (snippet) and date of the result.</param>
    /// <returns>Returns the search message.</returns>
    private SearchMessage MapResult(SearchResult result, bool withMessage)
    {
        var forumId = GetNumericKey(result, ForumSearchDocuments.ForumIdKey);
        var topicId = GetNumericKey(result, ForumSearchDocuments.TopicIdKey);
        var messageId = GetNumericKey(result, ForumSearchDocuments.MessageIdKey);

        var forumName = this.GetRepository<Forum>().GetById(forumId)?.Name ?? string.Empty;

        // the results are shown as html (also by javascript): the texts are encoded
        var message = new SearchMessage
                          {
                              MessageId = messageId,
                              TopicId = topicId,
                              Topic = HttpUtility.HtmlEncode(result.Title),
                              TopicUrl = this.Get<LinkBuilder>().GetTopicLink(topicId, result.Title),
                              ForumId = forumId,
                              ForumName = HttpUtility.HtmlEncode(forumName),
                              ForumUrl = this.Get<LinkBuilder>().GetForumLink(forumId, forumName),
                              UserId = GetNumericKey(result, ForumSearchDocuments.UserIdKey),
                              UserName = HttpUtility.HtmlEncode(GetKeyword(result, ForumSearchDocuments.UserNameKey)),
                              UserDisplayName = HttpUtility.HtmlEncode(
                                  GetKeyword(result, ForumSearchDocuments.UserDisplayNameKey)),
                              Posted = result.ModifiedTimeUtc.ToString(
                                  "yyyy-MM-ddTHH:mm:ssZ",
                                  CultureInfo.InvariantCulture)
                          };

        if (!withMessage)
        {
            return message;
        }

        // the snippet of DNN is the highlighted part of the post (the post is indexed without bbcode and html)
        message.Message = result.Snippet;
        message.Description = HttpUtility.HtmlEncode(result.Description);
        message.TopicTags = HttpUtility.HtmlEncode(result.Tags.ToDelimitedString(","));
        message.MessageUrl = this.Get<LinkBuilder>().GetLink(
            ForumPages.Posts,
            new { m = messageId, name = result.Title });

        try
        {
            message.Posted = this.Get<BoardSettings>().ShowRelativeTime
                                 ? result.ModifiedTimeUtc.ToRelativeTime()
                                 : this.Get<IDateTimeService>().Format(DateTimeFormat.BothTopic, result.ModifiedTimeUtc);
        }
        catch (Exception)
        {
            // keep the date
        }

        return message;
    }
}
