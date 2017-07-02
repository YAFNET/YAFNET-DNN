/* Yet Another Forum.NET
 * Copyright (C) 2003-2005 Bjørnar Henden
 * Copyright (C) 2006-2013 Jaben Cargman
 * Copyright (C) 2014-2017 Ingo Herbote
 * http://www.yetanotherforum.net/
 *
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at

 * http://www.apache.org/licenses/LICENSE-2.0

 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 */

 namespace YAF.DotNetNuke.Components.Integration
{
    using System;
    using System.Collections.Generic;
    using System.Data;

    using global::DotNetNuke.Entities.Portals;
    using global::DotNetNuke.Services.Sitemap;

    using YAF.Classes.Data;
    using YAF.Types.Constants;
    using YAF.Utils;

    /// <summary>
    /// YAF.NET Forum SiteMap Provider
    /// </summary>
    /// <seealso cref="SitemapProvider" />
    public class Sitemap : SitemapProvider
    {
        #region Public Methods

        /// <summary>
        /// Get the Sitemap URLs.
        /// </summary>
        /// <param name="portalId">The portal id.</param>
        /// <param name="portalSettings">The portal settings.</param>
        /// <param name="version">The version.</param>
        /// <returns>
        /// The List with URLs.
        /// </returns>
        public override List<SitemapUrl> GetUrls(int portalId, PortalSettings portalSettings, string version)
        {
            var urls = new List<SitemapUrl>();

            var forumListDataTable = LegacyDb.forum_simplelist(1, 100);

            foreach (DataRow drow in forumListDataTable.Rows)
            {
                var pageUrl = new SitemapUrl
                                  {
                                      Url =
                                          YafBuildLink.GetLinkNotEscaped(
                                              ForumPages.topics,
                                              true,
                                              "f={0}",
                                              drow["ForumID"]),
                                      Priority = (float)0.8,
                                      LastModified = DateTime.Now,
                                      ChangeFrequency = SitemapChangeFrequency.Always
                                  };



                urls.Add(pageUrl);
            }

            return urls;
        }

        #endregion
    }
}