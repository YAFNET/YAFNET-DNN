/* Yet Another Forum.NET
 * Copyright (C) 2003-2005 Bj�rnar Henden
 * Copyright (C) 2006-2013 Jaben Cargman
 * Copyright (C) 2014-2020 Ingo Herbote
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
    #region

    using System;
    using System.Data;
    using System.Text.RegularExpressions;
    using System.Web;
    using System.Web.Security;
    using System.Web.UI;
    using System.Web.UI.WebControls;

    using global::DotNetNuke.Common;
    using global::DotNetNuke.Entities.Modules;
    using global::DotNetNuke.Entities.Tabs;
    using global::DotNetNuke.Entities.Users;
    using global::DotNetNuke.Framework.JavaScriptLibraries;
    using global::DotNetNuke.Services.Exceptions;
    using global::DotNetNuke.Services.Localization;
    using global::DotNetNuke.Services.Url.FriendlyUrl;

    using YAF.Configuration;
    using YAF.Core.Context;
    using YAF.Core.Helpers;
    using YAF.Core.Model;
    using YAF.Core.UsersRoles;
    using YAF.DotNetNuke.Components.Controllers;
    using YAF.DotNetNuke.Components.Utils;
    using YAF.Types.Extensions;
    using YAF.Types.Interfaces;
    using YAF.Types.Models;
    using YAF.Utils.Helpers;
    using YAF.Utils.Helpers.StringUtils;
    using YAF.Web.Controls;

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
        /// The YAF TabInfo.
        /// </summary>
        private TabInfo yafTabInfo;

        /// <summary>
        /// The sort order
        /// </summary>
        private string sortOrder;

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
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The <see cref="RepeaterItemEventArgs"/> instance containing the event data.
        /// </param>
        protected void LatestPostsItemDataBound(object sender, RepeaterItemEventArgs e)
        {
            switch (e.Item.ItemType)
            {
                case ListItemType.Header:
                    {
                        var objLiteral = new Literal { Text = this.GetHeader() };
                        e.Item.Controls.Add(objLiteral);
                    }

                    break;
                case ListItemType.AlternatingItem:
                case ListItemType.Item:
                    {
                        var objLiteral = new Literal { Text = this.ProcessItem(e) };
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
                        var objLiteral = new Literal { Text = this.GetFooter() };
                        e.Item.Controls.Add(objLiteral);
                    }

                    break;
            }
        }

        /// <summary>
        /// Handles the Load event of the Page control.
        /// </summary>
        /// <param name="sender">
        /// The source of the event.
        /// </param>
        /// <param name="e">
        /// The <see cref="EventArgs"/> instance containing the event data.
        /// </param>
        protected void Page_Load(object sender, EventArgs e)
        {
            this.LoadSettings();

            var type = typeof(Page);

            if (this.useRelativeTime)
            {
                JavaScript.RequestRegistration(CommonJs.jQuery);

                ScriptManager.RegisterClientScriptInclude(
                this,
                type,
                "timeagojs",
                this.ResolveUrl("~/DesktopModules/YAF.WhatsNew/jquery.ForumExtensions.min.js"));

                var momentLoadJs = $@"Sys.WebForms.PageRequestManager.getInstance().add_pageLoaded(loadTimeAgo);
            function loadTimeAgo() {{
            
		    moment.locale('{(BoardContext.Current.CultureUser.IsSet()
                                 ? BoardContext.Current.CultureUser.Substring(0, 2)
                                 : BoardContext.Current.Get<BoardSettings>().Culture.Substring(0, 2))}');
            jQuery('abbr.timeago').html(function(index, value) {{
                 
            return moment(value).fromNow();
            }});}}";

                ScriptManager.RegisterStartupScript(this, type, "timeagoloadjs", momentLoadJs, true);
            }

            this.BindData();
        }

        /// <summary>
        /// Get the YAF Board id from ModuleId and the Module Settings
        /// </summary>
        /// <param name="moduleId">
        ///     The Module id of the YAF Module Instance
        /// </param>
        /// <param name="tabId">
        /// The Tab ID of the YAF Module Instance
        /// </param>
        /// <returns>
        /// The Board Id From the Settings
        /// </returns>
        private static int GetYafBoardId(int moduleId, int tabId)
        {
            var moduleInfo = new ModuleController().GetModule(moduleId, tabId);

            var moduleSettings = moduleInfo.ModuleSettings;

            int forumId;

            try
            {
                forumId = moduleSettings["forumboardid"].ToType<int>();
            }
            catch (Exception)
            {
                forumId = -1;
            }

            return forumId;
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

                // Resort the table
                activeTopics.DefaultView.Sort = this.sortOrder switch
                {
                    "views" => "Views DESC",
                    "replies" => "NumPosts DESC",
                    "lastpost" => "LastPosted DESC",
                    _ => "LastPosted DESC",
                };
                this.LatestPosts.DataSource = activeTopics;
                this.LatestPosts.DataBind();

                if (activeTopics.Rows.Count <= 0)
                {
                    this.lInfo.Text = Localization.GetString("NoMessages.Text", this.LocalResourceFile);
                    this.lInfo.Style.Add("font-style", "italic");
                    this.lInfo.Visible = true;
                }
                else
                {
                  this.lInfo.Visible = false;
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
            var yafUserId = BoardContext.Current.GetRepository<User>().GetUserId(this.boardId, dnnUser.ProviderUserKey.ToString());

            if (!yafUserId.Equals(0))
            {
                return yafUserId;
            }

            // Get current DNN user
            var dnnUserInfo = UserController.Instance.GetCurrentUserInfo();

            return UserImporter.CreateYafUser(
                dnnUserInfo,
                dnnUser,
                this.boardId,
                this.PortalSettings.PortalId,
                BoardContext.Current.Get<BoardSettings>());
        }

        /// <summary>
        /// Load Module Settings
        /// </summary>
        private void LoadSettings()
        {
            try
            {
                var moduleSettings = this.Settings;

                if (moduleSettings["YafPage"].ToType<string>().IsSet())
                {
                    this.yafTabId = moduleSettings["YafPage"].ToType<int>();

                    this.yafTabInfo = new TabController().GetTab(this.yafTabId, this.PortalSettings.PortalId, true);
                }
                else
                {
                    // If Module is not Configured show Message or Redirect to Settings Page if Current User has Edit Rights
                    if (this.IsEditable)
                    {
                        this.Response.Redirect(
                            this.ResolveUrl(
                                $"~/tabid/{this.PortalSettings.ActiveTab.TabID}/ctl/Module/ModuleId/{this.ModuleId}/Default.aspx"));
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
                                $"~/tabid/{this.PortalSettings.ActiveTab.TabID}/ctl/Module/ModuleId/{this.ModuleId}/Default.aspx"));
                    }
                    else
                    {
                        this.lInfo.Text = Localization.GetString("NotConfigured.Text", this.LocalResourceFile);
                        this.lInfo.Style.Add("font-style", "italic");
                    }
                }

                // Get and Set Board Id
                this.boardId = GetYafBoardId(this.yafModuleId, this.yafTabId);

                if (this.boardId.Equals(-1))
                {
                    // If Module is not Configured show Message or Redirect to Settings Page if Current User has Edit Rights
                    if (this.IsEditable)
                    {
                        this.Response.Redirect(
                            this.ResolveUrl(
                                $"~/tabid/{this.PortalSettings.ActiveTab.TabID}/ctl/Module/ModuleId/{this.ModuleId}/Default.aspx"));
                    }
                    else
                    {
                        this.lInfo.Text = Localization.GetString("NotConfigured.Text", this.LocalResourceFile);
                        this.lInfo.Style.Add("font-style", "italic");
                    }
                }
                else
                {
                    this.sortOrder = moduleSettings["YafSortOrder"].ToType<string>().IsSet()
                                        ? moduleSettings["YafSortOrder"].ToType<string>()
                                        : "lastpost";

                    this.maxPosts = moduleSettings["YafMaxPosts"].ToType<string>().IsSet()
                                    ? moduleSettings["YafMaxPosts"].ToType<int>()
                                    : 10;

                    this.useRelativeTime = !moduleSettings["YafUseRelativeTime"].ToType<string>().IsSet()
                                           || moduleSettings["YafUseRelativeTime"].ToType<bool>();

                    this.headerTemplate = moduleSettings["YafWhatsNewHeader"].ToType<string>().IsSet()
                                              ? moduleSettings["YafWhatsNewHeader"].ToType<string>()
                                              : @"<div class=""card"" style=""width: 20rem;""><ul class=""list-group list-group-flush"">";

                    this.itemTemplate = moduleSettings["YafWhatsNewItemTemplate"].ToType<string>().IsSet()
                                            ? moduleSettings["YafWhatsNewItemTemplate"].ToType<string>()
                                            : "<li class=\"list-group-item\">[LASTPOSTICON]&nbsp;<strong>[TOPICLINK]</strong>&nbsp;([FORUMLINK])<br />\"[LASTMESSAGE:150]\"<br />[BYTEXT]&nbsp;[LASTUSERLINK]&nbsp;[LASTPOSTEDDATETIME]</li>";

                    this.footerTemplate = moduleSettings["YafWhatsNewFooter"].ToType<string>().IsSet()
                                              ? moduleSettings["YafWhatsNewFooter"].ToType<string>()
                                              : "</ul></div>";
                }
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
        /// <param name="e">
        /// The <see cref="RepeaterItemEventArgs"/> instance containing the event data.
        /// </param>
        /// <returns>
        /// Returns the Item as string
        /// </returns>
        private string ProcessItem(RepeaterItemEventArgs e)
        {
            var currentRow = (DataRowView)e.Item.DataItem;

            var currentItem = this.itemTemplate;

            var messageUrl = FriendlyUrlProvider.Instance().FriendlyUrl(
                this.yafTabInfo,
                $"{Globals.ApplicationURL(this.yafTabInfo.TabID)}&g=posts&m={currentRow["LastMessageID"]}",
                UrlRewriteHelper.CleanStringForURL(
                    BoardContext.Current.Get<IBadWordReplace>().Replace(currentRow["Topic"].ToString())));

            currentItem = currentItem.Replace("[LASTPOSTICON]", string.Empty);

            // Render TOPICLINK
            var textMessageLink = new HyperLink
                                      {
                                          Text =
                                              BoardContext.Current.Get<IBadWordReplace>()
                                              .Replace(currentRow["Topic"].ToString()),
                                          NavigateUrl = messageUrl
                                      };

            currentItem = currentItem.Replace("[TOPICLINK]", textMessageLink.RenderToString());

            // Render FORUMLINK
            var forumLink = new HyperLink
                                {
                                    Text = currentRow["Forum"].ToString(),
                                    NavigateUrl = FriendlyUrlProvider.Instance().FriendlyUrl(
                                        this.yafTabInfo,
                                        $"{Globals.ApplicationURL(this.yafTabInfo.TabID)}&g=topics&f={currentRow["ForumID"]}",
                                        UrlRewriteHelper.CleanStringForURL(
                                            BoardContext.Current.Get<IBadWordReplace>()
                                                .Replace(currentRow["Forum"].ToString())))
                                };

            currentItem = currentItem.Replace("[FORUMLINK]", forumLink.RenderToString());

            // Render BYTEXT
            currentItem = currentItem.Replace(
                "[BYTEXT]",
                BoardContext.Current.Get<IHaveLocalization>().GetText("SEARCH", "BY"));

            // Render LASTUSERLINK
            // Just in case...
            if (currentRow["LastUserID"] != DBNull.Value)
            {
                var userName = BoardContext.Current.Get<BoardSettings>().EnableDisplayName
                                   ? currentRow["LastUserDisplayName"].ToString()
                                   : currentRow["LastUserName"].ToString();

                userName = new UnicodeEncoder().XSSEncode(userName);

                var lastUserLink = new HyperLink
                                       {
                                           Text = userName,
                                           ToolTip = userName,
                                           NavigateUrl = FriendlyUrlProvider.Instance().FriendlyUrl(
                                               this.yafTabInfo,
                                               $"{Globals.ApplicationURL(this.yafTabInfo.TabID)}&g=profile&u={currentRow["LastUserID"]}",
                                               userName)
                                       };

                currentItem = currentItem.Replace("[LASTUSERLINK]", lastUserLink.RenderToString());
            }

            // Render LASTMESSAGE
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
                        $"[LASTMESSAGE:{match.Groups["count"].Value}]",
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

            // Render LASTPOSTEDDATETIME
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