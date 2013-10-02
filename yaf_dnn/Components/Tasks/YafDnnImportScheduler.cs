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
    using System.Data;
    using System.IO;
    using System.Web;

    using global::DotNetNuke.Services.Exceptions;

    using global::DotNetNuke.Services.Scheduling;

    using YAF.DotNetNuke.Components.Utils;
    using YAF.Types.Extensions;

    #endregion

    /// <summary>
    /// The YAF DNN import scheduler.
    /// </summary>
    public class YafDnnImportScheduler : SchedulerClient
    {
        #region Constants and Fields

        /// <summary>
        /// The info.
        /// </summary>
        private string info = string.Empty;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="YafDnnImportScheduler"/> class.
        /// </summary>
        /// <param name="scheduleHistoryItem">
        /// The schedule history item.
        /// </param>
        public YafDnnImportScheduler(ScheduleHistoryItem scheduleHistoryItem)
        {
            this.ScheduleHistoryItem = scheduleHistoryItem;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// The do work.
        /// </summary>
        public override void DoWork()
        {
            try
            {
                this.GetSettings();

                // report success to the scheduler framework
                this.ScheduleHistoryItem.Succeeded = true;

                this.ScheduleHistoryItem.AddLogNote(this.info);
            }
            catch (Exception exc)
            {
                this.ScheduleHistoryItem.Succeeded = false;
                this.ScheduleHistoryItem.AddLogNote("EXCEPTION: {0}".FormatWith(exc));
                this.Errored(ref exc);

                Exceptions.LogException(exc);
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets the settings.
        /// </summary>
        private void GetSettings()
        {
            var settings = new DataSet();

            var filePath = "{0}App_Data/YafImports.xml".FormatWith(HttpRuntime.AppDomainAppPath);

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
                sw.WriteLine("<Import PortalId=\"0\" BoardId=\"1\"/>");
                sw.WriteLine("</YafImports>");

                sw.Close();
                file.Close();

                settings.ReadXml(filePath);
            }

            foreach (DataRow dataRow in settings.Tables[0].Rows)
            {
                UserImporter.ImportUsers(
                    dataRow["BoardId"].ToType<int>(),
                    dataRow["PortalId"].ToType<int>(),
                    out this.info);
            }
        }

        #endregion
    }
}