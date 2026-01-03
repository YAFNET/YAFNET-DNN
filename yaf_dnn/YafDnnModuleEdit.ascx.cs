/* Yet Another Forum.NET
 * Copyright (C) 2003-2005 Bjørnar Henden
 * Copyright (C) 2006-2013 Jaben Cargman
 * Copyright (C) 2014-2026 Ingo Herbote
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

namespace YAF.DotNetNuke;

using System.Web.UI.WebControls;

using global::DotNetNuke.Abstractions.Application;
using global::DotNetNuke.Common.Utilities;

using YAF.Core.Services.Import;

/// <summary>
/// The YAF Module Settings Page
/// </summary>
public partial class YafDnnModuleEdit : PortalModuleBase, IHaveServiceLocator
{
    /// <summary>
    /// The navigation manager.
    /// </summary>
    private readonly INavigationManager navigationManager;

    private readonly IApplicationStatusInfo applicationStatusInfo;

    /// <summary>
    ///     Gets or sets the service locator.
    /// </summary>
    public IServiceLocator ServiceLocator { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="YafDnnModuleEdit"/> class.
    /// </summary>
    public YafDnnModuleEdit()
    {
        this.ServiceLocator = BoardContext.Current.ServiceLocator;
        this.navigationManager = this.DependencyProvider.GetRequiredService<INavigationManager>();
        this.applicationStatusInfo = this.DependencyProvider.GetRequiredService<IApplicationStatusInfo>();
    }

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
        var moduleController = new ModuleController();

        var tabModule = moduleController.GetModule(this.ActiveForums.SelectedValue.ToType<int>());

        // First Create new empty forum
        var newBoardId = this.CreateBoard(false, tabModule.ModuleTitle);

        DataController.ImportActiveForums(
            this.ActiveForums.SelectedValue.ToType<int>(),
            newBoardId,
            this.PortalSettings);

        this.MigrateAttachments();

        this.FixLastForumTopic(newBoardId);

        moduleController.UpdateModuleSetting(this.ModuleId, "forumboardid", newBoardId.ToString());

        moduleController.UpdateModuleSetting(this.ModuleId, "RemoveTabName", this.RemoveTabName.SelectedValue);
        moduleController.UpdateModuleSetting(
            this.ModuleId,
            "InheritDnnLanguage",
            this.InheritDnnLanguage.Checked.ToString());

        var boardSettings = this.Get<BoardSettingsService>().LoadBoardSettings(newBoardId, null);

        boardSettings.DNNPageTab = this.TabId;
        boardSettings.DNNPortalId = this.PortalId;
        boardSettings.BaseUrlMask = $"http://{HttpContext.Current.Request.ServerVariables["SERVER_NAME"]}/";

        // save the settings to the database
        this.Get<BoardSettingsService>().SaveRegistry(boardSettings);

        // Reload forum settings
        BoardContext.Current.BoardSettings = boardSettings;

        this.Response.Redirect(this.navigationManager.NavigateURL(), true);
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

        var boardSettings = this.Get<BoardSettingsService>().LoadBoardSettings(newBoardId, null);

        boardSettings.DNNPageTab = this.TabId;
        boardSettings.DNNPortalId = this.PortalId;
        boardSettings.BaseUrlMask = $"http://{HttpContext.Current.Request.ServerVariables["SERVER_NAME"]}/";

        // save the settings to the database
        this.Get<BoardSettingsService>().SaveRegistry(boardSettings);

        Config.Touch(this.applicationStatusInfo);

        this.Response.Redirect(this.navigationManager.NavigateURL(this.TabId), true);
    }

    /// <summary>
    /// Cancel Editing.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="System.EventArgs" /> instance containing the event data.</param>
    private void CancelClick(object sender, EventArgs e)
    {
        this.Response.Redirect(this.navigationManager.NavigateURL(), true);
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

        var dt = this.GetRepository<Board>().GetAll();

        this.BoardID.DataSource = dt;
        this.BoardID.DataTextField = "Name";
        this.BoardID.DataValueField = "ID";

        this.BoardID.DataBind();

        if (this.Settings["forumboardid"] != null)
        {
            var item = this.BoardID.Items.FindByValue(this.Settings["forumboardid"].ToString());
            if (item != null)
            {
                item.Selected = true;
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
        var inheritDnnLang = true;

        if ((string)this.Settings["InheritDnnLanguage"] != null)
        {
            bool.TryParse((string)this.Settings["InheritDnnLanguage"], out inheritDnnLang);
        }

        this.InheritDnnLanguage.Checked = inheritDnnLang;
    }

    /// <summary>
    /// Fills the active forums list.
    /// </summary>
    private void FillActiveForumsList()
    {
        var objTabController = new TabController();

        var objDesktopModuleInfo =
            DesktopModuleController.GetDesktopModuleByModuleName("Active Forums", this.PortalId);

        if (objDesktopModuleInfo is null)
        {
            this.ActiveForumsPlaceHolder.Visible = false;
            return;
        }

        var tabs = TabController.GetPortalTabs(this.PortalSettings.PortalId, -1, true, true);

        tabs.Where(tab => !tab.IsDeleted).ForEach(
            tabInfo =>
                {
                    var moduleController = new ModuleController();

                    var tabModules = moduleController.GetTabModules(tabInfo.TabID).Select(pair => pair.Value).Where(
                        m => !m.IsDeleted && m.DesktopModuleID == objDesktopModuleInfo.DesktopModuleID);

                    tabModules.ForEach(
                        moduleInfo =>
                            {
                                var path = tabInfo.TabName;
                                var tabSelected = tabInfo;

                                while (tabSelected.ParentId != Null.NullInteger)
                                {
                                    tabSelected = objTabController.GetTab(tabSelected.ParentId, tabInfo.PortalID, false);
                                    if (tabSelected is null)
                                    {
                                        break;
                                    }

                                    path = $"{tabSelected.TabName} -> {path}";
                                }

                                var objListItem = new ListItem
                                                      {
                                                          Value = moduleInfo.ModuleID.ToString(), Text = $@"{path} -> {moduleInfo.ModuleTitle}"
                                                      };

                                this.ActiveForums.Items.Add(objListItem);
                            });
                });

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

        var boardSettings = this.Get<BoardSettingsService>()
            .LoadBoardSettings(this.BoardID.SelectedValue.ToType<int>(), null);

        boardSettings.DNNPageTab = this.TabId;
        boardSettings.DNNPortalId = this.PortalId;
        boardSettings.BaseUrlMask = $"http://{HttpContext.Current.Request.ServerVariables["SERVER_NAME"]}/";

        // save the settings to the database
        this.Get<BoardSettingsService>().SaveRegistry(boardSettings);

        // Import Users & Roles
        UserImporter.ImportUsers(
            this.BoardID.SelectedValue.ToType<int>(),
            this.PortalSettings.PortalId,
            out _);

        Config.Touch(this.applicationStatusInfo);

        this.Response.Redirect(this.navigationManager.NavigateURL(this.TabId), true);
    }

    /// <summary>
    /// The create board.
    /// </summary>
    /// <param name="importUsers">if set to <c>true</c> [import users].</param>
    /// <param name="boardName">Name of the board.</param>
    /// <returns>
    /// Returns the Board ID of the new Board.
    /// </returns>
    private int CreateBoard(bool importUsers, string boardName)
    {
        // new admin
        var newAdmin = UserController.Instance.GetCurrentUserInfo().ToAspNetUsers();

        // Create Board
        var newBoardId = this.CreateBoardDatabase(
            boardName,
            "english.xml",
            newAdmin);

        if (newBoardId > 0 && Configuration.Config.MultiBoardFolders)
        {
            // Successfully created the new board
            var boardFolder = this.Server.MapPath(
                Path.Combine(Configuration.Config.BoardRoot, $"{newBoardId}/"));

            // Create New Folders.
            if (!Directory.Exists(Path.Combine(boardFolder, "Images")))
            {
                // Create the Images Folders
                Directory.CreateDirectory(Path.Combine(boardFolder, "Images"));

                // Create Sub Folders
                Directory.CreateDirectory(Path.Combine(boardFolder, "Images", "Avatars"));
                Directory.CreateDirectory(Path.Combine(boardFolder, "Images", "Categories"));
                Directory.CreateDirectory(Path.Combine(boardFolder, "Images", "Forums"));
                Directory.CreateDirectory(Path.Combine(boardFolder, "Images", "Medals"));
            }

            if (!Directory.Exists(Path.Combine(boardFolder, "Uploads")))
            {
                Directory.CreateDirectory(Path.Combine(boardFolder, "Uploads"));
            }
        }

        // Import Users & Roles
        if (importUsers)
        {
            UserImporter.ImportUsers(
                newBoardId,
                this.PortalSettings.PortalId,
                out _);
        }

        return newBoardId;
    }

    /// <summary>
    /// Creates the board in the database.
    /// </summary>
    /// <param name="boardName">
    /// Name of the board.
    /// </param>
    /// <param name="langFile">
    /// The language file.
    /// </param>
    /// <param name="newAdmin">
    /// The new admin.
    /// </param>
    /// <returns>
    /// Returns the Board ID of the new Board.
    /// </returns>
    private int CreateBoardDatabase(
        string boardName,
        string langFile,
        AspNetUsers newAdmin)
    {
        var newBoardId = this.GetRepository<Board>().Create(
            boardName,
            this.PortalSettings.Email,
            "en-US",
            langFile,
            newAdmin.UserName,
            newAdmin.Email,
            newAdmin.Id,
            this.PortalSettings.UserInfo.IsSuperUser,
            Configuration.Config.CreateDistinctRoles && Configuration.Config.IsAnyPortal ? "YAF " : string.Empty);

        var loadWrapper = new Action<string, Action<Stream>>(
            (file, streamAction) =>
                {
                    var fullFile = this.Get<HttpRequestBase>().MapPath(file);

                    if (!File.Exists(fullFile))
                    {
                        return;
                    }

                    // import into board...
                    using var stream = new StreamReader(fullFile);

                    streamAction(stream.BaseStream);
                    stream.Close();
                });

        // load default bbcode if available...
        loadWrapper("install/bbCodeExtensions.xml", s => DataImport.BBCodeExtensionImport(newBoardId, s));

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
            Path.Combine(
                "DesktopModules",
                "YetAnotherForumDotNet",
                this.Get<BoardFolders>().Uploads));

        if (Directory.Exists(attachActiveFolderPath))
        {
            Directory.GetFiles(attachActiveFolderPath).ForEach(
                attachment =>
                    {
                        var fileName = Path.GetFileName(attachment);
                        File.Copy(attachment, $"{attachYafFolderPath}\\{fileName}.yafupload");
                    });
        }
    }

    /// <summary>
    /// Fixes the last forum topic & Post.
    /// </summary>
    /// <param name="boardId">The board identifier.</param>
    private void FixLastForumTopic(int boardId)
    {
        var forums = this.GetRepository<Forum>().ListAll(boardId);

        forums.ForEach(
            forum => this.GetRepository<Forum>().UpdateLastPost(forum.Item1.ID));
    }
}