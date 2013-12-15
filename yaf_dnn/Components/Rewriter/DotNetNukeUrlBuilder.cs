/* Yet Another Forum.net
 * Copyright (C) 2006-2013 Jaben Cargman
 * http://www.yetanotherforum.net/
 * 
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 */

namespace YAF.DotNetNuke
{
    #region Using

    using System;
    using System.Linq;
    using System.Text;

    using global::DotNetNuke.Common;
    using global::DotNetNuke.Entities.Modules;
    using global::DotNetNuke.Entities.Portals;

    using global::DotNetNuke.Entities.Tabs;
    using global::DotNetNuke.Entities.Users;

    using YAF.Classes;
    using YAF.Core;
    using YAF.Core.Helpers;
    using YAF.Core.URLBuilder;
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
            return this.BuildUrl(url);
        }

        /// <summary>
        /// The build url.
        /// </summary>
        /// <param name="url">
        /// The url.
        /// </param>
        /// <returns>
        /// The new Url.
        /// </returns>
        public override string BuildUrl(string url)
        {
            var newUrl = new StringBuilder();

            var portalSettings = PortalController.GetCurrentPortalSettings();

            var yafTab = this.GetYAFTab(portalSettings);

            var boardNameOrPageName = UrlRewriteHelper.CleanStringForURL(
                YafContext.Current.Get<YafBoardSettings>().Name);

            if (!Config.EnableURLRewriting)
            {
                return this.GetStandardUrl(yafTab, url, boardNameOrPageName, portalSettings);
            }

            var parser = new SimpleURLParameterParser(url);

            switch (parser["g"])
            {
                case "topics":
                    {
                        boardNameOrPageName = UrlRewriteHelper.GetForumName(parser["f"].ToType<int>());
                    }

                    break;
                case "posts":
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
                case "profile":
                    {
                        boardNameOrPageName = UrlRewriteHelper.GetProfileName(parser["u"].ToType<int>());
                    }

                    break;
                case "forum":
                    {
                        if (parser["c"].IsSet())
                        {
                            boardNameOrPageName = UrlRewriteHelper.GetCategoryName(parser["c"].ToType<int>());
                        }
                    }

                    break;
                case "cp_editprofile":
                    {
                        // Redirect the user to the Dnn profile page.
                        return Globals.UserProfileURL(UserController.GetCurrentUserInfo().UserID);
                    }
            }

            newUrl.AppendFormat(
                Globals.FriendlyUrl(
                    yafTab,
                    "{0}&{1}".FormatWith(Globals.ApplicationURL(yafTab.TabID), url),
                    boardNameOrPageName,
                    portalSettings));

            /*newUrl.AppendFormat(
               FriendlyUrlProvider.Instance().FriendlyUrl(
                    activeTab,
                    "~/Default.aspx?TabId={0}&{1}".FormatWith(activeTab.TabID, url),
                    boardNameOrPageName,
                    portalSettings));*/

            // add anchor
            if (parser.HasAnchor)
            {
                newUrl.AppendFormat("#{0}", parser.Anchor);
            }

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
            return Globals.FriendlyUrl(
                activeTab,
                "{0}&{1}".FormatWith(Globals.ApplicationURL(activeTab.TabID), url),
                boardNameOrPageName,
                portalSettings);
        }

        /// <summary>
        /// Gets the YAF tab id.
        /// </summary>
        /// <param name="portalSettings">The portal settings.</param>
        /// <returns>Return the YAF tab id</returns>
        private TabInfo GetYAFTab(PortalSettings portalSettings)
        {
            var tabs = TabController.GetPortalTabs(portalSettings.PortalId, -1, true, true);

            var desktopModuleInfo =
                DesktopModuleController.GetDesktopModuleByModuleName("YetAnotherForumDotNet", portalSettings.PortalId);

            foreach (TabInfo tab in from tab in tabs
                                    where tab != null && !tab.IsDeleted
                                    let modules = new ModuleController()
                                    from pair in modules.GetTabModules(tab.TabID)
                                    let module = pair.Value
                                    where
                                        !module.IsDeleted && module.DesktopModuleID == desktopModuleInfo.DesktopModuleID
                                    select tab)
            {
                return tab;
            }

            return portalSettings.ActiveTab;
        }
    }
}