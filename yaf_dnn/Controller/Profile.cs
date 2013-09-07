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

namespace YAF.DotNetNuke.Controller
{
    #region

    using System;
    using System.Data;

    using global::DotNetNuke.Data;

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
            DateTime lastUpdatedDate = new DateTime();

            using (IDataReader dr = DataProvider.Instance().ExecuteReader("YafDnn_LastUpdatedProfile", userID))
            {
                while (dr.Read())
                {
                    lastUpdatedDate = (DateTime)dr["LastUpdatedDate"];
                }
            }

            return lastUpdatedDate;
        }       

        #endregion
    }
}