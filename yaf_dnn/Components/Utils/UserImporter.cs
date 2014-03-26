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

namespace YAF.DotNetNuke.Components.Utils
{
    using System;
    using System.Data;
    using System.Web.Security;

    using global::DotNetNuke.Common;
    using global::DotNetNuke.Common.Utilities;
    using global::DotNetNuke.Entities.Portals;
    using global::DotNetNuke.Entities.Users;
    using global::DotNetNuke.Services.Exceptions;

    using YAF.Classes;
    using YAF.Classes.Data;
    using YAF.Core;
    using YAF.DotNetNuke.Components.Integration;
    using YAF.Types.Constants;
    using YAF.Types.Extensions;
    using YAF.Types.Flags;
    using YAF.Types.Interfaces;
    using YAF.Utils;

    /// <summary>
    /// YAF User Importer
    /// </summary>
    public class UserImporter
    {
        /// <summary>
        /// Imports the users.
        /// </summary>
        /// <param name="boardId">The board id.</param>
        /// <param name="portalId">The portal id.</param>
        /// <param name="info">The information text.</param>
        /// <returns>
        /// Returns the Number of Users that where imported
        /// </returns>
        public static int ImportUsers(int boardId, int portalId, out string info)
        {
            var portalGUID = new PortalController().GetPortal(portalId).GUID;

            return ImportUsers(boardId, portalId, portalGUID, out info);
        }

        /// <summary>
        /// Imports the users.
        /// </summary>
        /// <param name="boardId">The board id.</param>
        /// <param name="portalId">The portal id.</param>
        /// <param name="portalGUID">The portal unique identifier.</param>
        /// <param name="info">The information text.</param>
        /// <returns>
        /// Returns the Number of Users that where imported
        /// </returns>
        public static int ImportUsers(int boardId, int portalId, Guid portalGUID,  out string info)
        {
            var newUserCount = 0;

            var users = UserController.GetUsers(portalId);

            users.Sort(new UserComparer());

            // Load Yaf Board Settings if needed
            var boardSettings = YafContext.Current == null
                                    ? new YafLoadBoardSettings(boardId)
                                    : YafContext.Current.Get<YafBoardSettings>();

            var rolesChanged = false;

            try
            {
                foreach (UserInfo dnnUserInfo in users)
                {
                    var dnnUser = Membership.GetUser(dnnUserInfo.Username, true);

                    if (dnnUser == null)
                    {
                        continue;
                    }

                    if (dnnUserInfo.IsDeleted)
                    {
                        // TODO : Delete user in yaf
                        continue;
                    }

                    var yafUserId = LegacyDb.user_get(boardId, dnnUser.ProviderUserKey);

                    if (yafUserId.Equals(0))
                    {
                        // Create user if Not Exist
                        yafUserId = CreateYafUser(dnnUserInfo, dnnUser, boardId, null, boardSettings);
                        newUserCount++;
                    }
                    else
                    {
                        ProfileSyncronizer.UpdateUserProfile(
                            yafUserId,
                            YafUserProfile.GetProfile(dnnUser.UserName),
                            new CustomCombinedUserDataHelper(dnnUser, yafUserId, boardId),
                            dnnUserInfo,
                            dnnUser,
                            portalId,
                            portalGUID,
                            boardId,
                            true);
                    }

                    rolesChanged = RoleSyncronizer.SynchronizeUserRoles(boardId, portalId, yafUserId, dnnUserInfo);

                    // super admin check...
                    if (dnnUserInfo.IsSuperUser)
                    {
                        CreateYafHostUser(yafUserId, boardId);
                    }
                }

                YafContext.Current.Get<IDataCache>().Clear();

                DataCache.ClearCache();
            }
            catch (Exception ex)
            {
                Exceptions.LogException(ex);
            }

            info = "{0} User(s) Imported{1}".FormatWith(
                newUserCount,
                rolesChanged ? ", but all User Roles are synchronized!" : ", User Roles already synchronized!");

            return newUserCount;
        }

