/* Yet Another Forum.NET
 * Copyright (C) 2003-2005 Bjørnar Henden
 * Copyright (C) 2006-2013 Jaben Cargman
 * Copyright (C) 2014-2016 Ingo Herbote
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
    using System.Data;
    using System.IO;
    using System.Web;
    using System.Web.Security;
    using System.Web.UI.WebControls;

    using global::DotNetNuke.Common;
    using global::DotNetNuke.Entities.Modules;
    using global::DotNetNuke.Services.Localization;

    using YAF.Classes;
    using YAF.Core;
    using YAF.Core.Model;
    using YAF.Core.Services.Import;
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

            if (this.IsPostBack)
            {
                return;
            }

            using (DataTable dt = YafContext.Current.GetRepository<Board>().List())
            {
                this.BoardID.DataSource = dt;
                this.BoardID.DataTextField = "Name";
                this.BoardID.DataValueField = "BoardID";
                this.BoardID.DataBind();
                if (this.Settings["forumboardid"] != null)
                {
                    ListItem item = this.BoardID.Items.FindByValue(this.Settings["forumboardid"].ToString());
                    if (item != null)
                    {
                        item.Selected = true;
                    }
                }
            }

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
            bool ineritDnnLang = true;

            if ((string)this.Settings["InheritDnnLanguage"] != null)
            {
                bool.TryParse((string)this.Settings["InheritDnnLanguage"], out ineritDnnLang);
            }

            this.InheritDnnLanguage.Checked = ineritDnnLang;
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

            var boardSettings = new YafLoadBoardSettings(this.BoardID.SelectedValue.ToType<int>())
                                    {
                                        DNNPageTab = this.TabId,
                                        DNNPortalId = this.PortalId,
                                        BaseUrlMask =
                                            "http://{0}/".FormatWith(
                                                HttpContext.Current.Request.ServerVariables["SERVER_NAME"])
                                    };

            // save the settings to the database
            boardSettings.SaveRegistry();

            // Reload forum settings
            YafContext.Current.BoardSettings = null;

            YafBuildLink.Redirect(ForumPages.forum);
        }

        /// <summary>
        /// The create board.
        /// </summary>
        /// <returns></returns>
       private int CreateBoard()
        {
                // new admin
                MembershipUser newAdmin = UserMembershipHelper.GetUser();

                // Create Board
                var newBoardID = this.CreateBoardDatabase(
                    this.NewBoardName.Text.Trim(),
                    YafContext.Current.Get<MembershipProvider>().ApplicationName,
                    YafContext.Current.Get<RoleProvider>().ApplicationName,
                    "english.xml",
                    newAdmin);


            if (newBoardID > 0 && Config.MultiBoardFolders)
            {
                // Successfully created the new board
                string boardFolder = this.Server.MapPath(Path.Combine(Config.BoardRoot, "{0}/".FormatWith(newBoardID)));

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

            return newBoardID;
        }

        /// <summary>
        /// Creates the board in the database.
        /// </summary>
        /// <param name="boardName">Name of the board.</param>
        /// <param name="boardMembershipAppName">Name of the board membership application.</param>
        /// <param name="boardRolesAppName">Name of the board roles application.</param>
        /// <param name="langFile">The language file.</param>
        /// <param name="newAdmin">The new admin.</param>
        /// <returns></returns>
        private int CreateBoardDatabase(
            string boardName,
            string boardMembershipAppName,
            string boardRolesAppName,
            string langFile,
            MembershipUser newAdmin)
        {
            int newBoardID = YafContext.Current.GetRepository<Board>()
                .Create(
                    boardName,
                    "en-US",
                    langFile,
                    boardMembershipAppName,
                    boardRolesAppName,
                    newAdmin.UserName,
                    newAdmin.Email,
                    newAdmin.ProviderUserKey.ToString(),
                    this.PageContext().IsHostAdmin,
                    Config.CreateDistinctRoles && Config.IsAnyPortal ? "YAF " : string.Empty);

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
            loadWrapper("install/bbCodeExtensions.xml", s => DataImport.BBCodeExtensionImport(newBoardID, s));

            // load default extensions if available...
            loadWrapper("install/fileExtensions.xml", s => DataImport.FileExtensionImport(newBoardID, s));

            // load default topic status if available...
            loadWrapper("install/TopicStatusList.xml", s => DataImport.TopicStatusImport(newBoardID, s));

            // load default spam word if available...
            loadWrapper("install/SpamWords.xml", s => DataImport.SpamWordsImport(newBoardID, s));


            return newBoardID;
        }

        /// <summary>
        /// Handles the OnClick event of the Create control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Create_OnClick(object sender, EventArgs e)
        {
            var newBoardId = this.CreateBoard();

            var moduleController = new ModuleController();

            moduleController.UpdateModuleSetting(this.ModuleId, "forumboardid", newBoardId.ToString());

            moduleController.UpdateModuleSetting(this.ModuleId, "RemoveTabName", this.RemoveTabName.SelectedValue);
            moduleController.UpdateModuleSetting(
                this.ModuleId,
                "InheritDnnLanguage",
                this.InheritDnnLanguage.Checked.ToString());

            var boardSettings = new YafLoadBoardSettings(newBoardId)
                                    {
                                        DNNPageTab = this.TabId,
                                        DNNPortalId = this.PortalId,
                                        BaseUrlMask =
                                            "http://{0}/".FormatWith(
                                                HttpContext.Current.Request.ServerVariables["SERVER_NAME"])
                                    };

            // save the settings to the database
            boardSettings.SaveRegistry();

            // Reload forum settings
            YafContext.Current.BoardSettings = null;

            this.Response.Redirect(Globals.NavigateURL(), true);
        }

        #endregion
    }
}