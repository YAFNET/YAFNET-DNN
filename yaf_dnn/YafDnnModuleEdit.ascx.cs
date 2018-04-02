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

namespace YAF.DotNetNuke
{
    #region Using

    using System;
    using System.IO;
    using System.Linq;
    using System.Web;
    using System.Web.Security;
    using System.Web.UI.WebControls;

    using global::DotNetNuke.Common;
    using global::DotNetNuke.Common.Utilities;
    using global::DotNetNuke.Entities.Modules;
    using global::DotNetNuke.Entities.Tabs;
    using global::DotNetNuke.Services.Localization;

    using YAF.Classes;
    using YAF.Core;
    using YAF.Core.Model;
    using YAF.Core.Services.Import;
    using YAF.DotNetNuke.Components.Controllers;
    using YAF.DotNetNuke.Components.Utils;
    using YAF.Types.Constants;
    using YAF.Types.Extensions;
    using YAF.Types.Interfaces;
    using YAF.Types.Models;
    using YAF.Utils;

    #endregion

    /// <summary>
    /// The YAF Module Settings Page
    /// </summary>
    public partial class YafDnnModuleEdit : PortalModuleBase
    {
        #region Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit(EventArgs e)
        {
            this.Load += this.DotNetNukeModuleEdit_Load;
            this.update.Click += this.UpdateClick;
            this.cancel.Click += this.CancelClick;

            base.OnInit(e);
        }

        /// <summary>
        /// Handles the OnClick event of the ImportForums control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void ImportForums_OnClick(object sender, EventArgs e)
        {
            // First Create new empty forum
            var newBoardId = this.CreateBoard(false, "Import Board");

            Data.ImportActiveForums(this.ActiveForums.SelectedValue.ToType<int>(), newBoardId);

            this.MigrateAttachments();

            var moduleController = new ModuleController();

            moduleController.UpdateModuleSetting(this.ModuleId, "forumboardid", (newBoardId + 1).ToString());

            moduleController.UpdateModuleSetting(this.ModuleId, "RemoveTabName", this.RemoveTabName.SelectedValue);
            moduleController.UpdateModuleSetting(
                this.ModuleId,
                "InheritDnnLanguage",
                this.InheritDnnLanguage.Checked.ToString());

            var boardSettings =
                new YafLoadBoardSettings(newBoardId + 1)
                    {
                        DNNPageTab = this.TabId,
                        DNNPortalId = this.PortalId,
                        BaseUrlMask =
                            "http://{0}/".FormatWith(
                                HttpContext.Current.Request.ServerVariables[
                                    "SERVER_NAME"])
                    };

            // save the settings to the database
            boardSettings.SaveRegistry();

            // Reload forum settings
            YafContext.Current.BoardSettings = null;

            this.Response.Redirect(Globals.NavigateURL(), true);
        }

        /// <summary>
        /// Handles the OnClick event of the Create control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Create_OnClick(object sender, EventArgs e)
        {
            var newBoardId = this.CreateBoard(true, this.NewBoardName.Text.Trim());

            var moduleController = new ModuleController();

            moduleController.UpdateModuleSetting(this.ModuleId, "forumboardid", newBoardId.ToString());

            moduleController.UpdateModuleSetting(this.ModuleId, "RemoveTabName", this.RemoveTabName.SelectedValue);
            moduleController.UpdateModuleSetting(
                this.ModuleId,
                "InheritDnnLanguage",
                this.InheritDnnLanguage.Checked.ToString());

            var boardSettings =
                new YafLoadBoardSettings(newBoardId)
                    {
                        DNNPageTab = this.TabId,
                        DNNPortalId = this.PortalId,
                        BaseUrlMask =
                            "http://{0}/".FormatWith(
                                HttpContext.Current.Request.ServerVariables[
                                    "SERVER_NAME"])
                    };

            // save the settings to the database
            boardSettings.SaveRegistry();

            // Reload forum settings
            YafContext.Current.BoardSettings = null;

            this.Response.Redirect(Globals.NavigateURL(), true);
        }

        /// <summary>
        /// Cancel Editing.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.EventArgs" /> instance containing the event data.</param>
        private void CancelClick(object sender, EventArgs e)
        {
            this.Response.Redirect(Globals.NavigateURL(), true);
        }

