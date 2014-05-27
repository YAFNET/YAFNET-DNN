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

    using global::DotNetNuke.Security;

    using global::DotNetNuke.Services.Exceptions;

    using global::DotNetNuke.Services.Localization;

    using YAF.Classes;
    using YAF.Classes.Data;
    using YAF.Core;
    using YAF.DotNetNuke.Components.Objects;
    using YAF.DotNetNuke.Components.Utils;
    using YAF.Types;
    using YAF.Types.Attributes;
    using YAF.Types.Extensions;
    using YAF.Types.Interfaces;

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
        /// Gets or sets the Service Locator.
        /// </summary>
        [Inject]
        public IServiceLocator ServiceLocator { get; set; }

        /// <summary>
        ///  Gets Add Menu Entries to Module Container
        /// </summary>
        public ModuleActionCollection ModuleActions
        {
            get
            {
                ModuleActionCollection actions = new ModuleActionCollection
                                                     {
                                                         {
                                                             this.GetNextActionID(),
                                                             Localization.GetString(
                                                                 "EditYafSettings.Text",
                                                                 this.LocalResourceFile),
                                                             ModuleActionType.AddContent,
                                                             string.Empty, string.Empty,
                                                             this.EditUrl(), false,
                                                             SecurityAccessLevel.Host, true, false
                                                         },
                                                         {
                                                             this.GetNextActionID(),
                                                             Localization.GetString(
                                                                 "UserImporter.Text",
                                                                 this.LocalResourceFile),
                                                             ModuleActionType.AddContent,
                                                             string.Empty, string.Empty,
                                                             this.EditUrl("Import"), false,
                                                             SecurityAccessLevel.Host, true, false
                                                         }
                                                     };

                return actions;
            }
        }

        /// <summary>
        /// Gets the session user key name.
        /// </summary>
        public string SessionUserKeyName
        {
            get
            {
                return "yaf_dnn_boardid{0}_userid{1}_portalid{2}".FormatWith(
                    this.forum1.BoardID,
                    this.UserId,
                    this.CurrentPortalSettings.PortalId);
            }
        }

        /// <summary>
        /// Gets the Base Page
        /// </summary>
        public CDefault BasePage
        {
            get
            {
                return this.basePage ?? (this.basePage = GetDefault(this));
            }
        }

        /// <summary>
        /// Gets the YAF Cultures
        /// </summary>
        private static List<YafCultureInfo> YafCultures
        {
            get
            {
                const string CACHEKEY = "YAF_Cultures";

                List<YafCultureInfo> cultures;

                if (DataCache.GetCache(CACHEKEY) is List<YafCultureInfo>)
                {
                    cultures = DataCache.GetCache(CACHEKEY).ToType<List<YafCultureInfo>>();

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
        /// Gets CurrentPortalSettings.
        /// </summary>
        private PortalSettings CurrentPortalSettings
        {
            get
            {
                return this.portalSettings ?? (this.portalSettings = PortalController.GetCurrentPortalSettings());
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.TemplateControl.Error" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> that contains the event data.</param>
        protected override void OnError(EventArgs e)
        {
            Exception x = this.Server.GetLastError();

            YafContext.Current.Get<ILogger>().Error(x, "Error on the DNN Module");

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
            // setup jQuery and Jquery Ui Tabs.
            jQuery.RequestRegistration();
            jQuery.RequestUIRegistration();

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
                Control parent = control.Parent;

                if (parent == null)
                {
                    return null;
                }

                var cDefault = parent as CDefault;

                if (cDefault != null)
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

                YafContext.Current.Get<YafBoardSettings>().Language =
                    YafCultures.Find(yafCult => yafCult.Culture.Equals(langCode)) != null
                        ? YafCultures.Find(yafCult => yafCult.Culture.Equals(langCode)).LanguageFile
                        : "english.xml";
            }
            catch (Exception)
            {
                YafContext.Current.Get<YafBoardSettings>().Language = "english.xml";
            }
        }

        /// <summary>
        /// Handles the Load event of the DotNetNukeModule control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void DotNetNukeModule_Load(object sender, EventArgs e)
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
            if (this.Session["{0}_rolesloaded".FormatWith(this.SessionUserKeyName)] != null)
            {
                return;
            }

            RoleSyncronizer.SynchronizeUserRoles(
                this.forum1.BoardID,
                this.CurrentPortalSettings.PortalId,
                yafUserId,
                dnnUser);

            this.Session["{0}_rolesloaded".FormatWith(this.SessionUserKeyName)] = true;
        }

        /// <summary>
        /// Check if the DNN User exists in YAF, and if the Profile is up to date.
        /// </summary>
        private void CreateOrUpdateUser()
        {
            // Get current DNN user
            var dnnUserInfo = UserController.GetCurrentUserInfo();

            // get the user from the membership provider
            MembershipUser dnnMembershipUser = Membership.GetUser(dnnUserInfo.Username, true);

            if (dnnMembershipUser == null)
            {
                return;
            }

            // Check if the user exists in yaf
            var yafUserId = LegacyDb.user_get(this.forum1.BoardID, dnnMembershipUser.ProviderUserKey);

            var boardSettings = YafContext.Current == null
                                    ? new YafLoadBoardSettings(this.forum1.BoardID)
                                    : YafContext.Current.Get<YafBoardSettings>();

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
                    UserImporter.CreateYafHostUser(yafUserId, this.forum1.BoardID);
                }
            }
            else
            {
                this.CheckForRoles(dnnUserInfo, yafUserId);

                ProfileSyncronizer.UpdateUserProfile(
                    yafUserId,
                    YafContext.Current.Profile,
                    YafContext.Current.CurrentUserData,
                    dnnUserInfo,
                    dnnMembershipUser,
                    this.CurrentPortalSettings.PortalId,
                    this.CurrentPortalSettings.GUID,
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
            if (removeTabName.Equals(2) && YafContext.Current != null)
            {
                if (YafContext.Current.Get<YafBoardSettings>().Name.Equals(this.CurrentPortalSettings.ActiveTab.TabName))
                {
                    removeTabName = 1;
                }

                if (this.CurrentPortalSettings.ActiveTab.Title.IsSet())
                {
                    if (
                        YafContext.Current.Get<YafBoardSettings>()
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
                        "> {0}".FormatWith(this.CurrentPortalSettings.ActiveTab.TabName),
                        string.Empty);
            }

            BreadCrumbHelper.UpdateDnnBreadCrumb(this, "dnnBreadcrumb");
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

            this.Load += this.DotNetNukeModule_Load;
            this.forum1.PageTitleSet += this.Forum1_PageTitleSet;

            // This will create an error if there is no setting for forumboardid
            if (this.Settings["forumboardid"] != null)
            {
                this.forum1.BoardID = this.Settings["forumboardid"].ToType<int>();

                var boardSettings = new YafLoadBoardSettings(this.forum1.BoardID);

                if (boardSettings.DNNPageTab.Equals(-1) || !boardSettings.DNNPageTab.Equals(this.TabId))
                {
                    if (HttpContext.Current.User.Identity.IsAuthenticated
                        && UserController.GetCurrentUserInfo().IsSuperUser)
                    {
                        this.Response.Redirect(
                            this.ResolveUrl(
                                "~/tabid/{0}/ctl/Edit/mid/{1}/Default.aspx".FormatWith(
                                    this.PortalSettings.ActiveTab.TabID,
                                    this.ModuleId)));
                    }
                    /*else
                    {
                        boardSettings.DNNPageTab = this.TabId;

                        // save the settings to the database
                        boardSettings.SaveRegistry();

                        // Reload forum settings
                        YafContext.Current.BoardSettings = null;
                    }*/
                }

                // Inherit Language from Dnn?
                var ineritDnnLanguage = true;

                if (this.Settings["InheritDnnLanguage"] != null)
                {
                    ineritDnnLanguage = this.Settings["InheritDnnLanguage"].ToType<bool>();
                }

                if (ineritDnnLanguage)
                {
                    SetDnnLangToYaf();
                }

                // Override to set to specifc ID
                if (this.Settings["forumcategoryid"] != null)
                {
                    this.forum1.CategoryID = this.Settings["forumcategoryid"].ToType<int>();
                }
            }
            else
            {
                if (HttpContext.Current.User.Identity.IsAuthenticated && UserController.GetCurrentUserInfo().IsSuperUser)
                {
                    this.Response.Redirect(
                        this.ResolveUrl(
                            "~/tabid/{0}/ctl/Edit/mid/{1}/Default.aspx".FormatWith(
                                this.PortalSettings.ActiveTab.TabID,
                                this.ModuleId)));
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