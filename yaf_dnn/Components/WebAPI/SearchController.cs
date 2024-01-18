﻿/* Yet Another Forum.NET
 * Copyright (C) 2003-2005 Bjørnar Henden
 * Copyright (C) 2006-2013 Jaben Cargman
 * Copyright (C) 2014-2024 Ingo Herbote
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

namespace YAF.DotNetNuke.Components.WebAPI;

/// <summary>
/// The YAF Search controller.
/// </summary>
public class SearchController : DnnApiController, IHaveServiceLocator
{
    /// <summary>
    ///   Gets ServiceLocator.
    /// </summary>
    public IServiceLocator ServiceLocator => BoardContext.Current.ServiceLocator;

    /// <summary>
    /// Get similar topic titles
    /// </summary>
    /// <param name="searchTopic">
    /// The search Topic.
    /// </param>
    /// <returns>
    /// Returns the search Results.
    /// </returns>
    [HttpPost]
    [AllowAnonymous]
    public IHttpActionResult GetSimilarTitles([FromBody] SearchTopic searchTopic)
    {
        var results = this.Get<ISearch>().SearchSimilar(
            string.Empty,
            searchTopic.SearchTerm,
            "Topic");

        if (results is null)
        {
            return this.Ok(
                new SearchGridDataSet
                    {
                        PageNumber = 0,
                        TotalRecords = 0,
                        PageSize = 0
                    });
        }

        return this.Ok(
            new SearchGridDataSet
                {
                    PageNumber = 1, TotalRecords = results.Count, PageSize = 20, SearchResults = results
                });
    }

    /// <summary>
    /// Gets the search results.
    /// </summary>
    /// <param name="searchTopic">
    /// The search Topic.
    /// </param>
    /// <returns>
    /// Returns the search Results.
    /// </returns>
    [HttpPost]
    [AllowAnonymous]
    public IHttpActionResult GetSearchResults([FromBody] SearchTopic searchTopic)
    {
        var results = this.Get<ISearch>().SearchPaged(
            out var totalHits,
            searchTopic.ForumId,
            searchTopic.SearchTerm,
            searchTopic.Page,
            searchTopic.PageSize);

        return this.Ok(
            new SearchGridDataSet
                {
                    PageNumber = searchTopic.Page,
                    TotalRecords = totalHits,
                    PageSize = searchTopic.PageSize,
                    SearchResults = results
                });
    }
}