        /// <summary>
        /// The dot net nuke module edit_ load.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void DotNetNukeModuleEdit_Load(object sender, EventArgs e)
        {
            this.update.Text = Localization.GetString("Update.Text", this.LocalResourceFile);
            this.cancel.Text = Localization.GetString("Cancel.Text", this.LocalResourceFile);
            this.create.Text = Localization.GetString("Create.Text", this.LocalResourceFile);
            this.ImportForums.Text = Localization.GetString("Import.Text", this.LocalResourceFile);

            if (this.IsPostBack)
            {
                return;
            }

            using (var dt = YafContext.Current.GetRepository<Board>().List())
            {
                this.BoardID.DataSource = dt;
                this.BoardID.DataTextField = "Name";
                this.BoardID.DataValueField = "BoardID";
                this.BoardID.DataBind();
                if (this.Settings["forumboardid"] != null)
                {
                    var item = this.BoardID.Items.FindByValue(this.Settings["forumboardid"].ToString());
                    if (item != null)
                    {
                        item.Selected = true;
                    }
                }
            }

            this.FillActiveForumsList();

            // Load Remove Tab Name Setting
            this.RemoveTabName.Items.Add(
                new ListItem(Localization.GetString("RemoveTabName0.Text", this.LocalResourceFile), "0"));
            this.RemoveTabName.Items.Add(
                new ListItem(Localization.GetString("RemoveTabName1.Text", this.LocalResourceFile), "1"));
            this.RemoveTabName.Items.Add(
                new ListItem(Localization.GetString("RemoveTabName2.Text", this.LocalResourceFile), "2"));

            this.RemoveTabName.SelectedValue = this.Settings["RemoveTabName"] != null
                                                   ? this.Settings["RemoveTabName"].ToType<string>()
                                                   : "1";

            // Load Inherit DNN Language Setting
            var ineritDnnLang = true;

            if ((string)this.Settings["InheritDnnLanguage"] != null)
            {
                bool.TryParse((string)this.Settings["InheritDnnLanguage"], out ineritDnnLang);
            }

            this.InheritDnnLanguage.Checked = ineritDnnLang;
        }

        /// <summary>
        /// Fills the active forums list.
        /// </summary>
        private void FillActiveForumsList()
        {
            var objTabController = new TabController();

            var objDesktopModuleInfo =
                DesktopModuleController.GetDesktopModuleByModuleName("Active Forums", this.PortalId);

            if (objDesktopModuleInfo == null)
            {
                this.ActiveForumsPlaceHolder.Visible = false;
                return;
            }

            var tabs = TabController.GetPortalTabs(this.PortalSettings.PortalId, -1, true, true);

            foreach (var tabInfo in tabs.Where(tab => !tab.IsDeleted))
            {
                var moduleController = new ModuleController();

                foreach (var pair in moduleController.GetTabModules(tabInfo.TabID))
                {
                    var moduleInfo = pair.Value;

                    if (moduleInfo.IsDeleted)
                    {
                        continue;
                    }

                    if (moduleInfo.DesktopModuleID != objDesktopModuleInfo.DesktopModuleID)
                    {
                        continue;
                    }

                    var path = tabInfo.TabName;
                    var tabSelected = tabInfo;

                    while (tabSelected.ParentId != Null.NullInteger)
                    {
                        tabSelected = objTabController.GetTab(tabSelected.ParentId, tabInfo.PortalID, false);
                        if (tabSelected == null)
                        {
                            break;
                        }

                        path = "{0} -> {1}".FormatWith(tabSelected.TabName, path);
                    }

                    var objListItem = new ListItem
                                          {
                                              Value = moduleInfo.ModuleID.ToString(),
                                              Text = "{0} -> {1}".FormatWith(path, moduleInfo.ModuleTitle)
                                          };

                    this.ActiveForums.Items.Add(objListItem);
                }
            }

            if (this.ActiveForums.Items.Count == 0)
            {
                this.ActiveForumsPlaceHolder.Visible = false;
            }
        }

        /// <summary>
        /// Save the Settings
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void UpdateClick(object sender, EventArgs e)
        {
            var moduleController = new ModuleController();

            moduleController.UpdateModuleSetting(this.ModuleId, "forumboardid", this.BoardID.SelectedValue);

            moduleController.UpdateModuleSetting(this.ModuleId, "RemoveTabName", this.RemoveTabName.SelectedValue);
            moduleController.UpdateModuleSetting(
                this.ModuleId,
                "InheritDnnLanguage",
                this.InheritDnnLanguage.Checked.ToString());

            var boardSettings =
                new YafLoadBoardSettings(this.BoardID.SelectedValue.ToType<int>())
                    {
                        DNNPageTab = this.TabId,
                        DNNPortalId = this.PortalId,
                        BaseUrlMask =
                            "http://{0}/".FormatWith(
                                HttpContext.Current
                                    .Request
                                    .ServerVariables[
                                        "SERVER_NAME"])
                    };

            // save the settings to the database
            boardSettings.SaveRegistry();

            string message;

            // Import Users & Roles
            UserImporter.ImportUsers(
                this.BoardID.SelectedValue.ToType<int>(),
                this.PortalSettings.PortalId,
                this.PortalSettings.GUID,
                out message);

            // Reload forum settings
            YafContext.Current.BoardSettings = null;

            YafBuildLink.Redirect(ForumPages.forum);
        }

