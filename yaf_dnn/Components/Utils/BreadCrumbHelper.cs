/* Yet Another Forum.NET
 * Copyright (C) 2006-2012 Jaben Cargman
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

namespace YAF.DotNetNuke.Components.Utils
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Text;
    using System.Web;
    using System.Web.UI;
    using System.Web.UI.WebControls;

    using global::DotNetNuke.UI.Skins;

    using YAF.Classes;
    using YAF.Controls;
    using YAF.Core;
    using YAF.Types.Extensions;
    using YAF.Types.Interfaces;
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
        /// <param name="dnnBreadCrumbID">The DNN bread crumb unique identifier.</param>
        /// <returns>
        /// Returns if the Bread Crumb was successfully appended
        /// </returns>
        public static bool UpdateDnnBreadCrumb(Control control, string dnnBreadCrumbID)
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

                var breadCrumbControl = FindDnnBreadCrumbControl(control, dnnBreadCrumbID);

                if (breadCrumbControl == null)
                {
                    return false;
                }

                var separator =
                    breadCrumbControl.GetType()
                        .GetProperty("Separator")
                        .GetValue(breadCrumbControl, BindingFlags.Public | BindingFlags.NonPublic, null, null, null)
                        .ToString();
                object cssObject = breadCrumbControl.GetType()
                    .GetProperty("CssClass")
                    .GetValue(breadCrumbControl, BindingFlags.Public | BindingFlags.NonPublic, null, null, null);

                var cssClass = "SkinObject";

                if (cssObject != null && cssObject.ToString().IsSet())
                {
                    cssClass = cssObject.ToString();
                }

                // add dnn css classes to yaf breadcrumb links
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
        /// <param name="theControl">The control.</param>
        /// <param name="dnnBreadCrumbID">The DNN bread crumb unique identifier.</param>
        /// <returns>Returns the Control if found</returns>
        private static Control FindDnnBreadCrumbControl(Control theControl, string dnnBreadCrumbID)
        {
            Control foundControl;

            if (theControl.Parent != null)
            {
                var control = theControl.Parent.FindControlAs<SkinObjectBase>(dnnBreadCrumbID);

                if (control != null && control.TemplateControl.AppRelativeVirtualPath.ToLowerInvariant()
                             .Contains("breadcrumb.ascx"))
                {
                    return control;
                }

                foundControl = FindDnnBreadCrumbControl(theControl.Parent, dnnBreadCrumbID);
            }
            else
            {
                foundControl = null;
            }

            return foundControl;
        }
    }
}