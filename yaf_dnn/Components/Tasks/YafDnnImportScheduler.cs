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
 * KIND, either exfess or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 */

namespace YAF.DotNetNuke
{
    #region Using

    using System;
    using System.Data;
    using System.IO;
    using System.Linq;
    using System.Web;

    using global::DotNetNuke.Services.Exceptions;

    using global::DotNetNuke.Services.Scheduling;

    using YAF.Core;
    using YAF.Core.Model;
    using YAF.DotNetNuke.Components.Controllers;
    using YAF.DotNetNuke.Components.Utils;
    using YAF.Types.Extensions;
    using YAF.Types.Interfaces;
    using YAF.Types.Models;

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

            var boards = YafContext.Current != null
                             ? YafContext.Current.GetRepository<Board>().ListTyped()
                             : Data.ListBoards();

            foreach (DataRow dataRow in settings.Tables[0].Rows)
            {
                var boardId = dataRow["BoardId"].ToType<int>();
                var portalId = dataRow["PortalId"].ToType<int>();

                // check if board exist
                if (boards.Any(b => b.ID.Equals(boardId)))
                {
                    UserImporter.ImportUsers(boardId, portalId, out this.info);
                }
            }
        }

        #endregion
    }
}