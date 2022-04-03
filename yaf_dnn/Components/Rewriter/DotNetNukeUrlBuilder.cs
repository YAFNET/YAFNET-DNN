/* Yet Another Forum.NET
 * Copyright (C) 2003-2005 Bjørnar Henden
 * Copyright (C) 2006-2013 Jaben Cargman
 * Copyright (C) 2014-2022 Ingo Herbote
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

namespace YAF.DotNetNuke
{
    #region Using

    using System;
    using System.Text;

    using global::DotNetNuke.Abstractions.Portals;
    using global::DotNetNuke.Common;
    using global::DotNetNuke.Entities.Portals;

    using global::DotNetNuke.Entities.Tabs;
    using global::DotNetNuke.Entities.Users;
    using global::DotNetNuke.Services.Localization;
    using global::DotNetNuke.Services.Url.FriendlyUrl;

    using YAF.Configuration;
    using YAF.Core.Context;
    using YAF.Core.Helpers;
    using YAF.Core.URLBuilder;
    using YAF.Core.Utilities;
    using YAF.Types;
    using YAF.Types.Attributes;
    using YAF.Types.Constants;
    using YAF.Types.Extensions;
    using YAF.Types.Interfaces;
    using YAF.Types.Interfaces.Identity;

    #endregion

    /// <summary>
    /// The DotNetNuke URL builder.
    /// </summary>
    [ExportService(ServiceLifetimeScope.Singleton)]
    public class DotNetNukeUrlBuilder : BaseUrlBuilder
    {
        #region Public Methods

        /// <summary>
        /// Builds the Full URL.
        /// </summary>
        /// <param name="url">
        /// The Complete.
        /// </param>
        /// <returns>
        /// Returns the URL.
        /// </returns>
        public override string BuildUrlFull([CanBeNull] string url)
        {
            return BuildUrlComplete(BoardContext.Current.Get<BoardSettings>(), url, true);
        }

        /// <summary>
        /// The build Complete.
        /// </summary>
        /// <param name="url">The Complete.</param>
        /// <returns>
        /// The new Complete.
        /// </returns>
        public override string BuildUrl([CanBeNull] string url)
        {
            return BuildUrlComplete(BoardContext.Current.Get<BoardSettings>(), url, false);
        }

        /// <summary>
        /// Builds Full URL for calling page with parameter URL as page's escaped parameter.
        /// </summary>
        /// <param name="boardSettings">The board settings.</param>
        /// <param name="url">URL to put into parameter.</param>
        /// <returns>
        /// URL to calling page with URL argument as page's parameter with escaped characters to make it valid parameter.
        /// </returns>
        public override string BuildUrl([NotNull] object boardSettings, [CanBeNull] string url)
        {
            return BuildUrlComplete(boardSettings, url, false);
        }

        /// <summary>
        /// Builds the URL complete.
        /// </summary>
        /// <param name="boardSettings">The board settings.</param>
        /// <param name="url">The URL.</param>
        /// <param name="fullUrl">if set to <c>true</c> [full URL].</param>
        /// <returns>
        /// The new URL.
        /// </returns>
        private static string BuildUrlComplete([NotNull] object boardSettings, [CanBeNull] string url, bool fullUrl)
        {
            CodeContracts.VerifyNotNull(boardSettings);

            var yafBoardSettings = boardSettings.ToType<BoardSettings>();

            var yafTab = new TabController().GetTab(yafBoardSettings.DNNPageTab, yafBoardSettings.DNNPortalId, true);

            var portalSettings = BoardContext.Current.Get<IPortalController>().GetCurrentSettings();

            CodeContracts.VerifyNotNull(portalSettings);

            if (portalSettings.ContentLocalizationEnabled)
            {
                yafTab = new TabController().GetTabByCulture(
                    yafBoardSettings.DNNPageTab,
                    yafBoardSettings.DNNPortalId,
                    new LocaleController().GetCurrentLocale(yafBoardSettings.DNNPortalId));
            }

            if (url.IsNotSet())
            {
                return GetBaseUrl(yafTab);
            }

            if (!Configuration.Config.EnableURLRewriting)
            {
                if (!fullUrl)
                {
                    return Globals.ResolveUrl($"{Globals.ApplicationURL(yafTab.TabID)}&{url}");
                }

                var baseUrlMask = yafBoardSettings.BaseUrlMask;

                if (baseUrlMask.EndsWith("/"))
                {
                    baseUrlMask = baseUrlMask.Remove(baseUrlMask.Length - 1);
                }

                return
                    $"{baseUrlMask}{Globals.ResolveUrl($"{Globals.ApplicationURL(yafTab.TabID)}&{url}")}";
            }

            var newUrl = new StringBuilder();

            var boardNameOrPageName = UrlRewriteHelper.CleanStringForURL(yafTab.TabName);

            var parser = new SimpleURLParameterParser(url);

            var pageName = parser["g"];
            var forumPage = ForumPages.Board;
            var getDescription = false;

            if (pageName.IsSet())
            {
                try
                {
                    forumPage = pageName.ToEnum<ForumPages>();
                    getDescription = true;
                }
                catch (Exception)
                {
                    getDescription = false;
                }
            }

            if (getDescription)
            {
                switch (forumPage)
                {
                    case ForumPages.Topics:
                        {
                            boardNameOrPageName = UrlRewriteHelper.CleanStringForURL(parser["name"]);
                        }

                        break;
                    case ForumPages.Posts:
                        {
                            if (parser["t"].IsSet())
                            {
                                var topicName = UrlRewriteHelper.CleanStringForURL(parser["name"]);

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
                                    topicName = UrlRewriteHelper.CleanStringForURL(parser["name"]);

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
                    case ForumPages.UserProfile:
                        {
                            var userInfo = UserController.GetUserByName(parser["name"]);

                            if (userInfo != null)
                            {
                                return Globals.UserProfileURL(userInfo.UserID);
                            }

                            var userId = BoardContext.Current.Get<IAspNetUsersHelper>()
                                .GetUserProviderKeyFromUserID(parser["u"].ToType<int>()).ToType<int>();

                            return Globals.UserProfileURL(userId);
                        }

                    case ForumPages.Board:
                        {
                            if (parser["c"].IsSet())
                            {
                                boardNameOrPageName = UrlRewriteHelper.CleanStringForURL(parser["name"]);
                            }
                            else if (parser["g"].IsSet())
                            {
                                return yafTab.FullUrl;
                            }
                        }

                        break;
                }
            }

            if (boardNameOrPageName.Equals(yafTab.TabName))
            {
                boardNameOrPageName = string.Empty;
            }

            newUrl.Append(
                FriendlyUrlProvider.Instance()
                    .FriendlyUrl(
                        yafTab,
                        $"{Globals.ApplicationURL(yafTab.TabID)}&{parser.CreateQueryString(new[] { "name" })}",
                        $"{boardNameOrPageName}.aspx",
                        portalSettings));

            // add anchor
            /*if (parser.HasAnchor)
            {
               newUrl.AppendFormat("#{0}", parser.Anchor);
            }*/

            var finalUrl = newUrl.ToString();

            if (finalUrl.EndsWith("/"))
            {
                finalUrl = finalUrl.Remove(finalUrl.Length - 1);
            }

            finalUrl = finalUrl.Replace("/%20/", "-");

            return finalUrl.Length >= 260
                       ? GetStandardUrl(yafTab, url, boardNameOrPageName, portalSettings)
                       : finalUrl;
        }

        #endregion

        /// <summary>
        /// Gets the base URL.
        /// </summary>
        /// <param name="yafTab">The YAF tab.</param>
        /// <returns>
        /// Returns the BaseUrl
        /// </returns>
        private static string GetBaseUrl([NotNull] TabInfo yafTab)
        {
            var baseUrl = Globals.ApplicationURL(yafTab.TabID);

            if (baseUrl.EndsWith(yafTab.TabName, StringComparison.InvariantCultureIgnoreCase))
            {
                baseUrl = baseUrl.Replace(yafTab.TabName, string.Empty);
            }

            if (baseUrl.EndsWith($"{yafTab.TabName}.aspx"))
            {
                baseUrl = baseUrl.Replace($"{yafTab.TabName}.aspx", string.Empty);
            }

            if (baseUrl.EndsWith($"{yafTab.TabName.ToLower()}.aspx"))
            {
                baseUrl = baseUrl.Replace($"{yafTab.TabName.ToLower()}.aspx", string.Empty);
            }

            return baseUrl;
        }

        /// <summary>
        /// Gets the standard URL without any specific page names.
        /// </summary>
        /// <param name="activeTab">The active tab.</param>
        /// <param name="url">The URL.</param>
        /// <param name="boardNameOrPageName">Name of the board name original page.</param>
        /// <param name="portalSettings">The portal settings.</param>
        /// <returns>Returns the Normal URL</returns>
        private static string GetStandardUrl(
            [NotNull] TabInfo activeTab,
            string url,
            string boardNameOrPageName,
            IPortalSettings portalSettings)
        {
            return FriendlyUrlProvider.Instance()
                .FriendlyUrl(
                    activeTab,
                    $"{Globals.ApplicationURL(activeTab.TabID)}&{url}",
                    boardNameOrPageName,
                    portalSettings);
        }
    }
}