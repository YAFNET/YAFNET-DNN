/* Yet Another Forum.NET
 * Copyright (C) 2003-2005 Bj�rnar Henden
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

namespace YAF.DotNetNuke.Components.Controllers
{
    #region

    using System;

    using global::DotNetNuke.Data;

    using YAF.Types.Extensions;

    #endregion

    /// <summary>
    /// DataController to Handling all SQL Stuff
    /// </summary>
    public class Profile
    {
        #region Public Methods

        /// <summary>
        /// Get The Latest DateTime where on of the DNN Profile Fields was updated
        /// </summary>
        /// <param name="userID">The user ID.</param>
        /// <returns>
        /// The DateTime when the DNN Profile was last updated.
        /// </returns>
        public static DateTime YafDnnGetLastUpdatedProfile(int userID)
        {
            var lastUpdatedDate = new DateTime();

            using (var dataReader = DataProvider.Instance().ExecuteReader("YafDnn_LastUpdatedProfile", userID))
            {
                while (dataReader.Read())
                {
                    lastUpdatedDate = dataReader["LastUpdatedDate"].ToType<DateTime>();
                }
            }

            return lastUpdatedDate;
        }       

        #endregion
    }
}