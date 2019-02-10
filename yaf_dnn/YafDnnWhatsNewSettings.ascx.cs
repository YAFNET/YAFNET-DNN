/* Yet Another Forum.NET
 * Copyright (C) 2003-2005 Bjørnar Henden
 * Copyright (C) 2006-2013 Jaben Cargman
 * Copyright (C) 2014-2019 Ingo Herbote
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
    using System.Web.UI.WebControls;

    using global::DotNetNuke.Common.Utilities;

    using global::DotNetNuke.Entities.Modules;

    using global::DotNetNuke.Entities.Tabs;

    using global::DotNetNuke.Services.Exceptions;

    using YAF.Types.Extensions;
    using YAF.Utils.Helpers;

    #endregion

    /// -----------------------------------------------------------------------------
    /// <summary>
    /// The Settings class manages Module Settings
    /// </summary>
    /// <history>
    /// </history>
    /// -----------------------------------------------------------------------------
    public partial class YafDnnWhatsNewSettings : ModuleSettingsBase
    {
        #region Public Methods

        /// -----------------------------------------------------------------------------
        /// <summary>
        /// LoadSettings loads the settings from the Database and displays them
        /// </summary>
        /// <history>
        /// </history>
        /// -----------------------------------------------------------------------------
        public override void LoadSettings()
        {
            this.FillYafInstances();

            try
            {
                if (this.IsPostBack)
                {
                    return;
                }

                if (this.YafInstances.Items.Count > 0)
                {
                    if (this.TabModuleSettings["YafPage"].ToType<string>().IsSet()
                        && this.TabModuleSettings["YafModuleId"].ToType<string>().IsSet())
                    {
                        this.YafInstances.SelectedValue = "{0}-{1}".FormatWith(
                            this.TabModuleSettings["YafPage"],
                            this.TabModuleSettings["YafModuleId"]);
                    }
                }

                this.SortOrder.SelectedValue = this.TabModuleSettings["YafSortOrder"].ToType<string>().IsSet()
                                             ? this.TabModuleSettings["YafSortOrder"].ToType<string>()
                                             : "lastpost";

                this.txtMaxResult.Text = this.TabModuleSettings["YafMaxPosts"].ToType<string>().IsSet()
                                             ? this.TabModuleSettings["YafMaxPosts"].ToType<string>()
                                             : "10";

                if (this.TabModuleSettings["YafUseRelativeTime"] is bool)
                {
                    this.UseRelativeTime.Checked = this.TabModuleSettings["YafUseRelativeTime"].ToType<bool>();
                }

                this.HtmlHeader.Text = this.TabModuleSettings["YafWhatsNewHeader"].ToType<string>().IsSet()
                                           ? this.TabModuleSettings["YafWhatsNewHeader"].ToType<string>()
                                           : "<ul>";

                this.HtmlItem.Text = this.TabModuleSettings["YafWhatsNewItemTemplate"].ToType<string>().IsSet()
                                         ? this.TabModuleSettings["YafWhatsNewItemTemplate"].ToType<string>()
                                         : "<li class=\"YafPosts\">[LASTPOSTICON]&nbsp;<strong>[TOPICLINK]</strong>&nbsp;([FORUMLINK])<br />\"[LASTMESSAGE:150]\"<br />[BYTEXT]&nbsp;[LASTUSERLINK]&nbsp;[LASTPOSTEDDATETIME]</li>";

                this.HtmlFooter.Text = this.TabModuleSettings["YafWhatsNewFooter"].ToType<string>().IsSet()
                                           ? this.TabModuleSettings["YafWhatsNewFooter"].ToType<string>()
                                           : "</ul>";
            }
            catch (Exception exc)
            {
                // Module failed to load
                Exceptions.ProcessModuleLoadException(this, exc);
            }
        }

        /// -----------------------------------------------------------------------------
        /// <summary>
        /// UpdateSettings saves the modified settings to the Database
        /// </summary>
        /// <history>
        /// </history>
        /// -----------------------------------------------------------------------------
        public override void UpdateSettings()
        {
            try
            {
                var objModules = new ModuleController();

                if (this.YafInstances.Items.Count > 0)
                {
                    var values = this.YafInstances.SelectedValue.Split(Convert.ToChar("-"));

                    if (values.Length == 2)
                    {
                        objModules.UpdateTabModuleSetting(this.TabModuleId, "YafPage", values[0]);
                        objModules.UpdateTabModuleSetting(this.TabModuleId, "YafModuleId", values[1]);
                    }
                }

                objModules.UpdateTabModuleSetting(
                    this.TabModuleId,
                    "YafSortOrder",
                    this.SortOrder.SelectedValue);


                if (ValidationHelper.IsNumeric(this.txtMaxResult.Text) || this.txtMaxResult.Text.IsSet())
                {
                    objModules.UpdateTabModuleSetting(this.TabModuleId, "YafMaxPosts", this.txtMaxResult.Text);
                }
                else
                {
                    objModules.UpdateTabModuleSetting(this.TabModuleId, "YafMaxPosts", "10");
                }

                objModules.UpdateTabModuleSetting(
                    this.TabModuleId,
                    "YafUseRelativeTime",
                    this.UseRelativeTime.Checked.ToString());

                if (this.HtmlHeader.Text.IsSet())
                {
                    objModules.UpdateTabModuleSetting(this.TabModuleId, "YafWhatsNewHeader", this.HtmlHeader.Text);
                }

                if (this.HtmlItem.Text.IsSet())
                {
                    objModules.UpdateTabModuleSetting(this.TabModuleId, "YafWhatsNewItemTemplate", this.HtmlItem.Text);
                }

                if (this.HtmlFooter.Text.IsSet())
                {
                    objModules.UpdateTabModuleSetting(this.TabModuleId, "YafWhatsNewFooter", this.HtmlFooter.Text);
                }
            }
            catch (Exception exc)
            {
                // Module failed to load
                Exceptions.ProcessModuleLoadException(this, exc);
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Fill DropDownList with Portal Tabs
        /// </summary>
        private void FillYafInstances()
        {
            var objTabController = new TabController();

            var objTabs = TabController.GetPortalTabs(this.PortalSettings.PortalId, -1, true, true);

            var objDesktopModuleInfo =
                DesktopModuleController.GetDesktopModuleByModuleName("YetAnotherForumDotNet", this.PortalId);

            if (objDesktopModuleInfo == null)
            {
                return;
            }

            foreach (var objTab in objTabs)
            {
                if (objTab == null || objTab.IsDeleted)
                {
                    continue;
                }

                var objModules = new ModuleController();

                foreach (var pair in objModules.GetTabModules(objTab.TabID))
                {
                    var objModule = pair.Value;

                    if (objModule.IsDeleted || objModule.DesktopModuleID != objDesktopModuleInfo.DesktopModuleID)
                    {
                        continue;
                    }

                    var strPath = objTab.TabName;
                    var objTabSelected = objTab;

                    while (objTabSelected.ParentId != Null.NullInteger)
                    {
                        objTabSelected = objTabController.GetTab(objTabSelected.ParentId, objTab.PortalID, false);
                        if (objTabSelected == null)
                        {
                            break;
                        }

                        strPath = "{0} -> {1}".FormatWith(objTabSelected.TabName, strPath);
                    }

                    var objListItem = new ListItem
                                          {
                                              Value = "{0}-{1}".FormatWith(objModule.TabID, objModule.ModuleID),
                                              Text = "{0} -> {1}".FormatWith(strPath, objModule.ModuleTitle)
                                          };

                    this.YafInstances.Items.Add(objListItem);
                }
            }
        }

        #endregion
    }
}