        /// <summary>
        /// Creates the YAF user.
        /// </summary>
        /// <param name="dnnUserInfo">The DNN user info.</param>
        /// <param name="dnnUser">The DNN user.</param>
        /// <param name="boardID">The board ID.</param>
        /// <param name="portalSettings">The portal settings.</param>
        /// <param name="boardSettings">The board settings.</param>
        /// <returns>
        /// Returns the User ID of the new User
        /// </returns>
        public static int CreateYafUser(
            UserInfo dnnUserInfo,
            MembershipUser dnnUser,
            int boardID,
            PortalSettings portalSettings,
            YafBoardSettings boardSettings)
        {
            // setup roles
            RoleMembershipHelper.SetupUserRoles(boardID, dnnUser.UserName);

            // create the user in the YAF DB so profile can gets created...
            var yafUserId = RoleMembershipHelper.CreateForumUser(dnnUser, dnnUserInfo.DisplayName, boardID);

            if (yafUserId == null)
            {
                return 0;
            }

            // create profile
            var userProfile = YafUserProfile.GetProfile(dnnUser.UserName);

            // setup their initial profile information
            userProfile.Initialize(dnnUser.UserName, true);

            if (dnnUserInfo.Profile.FullName.IsSet())
            {
                userProfile.RealName = dnnUserInfo.Profile.FullName;
            }

            if (dnnUserInfo.Profile.Country.IsSet() && !dnnUserInfo.Profile.Country.Equals("N/A"))
            {
                var regionInfo = ProfileSyncronizer.GetRegionInfoFromCountryName(dnnUserInfo.Profile.Country);

                if (regionInfo != null)
                {
                    userProfile.Country = regionInfo.TwoLetterISORegionName;
                }
            }

            if (dnnUserInfo.Profile.City.IsSet())
            {
                userProfile.City = dnnUserInfo.Profile.City;
            }

            if (dnnUserInfo.Profile.Website.IsSet())
            {
                userProfile.Homepage = dnnUserInfo.Profile.Website;
            } 

            userProfile.Save();

            // Save User
            LegacyDb.user_save(
                yafUserId,
                boardID,
                dnnUserInfo.Username,
                dnnUserInfo.DisplayName,
                dnnUserInfo.Email,
                0,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null);

            var autoWatchTopicsEnabled =
                boardSettings.DefaultNotificationSetting.Equals(UserNotificationSetting.TopicsIPostToOrSubscribeTo);

            // save notification Settings
            LegacyDb.user_savenotification(
                yafUserId,
                true,
                autoWatchTopicsEnabled,
                boardSettings.DefaultNotificationSetting,
                boardSettings.DefaultSendDigestEmail);

            RoleSyncronizer.SynchronizeUserRoles(boardID, portalSettings.PortalId, yafUserId.ToType<int>(), dnnUserInfo);

            return yafUserId.ToType<int>();
        }

        /// <summary>
        /// Adds the User as Host user if not already
        /// </summary>
        /// <param name="yafUserId">The YAF user id.</param>
        /// <param name="boardId">The board id.</param>
        public static void CreateYafHostUser(int yafUserId, int boardId)
        {
            // get this user information...
            var userInfoTable = LegacyDb.user_list(boardId, yafUserId, null, null, null);

            if (userInfoTable.Rows.Count <= 0)
            {
                return;
            }

            DataRow row = userInfoTable.Rows[0];

            if (row["IsHostAdmin"].ToType<bool>())
            {
                return;
            }

            // fix the IsHostAdmin flag...
            var userFlags = new UserFlags(row["Flags"]) { IsHostAdmin = true };

            // update...
            LegacyDb.user_adminsave(
                boardId,
                yafUserId,
                row["Name"],
                row["DisplayName"],
                row["Email"],
                userFlags.BitValue,
                row["RankID"]);
        }
    }
}