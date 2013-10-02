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
    #region Using

    using System;
    using System.Collections.Generic;
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
                    string[] values = this.YafInstances.SelectedValue.Split(Convert.ToChar("-"));

                    if (values.Length == 2)
                    {
                        objModules.UpdateTabModuleSetting(this.TabModuleId, "YafPage", values[0]);
                        objModules.UpdateTabModuleSetting(this.TabModuleId, "YafModuleId", values[1]);
                    }
                }

                if (ValidationHelper.IsNumeric(this.txtMaxResult.Text) || !string.IsNullOrEmpty(this.txtMaxResult.Text))
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

                if (!string.IsNullOrEmpty(this.HtmlHeader.Text))
                {
                    objModules.UpdateTabModuleSetting(this.TabModuleId, "YafWhatsNewHeader", this.HtmlHeader.Text);
                }

                if (!string.IsNullOrEmpty(this.HtmlItem.Text))
                {
                    objModules.UpdateTabModuleSetting(this.TabModuleId, "YafWhatsNewItemTemplate", this.HtmlItem.Text);
                }

                if (!string.IsNullOrEmpty(this.HtmlFooter.Text))
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

            List<TabInfo> objTabs = TabController.GetPortalTabs(this.PortalSettings.PortalId, -1, true, true);

            DesktopModuleInfo objDesktopModuleInfo =
                DesktopModuleController.GetDesktopModuleByModuleName("YetAnotherForumDotNet", this.PortalId);

            if (objDesktopModuleInfo == null)
            {
                return;
            }

            foreach (TabInfo objTab in objTabs)
            {
                if (objTab == null || objTab.IsDeleted)
                {
                    continue;
                }

                var objModules = new ModuleController();

                foreach (KeyValuePair<int, ModuleInfo> pair in objModules.GetTabModules(objTab.TabID))
                {
                    ModuleInfo objModule = pair.Value;

                    if (objModule.IsDeleted || objModule.DesktopModuleID != objDesktopModuleInfo.DesktopModuleID)
                    {
                        continue;
                    }

                    string strPath = objTab.TabName;
                    TabInfo objTabSelected = objTab;

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