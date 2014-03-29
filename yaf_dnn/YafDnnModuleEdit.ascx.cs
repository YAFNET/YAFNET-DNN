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
    #region Using

    using System;
    using System.Data;
    using System.Web.UI.WebControls;

    using global::DotNetNuke.Entities.Modules;

    using global::DotNetNuke.Services.Localization;

    using YAF.Classes;
    using YAF.Core;
    using YAF.Core.Model;
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
            this.cancel.Click += CancelClick;
            this.create.Click += CreateClick;
            this.BoardID.SelectedIndexChanged += this.BoardIdSelectedIndexChanged;
            base.OnInit(e);
        }

        /// <summary>
        /// Cancel Editing.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.EventArgs" /> instance containing the event data.</param>
        private static void CancelClick(object sender, EventArgs e)
        {
            YafBuildLink.Redirect(ForumPages.forum);
        }

        /// <summary>
        /// Create New Board
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private static void CreateClick(object sender, EventArgs e)
        {
            YafBuildLink.Redirect(ForumPages.admin_editboard);
        }

        /// <summary>
        /// The bind categories.
        /// </summary>
        private void BindCategories()
        {
            using (
                DataTable dt = YafContext.Current.GetRepository<Category>()
                    .List(null, this.BoardID.SelectedValue.ToType<int>()))
            {
                DataRow row = dt.NewRow();
                row["Name"] = Localization.GetString("AllCategories.Text", this.LocalResourceFile);
                row["CategoryID"] = DBNull.Value;
                dt.Rows.InsertAt(row, 0);

                this.CategoryID.DataSource = dt;
                this.CategoryID.DataTextField = "Name";
                this.CategoryID.DataValueField = "CategoryID";
                this.CategoryID.DataBind();

                if (this.Settings["forumcategoryid"] == null)
                {
                    return;
                }

                var item = this.CategoryID.Items.FindByValue(this.Settings["forumcategoryid"].ToString());

                if (item != null)
                {
                    item.Selected = true;
                }
            }
        }

        /// <summary>
        /// Change the Categories if the Board is changed
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void BoardIdSelectedIndexChanged(object sender, EventArgs e)
        {
            this.BindCategories();
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

            //this.update.Visible = this.IsEditable;
            //this.create.Visible = this.IsEditable;

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

            this.BindCategories();

            // Load Remove Tab Name Setting
            this.RemoveTabName.Items.Add(new ListItem(Localization.GetString("RemoveTabName0.Text", this.LocalResourceFile), "0"));
            this.RemoveTabName.Items.Add(new ListItem(Localization.GetString("RemoveTabName1.Text", this.LocalResourceFile), "1"));
            this.RemoveTabName.Items.Add(new ListItem(Localization.GetString("RemoveTabName2.Text", this.LocalResourceFile), "2"));

            this.RemoveTabName.SelectedValue = this.Settings["RemoveTabName"] != null ? this.Settings["RemoveTabName"].ToType<string>() : "1";

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
            var objModules = new ModuleController();

            objModules.UpdateModuleSetting(this.ModuleId, "forumboardid", this.BoardID.SelectedValue);
            objModules.UpdateModuleSetting(this.ModuleId, "forumcategoryid", this.CategoryID.SelectedValue);

            objModules.UpdateModuleSetting(this.ModuleId, "RemoveTabName", this.RemoveTabName.SelectedValue);
            objModules.UpdateModuleSetting(
                this.ModuleId, "InheritDnnLanguage", this.InheritDnnLanguage.Checked.ToString());

            var boardSettings = new YafLoadBoardSettings(this.BoardID.SelectedValue.ToType<int>())
                                    {
                                        DNNPageTab =
                                            this.TabId
                                    };

            // save the settings to the database
            boardSettings.SaveRegistry();

            // Reload forum settings
            YafContext.Current.BoardSettings = null;

            YafBuildLink.Redirect(ForumPages.forum);
        }

        #endregion
    }
}