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

/// <summary>
/// User Importer.
/// </summary>
public partial class YafDnnModuleImport : PortalModuleBase
{
    /// <summary>
    /// The type full name.
    /// </summary>
    private const string TypeFullName = "YAF.DotNetNuke.YafDnnImportScheduler, YAF.DotNetNuke.Module";

    /// <summary>
    /// The navigation manager.
    /// </summary>
    private readonly INavigationManager navigationManager;

    /// <summary>
    /// The board id.
    /// </summary>
    private int boardId;

    /// <summary>
    /// Initializes a new instance of the <see cref="YafDnnModuleImport"/> class.
    /// </summary>
    public YafDnnModuleImport()
    {
        this.navigationManager = this.DependencyProvider.GetRequiredService<INavigationManager>();
    }

    /// <summary>
    /// The add scheduler click.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
    protected void AddSchedulerClick(object sender, EventArgs e)
    {
        var btn = sender.ToType<LinkButton>();

        switch (btn.CommandArgument)
        {
            case "add":
                this.InstallScheduleClient();
                btn.CommandArgument = "delete";
                btn.Text = Localization.GetString("DeleteSheduler.Text", this.LocalResourceFile);
                break;
            case "delete":
                RemoveScheduleClient(GetIdOfScheduleClient(TypeFullName));
                btn.CommandArgument = "add";
                btn.Text = Localization.GetString("InstallSheduler.Text", this.LocalResourceFile);
                break;
        }
    }

    /// <summary>
    /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
    /// </summary>
    /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
    protected override void OnInit(EventArgs e)
    {
        this.Load += this.DotNetNukeModuleImportLoad;

        this.btnImportUsers.Click += this.ImportClick;
        this.Close.Click += this.CloseClick;
        this.btnAddScheduler.Click += this.AddSchedulerClick;

        base.OnInit(e);
    }

    /// <summary>
    /// The get id of schedule client.
    /// </summary>
    /// <param name="typeFullName">
    /// The type full name.
    /// </param>
    /// <returns>
    /// Returns the id of schedule client.
    /// </returns>
    private static int GetIdOfScheduleClient(string typeFullName)
    {
        // get array list of schedule items
        var scheduleItems = SchedulingProvider.Instance().GetSchedule();

        // find schedule item with matching TypeFullName
        var item = scheduleItems.Cast<ScheduleItem>().FirstOrDefault(s => s.TypeFullName == typeFullName);

        if (item != null)
        {
            return item.ScheduleID;
        }

        return -1;
    }

    /// <summary>
    /// The remove schedule client.
    /// </summary>
    /// <param name="scheduleId">The schedule id.</param>
    private static void RemoveScheduleClient(int scheduleId)
    {
        // get the item by id
        var item = SchedulingProvider.Instance().GetSchedule(scheduleId);

        // tell the provider to remove the item
        SchedulingProvider.Instance().DeleteSchedule(item);
    }

    /// <summary>
    /// The close click.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
    private void CloseClick(object sender, EventArgs e)
    {
        this.Response.Redirect(this.navigationManager.NavigateURL(), true);
    }

    /// <summary>
    /// Handles the Load event of the DotNetNukeModuleImport control.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
    private void DotNetNukeModuleImportLoad(object sender, EventArgs e)
    {
        this.btnImportUsers.Text = Localization.GetString("ImportNow.Text", this.LocalResourceFile);
        this.Close.Text = Localization.GetString("Close.Text", this.LocalResourceFile);
        this.btnAddScheduler.Text = Localization.GetString("InstallSheduler.Text", this.LocalResourceFile);

        try
        {
            this.boardId = this.Settings["forumboardid"].ToType<int>();
        }
        catch (Exception)
        {
            this.boardId = 1;
        }

        if (this.IsPostBack || GetIdOfScheduleClient(TypeFullName) <= 0)
        {
            return;
        }

        var importFile = $"{HttpRuntime.AppDomainAppPath}App_Data/YafImports.xml";

        var settings = new DataSet();

        try
        {
            settings.ReadXml(importFile);
        }
        catch (Exception)
        {
            var file = new FileStream(importFile, FileMode.Create);
            var sw = new StreamWriter(file);

            sw.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\" ?>");
            sw.WriteLine("<YafImports>");
            sw.WriteLine($"<Import PortalId=\"{this.PortalId}\" BoardId=\"{this.boardId}\"/>");
            sw.WriteLine("</YafImports>");

            sw.Close();
            file.Close();
        }

        var updateXml = false;

        foreach (DataRow dataRow in settings.Tables[0].Rows)
        {
            var portalId = dataRow["PortalId"].ToType<int>();
            var boardID = dataRow["BoardId"].ToType<int>();

            if (portalId.Equals(this.PortalId) && boardID.Equals(this.boardId))
            {
                updateXml = false;
                break;
            }

            updateXml = true;
        }

        if (updateXml)
        {
            var dr = settings.Tables["Import"].NewRow();

            dr["PortalId"] = this.PortalId.ToString();
            dr["BoardId"] = this.boardId.ToString();

            settings.Tables[0].Rows.Add(dr);

            settings.WriteXml(importFile);
        }

        this.btnAddScheduler.CommandArgument = "delete";
        this.btnAddScheduler.Text = Localization.GetString("DeleteSheduler.Text", this.LocalResourceFile);
    }

    /// <summary>
    /// Import/Update Users and Sync Roles
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
    private void ImportClick(object sender, EventArgs e)
    {
        UserImporter.ImportUsers(this.boardId, this.PortalSettings.PortalId, out var info);

        this.lInfo.Text = info;
    }

    /// <summary>
    /// The install schedule client.
    /// </summary>
    private void InstallScheduleClient()
    {
        var item = new ScheduleItem
                       {
                           FriendlyName = "YAF DotNetNuke User Importer",
                           TypeFullName = TypeFullName,
                           TimeLapse = 1,
                           TimeLapseMeasurement = "d",
                           RetryTimeLapse = 1,
                           RetryTimeLapseMeasurement = "d",
                           RetainHistoryNum = 5,
                           AttachToEvent = "None",
                           CatchUpEnabled = true,
                           Enabled = true,
                           ObjectDependencies = string.Empty,
                           Servers = string.Empty
                       };

        // add item
        SchedulingProvider.Instance().AddSchedule(item);

        var settings = new DataSet();

        var filePath = $"{HttpRuntime.AppDomainAppPath}App_Data/YafImports.xml";

        try
        {
            settings.ReadXml(filePath);
        }
        catch (Exception)
        {
            var file = new FileStream(filePath, FileMode.Create);
            var sw = new StreamWriter(file);

            sw.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\" ?>");
            sw.WriteLine("<YafImports>");
            sw.WriteLine($"<Import PortalId=\"{this.PortalId}\" BoardId=\"{this.boardId}\"/>");
            sw.WriteLine("</YafImports>");

            sw.Close();
            file.Close();
        }
    }
}