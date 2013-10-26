/* Yet Another Forum.NET
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
    #region

    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Web;
    using System.Web.Security;
    using System.Web.UI;

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
    using YAF.Core.Model;
    using YAF.DotNetNuke.Components.Objects;
    using YAF.DotNetNuke.Components.Utils;
    using YAF.Types;
    using YAF.Types.Attributes;
    using YAF.Types.Extensions;
    using YAF.Types.Interfaces;
    using YAF.Types.Models;

    #endregion

    /// <summary>
    /// The DotNetNuke Module Class.
    /// </summary>
    public partial class YafDnnModule : PortalModuleBase, IActionable, IHaveServiceLocator
    {
        #region Constants and Fields

        /// <summary>
        /// The _create new board.
        /// </summary>
        private bool createNewBoard;

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
                            Localization.GetString("EditYafSettings.Text", this.LocalResourceFile), 
                            ModuleActionType.AddContent, string.Empty, string.Empty, this.EditUrl(), 
                            false, 
                            SecurityAccessLevel.Host,
                            true,
                            false
                            },
                        {
                            this.GetNextActionID(), Localization.GetString("UserImporter.Text", this.LocalResourceFile),
                            ModuleActionType.AddContent, string.Empty, string.Empty, this.EditUrl("Import"), false,
                            SecurityAccessLevel.Host,
                            true,
                            false }
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
                    this.forum1.BoardID, this.UserId, this.CurrentPortalSettings.PortalId);
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

            this.Get<ILogger>().Error(x, "Error on the DNN Module");

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
        /// Creates the new board.
        /// </summary>
        /// <param name="dnnUserInfo">The DNN user info.</param>
        /// <param name="dnnUser">The DNN user.</param>
        private void CreateNewBoard(UserInfo dnnUserInfo, MembershipUser dnnUser)
        {
            if (dnnUserInfo.IsSuperUser)
            {
                // This is HOST and probably the first board.
                // The install routine already created the first board.
                // Make sure Module settings are in place
                var objForumSettings = new ModuleController();

                objForumSettings.UpdateModuleSetting(this.ModuleId, "forumboardid", "1");
                objForumSettings.UpdateModuleSetting(this.ModuleId, "forumcategoryid", string.Empty);

                this.forum1.BoardID = 1;
            }
            else
            {
                // This is an admin adding a new forum.
                var newBoardName = "{0} Forums".FormatWith(this.CurrentPortalSettings.PortalName);

                // Create the board
                var yafCultureInfo = CultureUtilities.GetYafCultureInfo(
                    YafCultures,
                    Localization.GetPageLocale(this.CurrentPortalSettings));

                var largestBoardId = this.GetRepository<Board>()
                                               .Create(
                                                   newBoardName,
                                                   yafCultureInfo.Culture,
                                                   yafCultureInfo.LanguageFile,
                                                   "DotNetNuke",
                                                   "DotNetNuke",
                                                   dnnUserInfo.Username,
                                                   dnnUserInfo.Email,
                                                   dnnUser.ProviderUserKey.ToString(),
                                                   false,
                                                   string.Empty);

                // Assign the new forum to this module
               var objForumSettings = new ModuleController();

                objForumSettings.UpdateModuleSetting(this.ModuleId, "forumboardid", largestBoardId.ToString());
                objForumSettings.UpdateModuleSetting(this.ModuleId, "forumcategoryid", string.Empty);

                this.forum1.BoardID = largestBoardId;
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

            RoleSyncronizer.SynchronizeUserRoles(this.forum1.BoardID, this.CurrentPortalSettings.PortalId, yafUserId, dnnUser);

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

            // Admin or Host user?
            if ((dnnUserInfo.IsSuperUser || dnnUserInfo.UserID == this.CurrentPortalSettings.AdministratorId) &&
                this.createNewBoard)
            {
                this.CreateNewBoard(dnnUserInfo, dnnMembershipUser);
            }

            // Check if the user exists in yaf
            var yafUserId = LegacyDb.user_get(this.forum1.BoardID, dnnMembershipUser.ProviderUserKey);

            if (yafUserId.Equals(0))
            {
                yafUserId = UserImporter.CreateYafUser(
                    dnnUserInfo,
                    dnnMembershipUser,
                    this.forum1.BoardID,
                    this.CurrentPortalSettings,
                    this.Get<YafBoardSettings>());

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
                    this.forum1.BoardID);
            }
        }

        /// <summary>
        /// Change Page Title
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ForumPageTitleArgs" /> instance containing the event data.</param>
        private void Forum1_PageTitleSet(object sender, ForumPageTitleArgs e)
        {
            int removeTabName = 1;

            if (this.Settings["RemoveTabName"] != null)
            {
                removeTabName = this.Settings["RemoveTabName"].ToType<int>();
            }

            // Check if Tab Name/Title Matches the Forum Board Name
            if (removeTabName.Equals(2))
            {
                if (this.Get<YafBoardSettings>().Name.Equals(this.CurrentPortalSettings.ActiveTab.TabName))
                {
                    removeTabName = 1;
                }

                if (this.CurrentPortalSettings.ActiveTab.Title.IsSet())
                {
                    if (
                        this.Get<YafBoardSettings>().Name.Equals(
                            this.CurrentPortalSettings.ActiveTab.Title))
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
                        "> {0}".FormatWith(this.CurrentPortalSettings.ActiveTab.TabName), string.Empty);
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

            // Get current BoardID
            try
            {
                this.createNewBoard = false;

                // This will create an error if there is no setting for forumboardid
                if (this.Settings["forumboardid"] != null)
                {
                    this.forum1.BoardID = this.Settings["forumboardid"].ToType<int>();
                }
                else
                {
                    // Create a new board
                    this.createNewBoard = true;
                }
                
            }
            catch (Exception)
            {
                // A forum does not exist for this module
                // Create a new board
                this.createNewBoard = true;
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

        #endregion
    }
}