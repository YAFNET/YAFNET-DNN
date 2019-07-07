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

namespace YAF.DotNetNuke.Components.Utils
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Text;
    using System.Web;
    using System.Web.UI;
    using System.Web.UI.WebControls;

    using global::DotNetNuke.Entities.Portals;
    using global::DotNetNuke.UI.Skins;

    using YAF.Classes;
    using YAF.Controls;
    using YAF.Core;
    using YAF.Types.Extensions;
    using YAF.Types.Interfaces;
    using YAF.Types.Objects;
    using YAF.Utils.Helpers;

    /// <summary>
    /// Helper Class to inject the Bread Crumb
    /// </summary>
    public class BreadCrumbHelper
    {
        /// <summary>
        /// Append YAF Bread Crumb to the DNN Bread Crumb
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="dnnBreadCrumbId">The DNN bread crumb unique identifier.</param>
        /// <param name="portalSettings">The portal settings.</param>
        /// <returns>
        /// Returns if the Bread Crumb was successfully appended
        /// </returns>
        public static bool UpdateDnnBreadCrumb(Control control, string dnnBreadCrumbId, PortalSettings portalSettings)
        {
            try
            {
                var yafPageLinks = GetYafPageLinkList();

                if (yafPageLinks == null)
                {
                    return false;
                }

                if (yafPageLinks.Count <= 1)
                {
                    return false;
                }

                var breadCrumbControl = FindDnnBreadCrumbControl(control, dnnBreadCrumbId);

                if (breadCrumbControl == null)
                {
                    return false;
                }

                var separator =
                    breadCrumbControl.GetType()
                        .GetProperty("Separator")
                        ?.GetValue(breadCrumbControl, BindingFlags.Public | BindingFlags.NonPublic, null, null, null)
                        .ToString();

                if (separator != null && (separator.IndexOf("src=", StringComparison.Ordinal) != -1 && !separator.Contains(portalSettings.ActiveTab.SkinPath)))
                {
                    separator = separator.Replace("src=\"", $"src=\"{portalSettings.ActiveTab.SkinPath}");
                }

                var cssObject = breadCrumbControl.GetType()
                    .GetProperty("CssClass")
                    ?.GetValue(breadCrumbControl, BindingFlags.Public | BindingFlags.NonPublic, null, null, null);

                var cssClass = "SkinObject";

                if (cssObject != null && cssObject.ToString().IsSet())
                {
                    cssClass = cssObject.ToString();
                }

                // add dnn CSS classes to YAF breadcrumb links
                var yafBreadCrumb = new StringBuilder();

                foreach (var link in yafPageLinks)
                {
                    var title = HttpUtility.HtmlEncode(link.Title.Trim());

                    if (YafContext.Current.Get<YafBoardSettings>().Name.Equals(title))
                    {
                        continue;
                    }

                    var url = link.URL.Trim();

                    yafBreadCrumb.AppendFormat(
                        @"{3}<a href=""{0}"" class=""{1}"">{2}</a>", 
                        url.IsNotSet() ? "#" : url, 
                        cssClass, 
                        title, 
                        separator);
                }

                breadCrumbControl.FindControlAs<Label>("lblBreadCrumb").Text += yafBreadCrumb.ToString();

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Gets the YAF page links list
        /// </summary>
        /// <returns>Returns the List if found</returns>
        private static List<PageLink> GetYafPageLinkList()
        {
            List<PageLink> pageLinksTable = null;

            var pageLinksControl = YafContext.Current.CurrentForumPage.FindControlAs<PageLinks>("PageLinks");

            if (pageLinksControl != null)
            {
                pageLinksTable = pageLinksControl.PageLinkList;
            }

            return pageLinksTable;
        }

        /// <summary>
        /// Finds the DNN bread crumb control.
        /// </summary>
        /// <param name="theControl">
        /// The control.
        /// </param>
        /// <param name="dnnBreadCrumbId">
        /// The DNN bread crumb unique identifier.
        /// </param>
        /// <returns>
        /// Returns the Control if found
        /// </returns>
        private static Control FindDnnBreadCrumbControl(Control theControl, string dnnBreadCrumbId)
        {
            Control foundControl;

            if (theControl.Parent != null)
            {
                var control = theControl.Parent.FindControlAs<SkinObjectBase>(dnnBreadCrumbId);

                if (control != null && control.TemplateControl.AppRelativeVirtualPath.ToLowerInvariant()
                             .Contains("breadcrumb.ascx"))
                {
                    return control;
                }

                foundControl = FindDnnBreadCrumbControl(theControl.Parent, dnnBreadCrumbId);
            }
            else
            {
                foundControl = null;
            }

            return foundControl;
        }
    }
}