        /// <summary>
        /// The create board.
        /// </summary>
        /// <param name="importUsers">if set to <c>true</c> [import users].</param>
        /// <param name="BoardName">Name of the board.</param>
        /// <returns>
        /// Returns the Board ID of the new Board.
        /// </returns>
        private int CreateBoard(bool importUsers, string boardName)
        {
            // new admin
            var newAdmin = UserMembershipHelper.GetUser();

            // Create Board
            var newBoardId = this.CreateBoardDatabase(
                boardName,
                YafContext.Current.Get<MembershipProvider>().ApplicationName,
                YafContext.Current.Get<RoleProvider>().ApplicationName,
                "english.xml",
                newAdmin);

            if (newBoardId > 0 && global::YAF.Classes.Config.MultiBoardFolders)
            {
                // Successfully created the new board
                var boardFolder = this.Server.MapPath(
                    Path.Combine(global::YAF.Classes.Config.BoardRoot, "{0}/".FormatWith(newBoardId)));

                // Create New Folders.
                if (!Directory.Exists(Path.Combine(boardFolder, "Images")))
                {
                    // Create the Images Folders
                    Directory.CreateDirectory(Path.Combine(boardFolder, "Images"));

                    // Create Sub Folders
                    Directory.CreateDirectory(Path.Combine(boardFolder, "Images\\Avatars"));
                    Directory.CreateDirectory(Path.Combine(boardFolder, "Images\\Categories"));
                    Directory.CreateDirectory(Path.Combine(boardFolder, "Images\\Forums"));
                    Directory.CreateDirectory(Path.Combine(boardFolder, "Images\\Emoticons"));
                    Directory.CreateDirectory(Path.Combine(boardFolder, "Images\\Medals"));
                    Directory.CreateDirectory(Path.Combine(boardFolder, "Images\\Ranks"));
                }

                if (!Directory.Exists(Path.Combine(boardFolder, "Themes")))
                {
                    Directory.CreateDirectory(Path.Combine(boardFolder, "Themes"));

                    // Need to copy default theme to the Themes Folder
                }

                if (!Directory.Exists(Path.Combine(boardFolder, "Uploads")))
                {
                    Directory.CreateDirectory(Path.Combine(boardFolder, "Uploads"));
                }
            }

            // Import Users & Roles
            if (importUsers)
            {
                string message;
                UserImporter.ImportUsers(
                    newBoardId,
                    this.PortalSettings.PortalId,
                    this.PortalSettings.GUID,
                    out message);
            }

            return newBoardId;
        }

        /// <summary>
        /// Creates the board in the database.
        /// </summary>
        /// <param name="boardName">Name of the board.</param>
        /// <param name="boardMembershipAppName">Name of the board membership application.</param>
        /// <param name="boardRolesAppName">Name of the board roles application.</param>
        /// <param name="langFile">The language file.</param>
        /// <param name="newAdmin">The new admin.</param>
        /// <returns>Returns the Board ID of the new Board.</returns>
        private int CreateBoardDatabase(
            string boardName,
            string boardMembershipAppName,
            string boardRolesAppName,
            string langFile,
            MembershipUser newAdmin)
        {
            var newBoardId = YafContext.Current.GetRepository<Board>()
                .Create(
                    boardName,
                    "en-US",
                    langFile,
                    boardMembershipAppName,
                    boardRolesAppName,
                    newAdmin.UserName,
                    newAdmin.Email,
                    newAdmin.ProviderUserKey.ToString(),
                    this.PortalSettings.UserInfo.IsSuperUser,
                    global::YAF.Classes.Config.CreateDistinctRoles && global::YAF.Classes.Config.IsAnyPortal
                        ? "YAF "
                        : string.Empty);

            var loadWrapper = new Action<string, Action<Stream>>(
                (file, streamAction) =>
                    {
                        var fullFile = YafContext.Current.Get<HttpRequestBase>().MapPath(file);

                        if (!File.Exists(fullFile))
                        {
                            return;
                        }

                        // import into board...
                        using (var stream = new StreamReader(fullFile))
                        {
                            streamAction(stream.BaseStream);
                            stream.Close();
                        }
                    });

            // load default bbcode if available...
            loadWrapper("install/bbCodeExtensions.xml", s => DataImport.BBCodeExtensionImport(newBoardId, s));

            // load default extensions if available...
            loadWrapper("install/fileExtensions.xml", s => DataImport.FileExtensionImport(newBoardId, s));

            // load default topic status if available...
            loadWrapper("install/TopicStatusList.xml", s => DataImport.TopicStatusImport(newBoardId, s));

            // load default spam word if available...
            loadWrapper("install/SpamWords.xml", s => DataImport.SpamWordsImport(newBoardId, s));

            return newBoardId;
        }

        /// <summary>
        /// Migrates the Active Forums Attachments.
        /// </summary>
        private void MigrateAttachments()
        {
            var attachActiveFolderPath = Path.Combine(this.PortalSettings.HomeDirectoryMapPath, "activeforums_Upload");
            var attachYafFolderPath = HttpContext.Current.Request.MapPath(
                Path.Combine("DesktopModules\\YetAnotherForumDotNet", YafBoardFolders.Current.Uploads));

            if (Directory.Exists(attachActiveFolderPath))
            {
                foreach (var attachment in Directory.GetFiles(attachActiveFolderPath))
                {
                    var fileName = Path.GetFileName(attachment);
                    File.Copy(attachment, "{0}\\{1}.yafupload".FormatWith(attachYafFolderPath, fileName));
                }
            }
        }

        #endregion
    }
}