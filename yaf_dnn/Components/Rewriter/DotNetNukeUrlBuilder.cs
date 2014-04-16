/* Yet Another Forum.NET
 * Copyright (C) 2003-2005 Bjørnar Henden
 * Copyright (C) 2006-2013 Jaben Cargman
 * Copyright (C) 2014 Ingo Herbote
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

namespace YAF.DotNetNuke
{
    #region Using

    using System;
    using System.Text;

    using global::DotNetNuke.Common;
    using global::DotNetNuke.Entities.Portals;

    using global::DotNetNuke.Entities.Tabs;
    using global::DotNetNuke.Services.Url.FriendlyUrl;

    using YAF.Classes;
    using YAF.Core;
    using YAF.Core.Helpers;
    using YAF.Core.URLBuilder;
    using YAF.Types.Constants;
    using YAF.Types.Extensions;
    using YAF.Types.Interfaces;
    using YAF.Utils;

    #endregion

    /// <summary>
    /// The DotNetNuke URL builder.
    /// </summary>
    public class DotNetNukeUrlBuilder : StandardUrlRewriter
    {
        #region Public Methods

        /// <summary>
        /// Builds the Full URL.
        /// </summary>
        /// <param name="url">
        /// The url.
        /// </param>
        /// <returns>
        /// Returns the URL.
        /// </returns>
        public override string BuildUrlFull(string url)
        {
            return this.BuildUrl(YafContext.Current.Get<YafBoardSettings>(), url);
        }

        /// <summary>
        /// Builds the Full URL.
        /// </summary>
        /// <param name="boardSettings">The board settings.</param>
        /// <param name="url">The url.</param>
        /// <returns>
        /// Returns the URL.
        /// </returns>
        public override string BuildUrlFull(object boardSettings, string url)
        {
            return this.BuildUrl(boardSettings, url);
        }

        /// <summary>
        /// The build url.
        /// </summary>
        /// <param name="url">The url.</param>
        /// <returns>
        /// The new Url.
        /// </returns>
        public override string BuildUrl(string url)
        {
            return this.BuildUrl(YafContext.Current.Get<YafBoardSettings>(), url);
        }

        /// <summary>
        /// The build url.
        /// </summary>
        /// <param name="boardSettings">The board settings.</param>
        /// <param name="url">The url.</param>
        /// <returns>
        /// The new Url.
        /// </returns>
        public override string BuildUrl(object boardSettings, string url)
        {
            var yafBoardSettings = boardSettings.ToType<YafBoardSettings>();

            var yafTab = new TabController().GetTab(yafBoardSettings.DNNPageTab);

            if (url.IsNotSet())
            {
                // return BaseURL
                var baseUrl = Globals.NavigateURL();

                if (baseUrl.EndsWith(yafTab.TabName))
                {
                    baseUrl = baseUrl.Replace(yafTab.TabName, string.Empty);
                }

                if (baseUrl.EndsWith("{0}.aspx".FormatWith(yafTab.TabName)))
                {
                    baseUrl = baseUrl.Replace("{0}.aspx".FormatWith(yafTab.TabName), string.Empty);
                }

                if (baseUrl.EndsWith("{0}.aspx".FormatWith(yafTab.TabName.ToLower())))
                {
                    baseUrl = baseUrl.Replace("{0}.aspx".FormatWith(yafTab.TabName.ToLower()), string.Empty);
                }

                return baseUrl;
            }

            if (!Config.EnableURLRewriting)
            {
                return Globals.ResolveUrl("{0}&{1}".FormatWith(Globals.ApplicationURL(yafTab.TabID), url));
            }

            var newUrl = new StringBuilder();

            var portalSettings = new PortalSettings(yafTab.PortalID);

            /*var portalSettings = PortalController.GetCurrentPortalSettings();

            var yafTab = new TabController().GetTab(
                yafBoardSettings.DNNPageTab,
                portalSettings.PortalId,
                false);*/

            var boardNameOrPageName = UrlRewriteHelper.CleanStringForURL(yafBoardSettings.Name);

            var parser = new SimpleURLParameterParser(url);

            switch (parser["g"].ToEnum<ForumPages>())
            {
                case ForumPages.topics:
                    {
                        boardNameOrPageName = UrlRewriteHelper.GetForumName(parser["f"].ToType<int>());
                    }

                    break;
                case ForumPages.posts:
                    {
                        if (parser["t"].IsSet())
                        {
                            var topicName = UrlRewriteHelper.GetTopicName(parser["t"].ToType<int>());

                            if (topicName.EndsWith("-"))
                            {
                                topicName = topicName.Remove(topicName.Length - 1, 1);
                            }

                            boardNameOrPageName = topicName;
                        }
                        else if (parser["m"].IsSet())
                        {
                            string topicName;

                            try
                            {
                                topicName = UrlRewriteHelper.GetTopicNameFromMessage(parser["m"].ToType<int>());

                                if (topicName.EndsWith("-"))
                                {
                                    topicName = topicName.Remove(topicName.Length - 1, 1);
                                }
                            }
                            catch (Exception)
                            {
                                topicName = parser["g"];
                            }

                            boardNameOrPageName = topicName;
                        }
                    }

                    break;
                case ForumPages.profile:
                    {
                        boardNameOrPageName =
                            UrlRewriteHelper.CleanStringForURL(
                                parser["name"].IsSet()
                                    ? parser["name"]
                                    : UrlRewriteHelper.GetProfileName(parser["u"].ToType<int>()));
                    }

                    break;
                case ForumPages.forum:
                    {
                        if (parser["c"].IsSet())
                        {
                            boardNameOrPageName = UrlRewriteHelper.GetCategoryName(parser["c"].ToType<int>());
                        }
                    }

                    break;
            }

            newUrl.Append(
                FriendlyUrlProvider.Instance()
                    .FriendlyUrl(
                        yafTab,
                        "{0}&{1}".FormatWith(Globals.ApplicationURL(yafTab.TabID), url),
                        boardNameOrPageName,
                        portalSettings.DefaultPortalAlias));

            // add anchor
            /*if (parser.HasAnchor)
            {
               newUrl.AppendFormat("#{0}", parser.Anchor);
            }*/

            return newUrl.Length >= 260
                       ? this.GetStandardUrl(yafTab, url, boardNameOrPageName, portalSettings)
                       : newUrl.ToString();
        }

        #endregion

        /// <summary>
        /// Gets the standard URL without any specific page names.
        /// </summary>
        /// <param name="activeTab">The active tab.</param>
        /// <param name="url">The URL.</param>
        /// <param name="boardNameOrPageName">Name of the board name original page.</param>
        /// <param name="portalSettings">The portal settings.</param>
        /// <returns>Returns the Normal URL</returns>
        private string GetStandardUrl(
            TabInfo activeTab,
            string url,
            string boardNameOrPageName,
            PortalSettings portalSettings)
        {
            return FriendlyUrlProvider.Instance()
                .FriendlyUrl(
                    activeTab,
                    "{0}&{1}".FormatWith(Globals.ApplicationURL(activeTab.TabID), url),
                    boardNameOrPageName,
                    portalSettings.DefaultPortalAlias);
        }
    }
}