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
    #region

    using System;
    using System.Collections;
    using System.Data;
    using System.Globalization;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Web;
    using System.Web.Security;
    using System.Web.UI;
    using System.Web.UI.WebControls;

    using global::DotNetNuke.Common;

    using global::DotNetNuke.Entities.Modules;
    using global::DotNetNuke.Entities.Users;
    using global::DotNetNuke.Framework;

    using global::DotNetNuke.Services.Exceptions;

    using global::DotNetNuke.Services.Localization;

    using YAF.Classes;
    using YAF.Classes.Data;
    using YAF.Controls;
    using YAF.Core;
    using YAF.Core.Helpers;
    using YAF.DotNetNuke.Components.Controllers;
    using YAF.DotNetNuke.Components.Utils;
    using YAF.Types.Extensions;
    using YAF.Types.Interfaces;
    using YAF.Utils.Helpers;

    #endregion

    /// <summary>
    /// The YAF What's new Module Page.
    /// </summary>
    public partial class YafDnnWhatsNew : PortalModuleBase
    {
        #region Constants and Fields

        /// <summary>
        ///   Use Relative Time Setting
        /// </summary>
        private bool useRelativeTime;

        /// <summary>
        ///   The YAF board id.
        /// </summary>
        private int boardId;

        /// <summary>
        ///   The max posts.
        /// </summary>
        private int maxPosts;

        /// <summary>
        ///   The YAF module id.
        /// </summary>
        private int yafModuleId;

        /// <summary>
        ///   The YAF tab id.
        /// </summary>
        private int yafTabId;

        /// <summary>
        /// The header Template
        /// </summary>
        private string headerTemplate;

        /// <summary>
        /// The item Template
        /// </summary>
        private string itemTemplate;

        /// <summary>
        /// The footer Template
        /// </summary>
        private string footerTemplate;

        #endregion

        #region Methods
        
        /// <summary>
        /// The latest posts_ item data bound.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="RepeaterItemEventArgs" /> instance containing the event data.</param>
        protected void LatestPostsItemDataBound(object sender, RepeaterItemEventArgs e)
        {
            switch (e.Item.ItemType)
            {
                case ListItemType.Header:
                    {
                        Literal objLiteral = new Literal { Text = this.GetHeader() };
                        e.Item.Controls.Add(objLiteral);
                    }

                    break;
                case ListItemType.AlternatingItem:
                case ListItemType.Item:
                    {
                        Literal objLiteral = new Literal { Text = this.ProcessItem(e) };
                        e.Item.Controls.Add(objLiteral);
                    }

                    break;
                    /*case ListItemType.Separator:
                    {
                        Literal objLiteral = new Literal { Text = this.ProcessSeparator() };
                        e.Item.Controls.Add(objLiteral);
                    }

                    break;*/
                case ListItemType.Footer:
                    {
                        Literal objLiteral = new Literal { Text = this.GetFooter() };
                        e.Item.Controls.Add(objLiteral);
                    }

                    break;
            }
        }

        /// <summary>
        /// Handles the Load event of the Page control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        protected void Page_Load(object sender, EventArgs e)
        {
            Type csType = typeof(Page);

            this.LoadSettings();

            if (this.useRelativeTime)
            {
                jQuery.RequestRegistration();

                ScriptManager.RegisterClientScriptInclude(
                    this,
                    csType,
                    "timeagojs",
                    this.ResolveUrl("~/DesktopModules/YetAnotherForumDotNet/resources/js/jquery.timeago.js"));

                var timeagoLoadJs = @"Sys.WebForms.PageRequestManager.getInstance().add_pageLoaded(loadTimeAgo);
            function loadTimeAgo() {{				      	
            {0}
              jQuery('abbr.timeago').timeago();	
			      }}".FormatWith(Localization.GetString("TIMEAGO_JS", this.LocalResourceFile));

                ScriptManager.RegisterStartupScript(this, csType, "timeagoloadjs", timeagoLoadJs, true);
            }

            this.BindData();
        }

        /// <summary>
        /// Get the YAF Board id from ModuleId and the Module Settings
        /// </summary>
        /// <param name="iModuleId">
        /// The Module id of the YAF Module Instance
        /// </param>
        /// <returns>
        /// The Board Id From the Settings
        /// </returns>
        private static int GetYafBoardId(int iModuleId)
        {
            var moduleController = new ModuleController();
            Hashtable moduleSettings = moduleController.GetModuleSettings(iModuleId);

            int iForumId;

            try
            {
                iForumId = int.Parse((string)moduleSettings["forumboardid"]);
            }
            catch (Exception)
            {
                iForumId = -1;
            }

            return iForumId;
        }

        /// <summary>
        /// Binds the data.
        /// </summary>
        private void BindData()
        {
            try
            {
                var yafUserId = this.GetYafUserId();

                Data.ActiveAccessUser(this.boardId, yafUserId, HttpContext.Current.User.Identity.IsAuthenticated);

                var activeTopics = Data.TopicLatest(this.boardId, this.maxPosts, yafUserId, false, true);

                this.LatestPosts.DataSource = activeTopics;
                this.LatestPosts.DataBind();

                if (activeTopics.Rows.Count <= 0)
                {
                    this.lInfo.Text = Localization.GetString("NoMessages.Text", this.LocalResourceFile);
                    this.lInfo.Style.Add("font-style", "italic");
                }
                else
                {
                    this.lInfo.Text = string.Empty;
                }
            }
            catch (Exception exc)
            {
                // Module failed to load 
                Exceptions.ProcessModuleLoadException(this, exc);
            }
        }

        /// <summary>
        /// Get YAF User Id from the Current DNN User
        /// </summary>
        /// <returns>
        /// The YAF User ID
        /// </returns>
        private int GetYafUserId()
        {
            // Check for user
            if (!HttpContext.Current.User.Identity.IsAuthenticated)
            {
                return UserMembershipHelper.GuestUserId;
            }

            // get the user from the membership provider
            var dnnUser = Membership.GetUser(this.UserInfo.Username, true);

            // Check if the user exists in yaf
            var yafUserId = LegacyDb.user_get(this.boardId, dnnUser.ProviderUserKey);

            if (!yafUserId.Equals(0))
            {
                return yafUserId;
            }

            // Get current DNN user
            var dnnUserInfo = UserController.GetCurrentUserInfo();

            return UserImporter.CreateYafUser(
                dnnUserInfo,
                dnnUser,
                this.boardId,
                this.PortalSettings,
                YafContext.Current.Get<YafBoardSettings>());
        }

        /// <summary>
        /// Load Module Settings
        /// </summary>
        private void LoadSettings()
        {
            try
            {
                var objModuleController = new ModuleController();

                var moduleSettings = objModuleController.GetTabModuleSettings(this.TabModuleId);

                if (moduleSettings["YafPage"].ToType<string>().IsSet())
                {
                    this.yafTabId = moduleSettings["YafPage"].ToType<int>();
                }
                else
                {
                    // If Module is not Configured show Message or Redirect to Settings Page if Current User has Edit Rights
                    if (this.IsEditable)
                    {
                        this.Response.Redirect(
                            this.ResolveUrl(
                                "~/tabid/{0}/ctl/Module/ModuleId/{1}/Default.aspx".FormatWith(
                                    this.PortalSettings.ActiveTab.TabID,
                                    this.ModuleId)));
                    }
                    else
                    {
                        this.lInfo.Text = Localization.GetString("NotConfigured.Text", this.LocalResourceFile);
                        this.lInfo.Style.Add("font-style", "italic");
                    }
                }

                if (moduleSettings["YafModuleId"].ToType<string>().IsSet())
                {
                    this.yafModuleId = moduleSettings["YafModuleId"].ToType<int>();
                }
                else
                {
                    // If Module is not Configured show Message or Redirect to Settings Page if Current User has Edit Rights
                    if (this.IsEditable)
                    {
                        this.Response.Redirect(
                            this.ResolveUrl(
                                "~/tabid/{0}/ctl/Module/ModuleId/{1}/Default.aspx".FormatWith(
                                    this.PortalSettings.ActiveTab.TabID,
                                    this.ModuleId)));
                    }
                    else
                    {
                        this.lInfo.Text = Localization.GetString("NotConfigured.Text", this.LocalResourceFile);
                        this.lInfo.Style.Add("font-style", "italic");
                    }
                }

                // Get and Set Board Id
                this.boardId = GetYafBoardId(this.yafModuleId);

                if (this.boardId.Equals(-1))
                {
                    // If Module is not Configured show Message or Redirect to Settings Page if Current User has Edit Rights
                    if (this.IsEditable)
                    {
                        this.Response.Redirect(
                            this.ResolveUrl(
                                "~/tabid/{0}/ctl/Module/ModuleId/{1}/Default.aspx".FormatWith(
                                    this.PortalSettings.ActiveTab.TabID,
                                    this.ModuleId)));
                    }
                    else
                    {
                        this.lInfo.Text = Localization.GetString("NotConfigured.Text", this.LocalResourceFile);
                        this.lInfo.Style.Add("font-style", "italic");
                    }
                }

                this.maxPosts = moduleSettings["YafMaxPosts"].ToType<string>().IsSet()
                                    ? moduleSettings["YafMaxPosts"].ToType<int>()
                                    : 10;

                this.useRelativeTime = !moduleSettings["YafUseRelativeTime"].ToType<string>().IsSet()
                                       || moduleSettings["YafUseRelativeTime"].ToType<bool>();

                this.headerTemplate = moduleSettings["YafWhatsNewHeader"].ToType<string>().IsSet()
                                          ? moduleSettings["YafWhatsNewHeader"].ToType<string>()
                                          : "<ul>";

                this.itemTemplate = moduleSettings["YafWhatsNewItemTemplate"].ToType<string>().IsSet()
                                        ? moduleSettings["YafWhatsNewItemTemplate"].ToType<string>()
                                        : "<li class=\"YafPosts\">[LASTPOSTICON]&nbsp;<strong>[TOPICLINK]</strong>&nbsp;([FORUMLINK])<br />\"[LASTMESSAGE:150]\"<br />[BYTEXT]&nbsp;[LASTUSERLINK]&nbsp;[LASTPOSTEDDATETIME]</li>";

                this.footerTemplate = moduleSettings["YafWhatsNewFooter"].ToType<string>().IsSet()
                                          ? moduleSettings["YafWhatsNewFooter"].ToType<string>()
                                          : "</ul>";
            }
            catch (Exception exc)
            {
                Exceptions.ProcessModuleLoadException(this, exc);
            }
        }

        /// <summary>
        /// Gets the List header.
        /// </summary>
        /// <returns>Returns the html header.</returns>
        private string GetHeader()
        {
            return this.headerTemplate;
        }

        /// <summary>
        /// Processes the item.
        /// </summary>
        /// <param name="e">The <see cref="RepeaterItemEventArgs" /> instance containing the event data.</param>
        /// <returns>Returns the Item as string</returns>
        private string ProcessItem(RepeaterItemEventArgs e)
        {
            var currentRow = (DataRowView)e.Item.DataItem;
            
            var currentItem = this.itemTemplate;
            
            var messageUrl =
                this.ResolveUrl(
                    "~/Default.aspx?tabid={1}&g=posts&m={0}#post{0}".FormatWith(
                        currentRow["LastMessageID"],
                        this.yafTabId));

            // make message url...
            if (Classes.Config.EnableURLRewriting)
            {
                messageUrl =
                    Globals.ResolveUrl(
                        "~/tabid/{0}/g/posts/m/{1}/{2}.aspx#post{1}".FormatWith(
                            this.yafTabId,
                            currentRow["LastMessageID"],
                            UrlRewriteHelper.CleanStringForURL(YafContext.Current.Get<IBadWordReplace>().Replace(currentRow["Topic"].ToString()))));
            }

            // Render [LASTPOSTICON]
            var lastPostedImage = new ThemeImage
                                      {
                                          LocalizedTitlePage = "DEFAULT",
                                          LocalizedTitleTag = "GO_LAST_POST",
                                          LocalizedTitle =
                                              Localization.GetString("LastPost.Text", this.LocalResourceFile),
                                          ThemeTag = "TOPIC_NEW",
                                          Style = "width:16px;height:16px"
                                      };

            currentItem = currentItem.Replace("[LASTPOSTICON]", lastPostedImage.RenderToString());

            // Render [TOPICLINK]
            var textMessageLink = new HyperLink
                                      {
                                          Text =
                                              YafContext.Current.Get<IBadWordReplace>()
                                              .Replace(currentRow["Topic"].ToString()),
                                          NavigateUrl = messageUrl
                                      };

            currentItem = currentItem.Replace("[TOPICLINK]", textMessageLink.RenderToString());

            // Render [FORUMLINK]
            var forumLink = new HyperLink
                                {
                                    Text = currentRow["Forum"].ToString(),
                                    NavigateUrl =
                                        Classes.Config.EnableURLRewriting
                                            ? Globals.ResolveUrl(
                                                "~/tabid/{0}/g/topics/f/{1}/{2}.aspx".FormatWith(
                                                    this.yafTabId,
                                                    currentRow["ForumID"],
                                                    currentRow["Forum"]))
                                            : this.ResolveUrl(
                                                "~/Default.aspx?tabid={1}&g=topics&f={0}".FormatWith(
                                                    currentRow["ForumID"],
                                                    this.yafTabId))
                                };

            currentItem = currentItem.Replace("[FORUMLINK]", forumLink.RenderToString());

            // Render [BYTEXT]
            currentItem = currentItem.Replace(
                "[BYTEXT]",
                YafContext.Current.Get<IHaveLocalization>().GetText("SEARCH", "BY"));

            // Render [LASTUSERLINK]
            // Just in case...
            if (currentRow["LastUserID"] != DBNull.Value)
            {
                var userName = YafContext.Current.Get<YafBoardSettings>().EnableDisplayName
                                   ? currentRow["LastUserDisplayName"].ToString()
                                   : currentRow["LastUserName"].ToString();

                userName = this.HtmlEncode(userName);

                var lastUserLink = new HyperLink
                                       {
                                           Text = userName,
                                           ToolTip = userName,
                                           NavigateUrl =
                                               Classes.Config.EnableURLRewriting
                                                   ? Globals.ResolveUrl(
                                                       "~/tabid/{0}/g/profile/u/{1}/{2}.aspx".FormatWith(
                                                           this.yafTabId,
                                                           currentRow["LastUserID"],
                                                           userName))
                                                   : this.ResolveUrl(
                                                       "~/Default.aspx?tabid={1}&g=profile&u={0}".FormatWith(
                                                           currentRow["LastUserID"],
                                                           this.yafTabId))
                                       };

                currentItem = currentItem.Replace("[LASTUSERLINK]", lastUserLink.RenderToString());
            }

            // Render [LASTMESSAGE]
            var lastMessage =
                BBCodeHelper.StripBBCode(
                    HtmlHelper.StripHtml(HtmlHelper.CleanHtmlString(currentRow["LastMessage"].ToType<string>())))
                    .RemoveMultipleWhitespace();

            try
            {
                var match = Regex.Match(currentItem, @"\[LASTMESSAGE\:(?<count>[0-9]*)\]", RegexOptions.Compiled);

                if (match.Success)
                {
                    var messageLimit = match.Groups["count"].Value.ToType<int>();

                    currentItem = currentItem.Replace(
                        "[LASTMESSAGE:{0}]".FormatWith(match.Groups["count"].Value),
                        lastMessage.Truncate(messageLimit));
                }
                else
                {
                    currentItem = currentItem.Replace("[LASTMESSAGE]", lastMessage);
                }
            }
            catch (Exception)
            {
                currentItem = currentItem.Replace("[LASTMESSAGE]", lastMessage);
            }

            // Render [LASTPOSTEDDATETIME]
            var displayDateTime = new DisplayDateTime { DateTime = currentRow["LastPosted"].ToType<DateTime>() };

            currentItem = currentItem.Replace("[LASTPOSTEDDATETIME]", displayDateTime.RenderToString());
            
            return currentItem;
        }

        /// <summary>
        /// Gets the List footer.
        /// </summary>
        /// <returns>Returns the html footer.</returns>
        private string GetFooter()
        {
            return this.footerTemplate;
        }

        #endregion
    }
}