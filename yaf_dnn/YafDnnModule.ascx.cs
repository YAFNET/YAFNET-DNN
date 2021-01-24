﻿/* Yet Another Forum.NET
 * Copyright (C) 2003-2005 Bjørnar Henden
 * Copyright (C) 2006-2013 Jaben Cargman
 * Copyright (C) 2014-2021 Ingo Herbote
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
    using System.Collections.Generic;
    using System.Threading;
    using System.Web;
    using System.Web.Security;
    using System.Web.UI;
    using System.Web.UI.WebControls;

    using global::DotNetNuke.Common.Utilities;
    using global::DotNetNuke.Entities.Modules;
    using global::DotNetNuke.Entities.Modules.Actions;
    using global::DotNetNuke.Entities.Portals;
    using global::DotNetNuke.Entities.Users;
    using global::DotNetNuke.Framework;
    using global::DotNetNuke.Framework.JavaScriptLibraries;
    using global::DotNetNuke.Security;
    using global::DotNetNuke.Services.Exceptions;
    using global::DotNetNuke.Services.Localization;

    using YAF.Configuration;
    using YAF.Core;
    using YAF.Core.Model;
    using YAF.DotNetNuke.Components.Objects;
    using YAF.DotNetNuke.Components.Utils;
    using YAF.Types;
    using YAF.Types.Attributes;
    using YAF.Types.Extensions;
    using YAF.Types.Interfaces;
    using YAF.Types.Models;
    using YAF.Web.EventsArgs;

    using Forum = YAF.Web.Controls.Forum;

    #endregion

    /// <summary>
    /// The DotNetNuke Module Class.
    /// </summary>
    public partial class YafDnnModule : PortalModuleBase, IActionable, IHaveServiceLocator
    {
        #region Constants and Fields

        /// <summary>
        /// The _portal settings.
        /// </summary>
        private PortalSettings portalSettings;

        /// <summary>
        /// The forum 1.
        /// </summary>
        private Forum forum1;

        /// <summary>
        /// The basePage
        /// </summary>
        private CDefault basePage;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the Service Locator.
        /// </summary>
        [Inject]
        public IServiceLocator ServiceLocator { get; }

        /// <summary>
        ///  Gets Add Menu Entries to Module Container
        /// </summary>
        public ModuleActionCollection ModuleActions
        {
            get
            {
                var actions = new ModuleActionCollection
                                                     {
                                                             {
                                                                 this.GetNextActionID(),
                                                                 Localization.GetString(
                                                                     "EditYafSettings.Text",
                                                                     this.LocalResourceFile),
                                                                 ModuleActionType.AddContent,
                                                                 string.Empty, string.Empty, this.EditUrl(), false,
                                                                 SecurityAccessLevel.Host, true, false
                                                             },
                                                             {
                                                                 this.GetNextActionID(),
                                                                 Localization.GetString(
                                                                     "UserImporter.Text",
                                                                     this.LocalResourceFile),
                                                                 ModuleActionType.AddContent,
                                                                 string.Empty, string.Empty, this.EditUrl("Import"), false,
                                                                 SecurityAccessLevel.Host, true, false
                                                             }
                                                     };

                return actions;
            }
        }

        /// <summary>
        /// Gets the YAF Cultures
        /// </summary>
        private static List<YafCultureInfo> YafCultures
        {
            get
            {
                const string CacheKey = "YAF_Cultures";

                List<YafCultureInfo> cultures;

                if (DataCache.GetCache(CacheKey) is List<YafCultureInfo>)
                {
                    cultures = DataCache.GetCache(CacheKey).ToType<List<YafCultureInfo>>();

                    if (cultures.Count == 0)
                    {
                        cultures = CultureUtilities.GetYafCultures();
                    }
                }
                else
                {
                    cultures = CultureUtilities.GetYafCultures();
                }

                return cultures;
            }
        }
        
        /// <summary>
         /// Gets the session user key name.
         /// </summary>
        private string SessionUserKeyName =>
            $"yaf_dnn_boardid{this.forum1.BoardID}_userid{this.UserId}_portalid{this.CurrentPortalSettings.PortalId}";

        /// <summary>
        /// Gets the Base Page
        /// </summary>
        private CDefault BasePage => this.basePage ?? (this.basePage = GetDefault(this));

        /// <summary>
        /// Gets CurrentPortalSettings.
        /// </summary>
        private PortalSettings CurrentPortalSettings =>
            this.portalSettings
            ?? (this.portalSettings = this.ModuleContext.PortalSettings);

        #endregion

        #region Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.TemplateControl.Error" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> that contains the event data.</param>
        protected override void OnError(EventArgs e)
        {
            var x = this.Server.GetLastError();

            BoardContext.Current.Get<ILogger>().Error(x, "Error on the DNN Module");

            base.OnError(e);
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init"/> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs"/> object that contains the event data.</param>
        protected override void OnInit(EventArgs e)
        {
            this.InitializeComponent();

            base.OnInit(e);
        }

        /// <summary>
        /// The On PreRender event.
        /// </summary>
        /// <param name="e">
        /// the Event Arguments
        /// </param>
        protected override void OnPreRender([NotNull] EventArgs e)
        {
            // setup jQuery
            JavaScript.RequestRegistration(CommonJs.jQuery);

            JavaScript.RequestRegistration("bootstrap-bundle");
            JavaScript.Register(this.Page);

            base.OnPreRender(e);
        }

        /// <summary>
        /// Get Default CDefault
        /// </summary>
        /// <param name="control">The <paramref name="control"/>.</param>
        /// <returns>
        /// The Control
        /// </returns>
        private static CDefault GetDefault(Control control)
        {
            while (true)
            {
                var parent = control.Parent;

                if (parent == null)
                {
                    return null;
                }

                if (parent is CDefault cDefault)
                {
                    return cDefault;
                }

                control = parent;
            }
        }

        /// <summary>
        /// Change YAF Language based on DNN Language,
        ///   will <c>override</c> the YAF Language Setting
        /// </summary>
        private static void SetDnnLangToYaf()
        {
            try
            {
                var currentCulture = Thread.CurrentThread.CurrentUICulture;

                var langCode = currentCulture.Name;

                BoardContext.Current.Get<BoardSettings>().Language =
                    YafCultures.Find(yafCult => yafCult.Culture.Equals(langCode)) != null
                        ? YafCultures.Find(yafCult => yafCult.Culture.Equals(langCode)).LanguageFile
                        : "english.xml";
            }
            catch (Exception)
            {
                BoardContext.Current.Get<BoardSettings>().Language = "english.xml";
            }
        }

        /// <summary>
        /// Handles the Load event of the DotNetNukeModule control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void DotNetNukeModuleLoad(object sender, EventArgs e)
        {
            if (this.Page.IsPostBack)
            {
                return;
            }

            // Check for user
            if (!HttpContext.Current.User.Identity.IsAuthenticated)
            {
                return;
            }

            try
            {
                this.CreateOrUpdateUser();
            }
            catch (Exception ex)
            {
                Exceptions.ProcessModuleLoadException(this, ex);
            }
        }

        /// <summary>
        /// Check if roles are synchronized and the user is added to them
        /// </summary>
        /// <param name="dnnUser">The Current DNN User</param>
        /// <param name="yafUserId">The YAF user id.</param>
        private void CheckForRoles(UserInfo dnnUser, int yafUserId)
        {
            // see if the roles have been synchronized...
            if (this.Session[$"{this.SessionUserKeyName}_rolesloaded"] != null)
            {
                return;
            }

            RoleSyncronizer.SynchronizeUserRoles(
                this.forum1.BoardID,
                this.CurrentPortalSettings.PortalId,
                yafUserId,
                dnnUser);

            this.Session[$"{this.SessionUserKeyName}_rolesloaded"] = true;
        }

        /// <summary>
        /// Check if the DNN User exists in YAF, and if the Profile is up to date.
        /// </summary>
        private void CreateOrUpdateUser()
        {
            // Get current DNN user
            var dnnUserInfo = UserController.Instance.GetCurrentUserInfo();

            // get the user from the membership provider
            var dnnMembershipUser = Membership.GetUser(dnnUserInfo.Username, true);

            if (dnnMembershipUser == null)
            {
                return;
            }

            // Check if the user exists in yaf
            var yafUserId = BoardContext.Current.GetRepository<User>().GetUserId(this.forum1.BoardID, dnnMembershipUser.ProviderUserKey.ToString());

            var boardSettings = BoardContext.Current == null
                                    ? new LoadBoardSettings(this.forum1.BoardID)
                                    : BoardContext.Current.Get<BoardSettings>();

            if (yafUserId.Equals(0))
            {
                yafUserId = UserImporter.CreateYafUser(
                    dnnUserInfo,
                    dnnMembershipUser,
                    this.forum1.BoardID,
                    this.CurrentPortalSettings.PortalId,
                    boardSettings);

                // super admin check...
                if (dnnUserInfo.IsSuperUser)
                {
                    UserImporter.SetYafHostUser(yafUserId, this.forum1.BoardID);
                }
            }
            else
            {
                this.CheckForRoles(dnnUserInfo, yafUserId);

                ProfileSyncronizer.UpdateUserProfile(
                    yafUserId,
                    BoardContext.Current.Profile,
                    BoardContext.Current.CurrentUserData,
                    dnnUserInfo,
                    boardSettings);
            }
        }

        /// <summary>
        /// Change Page Title
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ForumPageTitleArgs" /> instance containing the event data.</param>
        private void Forum1_PageTitleSet(object sender, ForumPageTitleArgs e)
        {
            var removeTabName = 1;

            if (this.Settings["RemoveTabName"] != null)
            {
                removeTabName = this.Settings["RemoveTabName"].ToType<int>();
            }

            // Check if Tab Name/Title Matches the Forum Board Name
            if (removeTabName.Equals(2) && BoardContext.Current != null)
            {
                if (BoardContext.Current.Get<BoardSettings>().Name.Equals(this.CurrentPortalSettings.ActiveTab.TabName))
                {
                    removeTabName = 1;
                }

                if (this.CurrentPortalSettings.ActiveTab.Title.IsSet())
                {
                    if (
                        BoardContext.Current.Get<BoardSettings>()
                            .Name.Equals(this.CurrentPortalSettings.ActiveTab.Title))
                    {
                        removeTabName = 1;
                    }
                }
            }

            // Remove Tab Title(Name) if already in the Page Title
            if (removeTabName.Equals(1))
            {
                this.BasePage.Title =
                    this.BasePage.Title.Replace(
                        $"> {this.CurrentPortalSettings.ActiveTab.TabName}",
                        string.Empty);
            }

            BreadCrumbHelper.UpdateDnnBreadCrumb(this, "dnnBreadcrumb", this.PortalSettings);
        }

        /// <summary>
        /// The initialize component.
        /// </summary>
        private void InitializeComponent()
        {
            if (AJAX.IsInstalled())
            {
                AJAX.RegisterScriptManager();
            }

            this.forum1 = new Forum();

            this.pnlModuleContent.Controls.Add(this.forum1);

            this.Load += this.DotNetNukeModuleLoad;
            this.forum1.PageTitleSet += this.Forum1_PageTitleSet;

            // This will create an error if there is no setting for forumboardid
            if (this.Settings["forumboardid"] != null)
            {
                this.forum1.BoardID = this.Settings["forumboardid"].ToType<int>();

                var boardSettingsTabId = BoardContext.Current.BoardSettings != null
                                         && BoardContext.Current.BoardSettings.BoardID.Equals(this.forum1.BoardID)
                                             ? BoardContext.Current.BoardSettings.DNNPageTab
                                             : new LoadBoardSettings(this.forum1.BoardID).DNNPageTab;

                if (boardSettingsTabId.Equals(-1)
                    || !boardSettingsTabId.Equals(this.TabId) && !this.CurrentPortalSettings.ContentLocalizationEnabled)
                {
                    if (HttpContext.Current.User.Identity.IsAuthenticated
                        && UserController.Instance.GetCurrentUserInfo().IsSuperUser)
                    {
                        this.Response.Redirect(
                            this.ResolveUrl(
                                $"~/tabid/{this.PortalSettings.ActiveTab.TabID}/ctl/Edit/mid/{this.ModuleId}/Default.aspx"));
                    }

                    /*else
                                        {
                                            boardSettings.DNNPageTab = this.TabId;
                    
                                            // save the settings to the database
                                            boardSettings.SaveRegistry();
                    
                                            // Reload forum settings
                                            BoardContext.Current.BoardSettings = null;
                                        }*/
                }

                if (BoardContext.Current.BoardSettings.DNNPortalId.Equals(-1))
                {
                    var boardSettings = new LoadBoardSettings(this.forum1.BoardID)
                                            {
                                                DNNPageTab = this.TabId,
                                                DNNPortalId = this.PortalId
                                            };

                    // save the settings to the database
                    boardSettings.SaveRegistry();
                }

                // Inherit Language from Dnn?
                var inheritDnnLanguage = true;

                if (this.Settings["InheritDnnLanguage"] != null)
                {
                    inheritDnnLanguage = this.Settings["InheritDnnLanguage"].ToType<bool>();
                }

                if (inheritDnnLanguage)
                {
                    SetDnnLangToYaf();
                }
            }
            else
            {
                if (HttpContext.Current.User.Identity.IsAuthenticated
                    && UserController.Instance.GetCurrentUserInfo().IsSuperUser)
                {
                    this.Response.Redirect(
                        this.ResolveUrl(
                            $"~/tabid/{this.PortalSettings.ActiveTab.TabID}/ctl/Edit/mid/{this.ModuleId}/Default.aspx"));
                }
                else
                {
                    this.pnlModuleContent.Controls.Clear();

                    this.pnlModuleContent.Controls.Add(
                        new Literal
                            {
                                Text =
                                    "<div class=\"dnnFormMessage dnnFormInfo\">Please login as Superuser (host) and Setup the forum first.</div>"
                            });
                }
            }
        }

        #endregion
    }
}