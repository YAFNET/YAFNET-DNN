/* Yet Another Forum.NET
 * Copyright (C) 2003-2005 Bjørnar Henden
 * Copyright (C) 2006-2013 Jaben Cargman
 * Copyright (C) 2014-2021 Ingo Herbote
 * https://www.yetanotherforum.net/
 * 
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at

 * https://www.apache.org/licenses/LICENSE-2.0

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
    using System.Linq;

    using global::DotNetNuke.Common.Utilities;
    using global::DotNetNuke.Entities.Users;
    using global::DotNetNuke.Services.Exceptions;

    using YAF.Configuration;
    using YAF.Core.BoardSettings;
    using YAF.Core.Context;
    using YAF.Core.Extensions;
    using YAF.Core.Model;
    using YAF.Types.Constants;
    using YAF.Types.Extensions;
    using YAF.Types.Flags;
    using YAF.Types.Interfaces;
    using YAF.Types.Interfaces.Identity;
    using YAF.Types.Models;
    using YAF.Types.Models.Identity;

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
            var newUserCount = 0;

            var users = UserController.GetUsers(portalId);

            // Inject SU here
            users.AddRange(UserController.GetUsers(false, true, Null.NullInteger));

            users.Sort(new UserComparer());

            // Load Yaf Board Settings if needed
            var boardSettings = BoardContext.Current == null
                ? new LoadBoardSettings(boardId)
                : BoardContext.Current.Get<BoardSettings>();

            var rolesChanged = false;

            try
            {
                foreach (var dnnUserInfo in users.Cast<UserInfo>())
                {
                    var dnnUser = BoardContext.Current.Get<IAspNetUsersHelper>().GetUserByName(dnnUserInfo.Username);

                    if (dnnUser == null)
                    {
                        continue;
                    }

                    // un-approve soft deleted user in yaf
                    if (dnnUserInfo.IsDeleted && dnnUser.IsApproved)
                    {
                        dnnUser.IsApproved = false;
                        BoardContext.Current.Get<IAspNetUsersHelper>().Update(dnnUser);

                        continue;
                    }

                    if (!dnnUserInfo.IsDeleted && !dnnUser.IsApproved)
                    {
                        dnnUser.IsApproved = true;
                        BoardContext.Current.Get<IAspNetUsersHelper>().Update(dnnUser);
                    }

                    var yafUserId = BoardContext.Current.GetRepository<User>().GetUserId(boardId, dnnUser.Id);

                    if (yafUserId.Equals(0))
                    {
                        // Create user if Not Exist
                        yafUserId = CreateYafUser(dnnUserInfo, dnnUser, boardId, portalId, boardSettings);
                        newUserCount++;
                    }

                    rolesChanged = RoleSyncronizer.SynchronizeUserRoles(boardId, portalId, yafUserId, dnnUserInfo);

                    // super admin check...
                    if (dnnUserInfo.IsSuperUser)
                    {
                        SetYafHostUser(yafUserId, boardId);
                    }
                }

                BoardContext.Current.Get<IDataCache>().Clear();

                DataCache.ClearCache();
            }
            catch (Exception ex)
            {
                Exceptions.LogException(ex);
            }

            info =
                $"{newUserCount} User(s) Imported, all user profiles are synchronized{(rolesChanged ? ", but all User Roles are synchronized!" : ", User Roles already synchronized!")}";

            return newUserCount;
        }

        /// <summary>
        /// Creates the YAF user.
        /// </summary>
        /// <param name="dnnUserInfo">The DNN user info.</param>
        /// <param name="dnnUser">The DNN user.</param>
        /// <param name="boardId">The board ID.</param>
        /// <param name="portalId">The portal identifier.</param>
        /// <param name="boardSettings">The board settings.</param>
        /// <returns>
        /// Returns the User ID of the new User
        /// </returns>
        public static int CreateYafUser(
            UserInfo dnnUserInfo,
            AspNetUsers dnnUser,
            int boardId,
            int portalId,
            BoardSettings boardSettings)
        {
            // setup roles
            BoardContext.Current.Get<IAspNetRolesHelper>().SetupUserRoles(boardId, dnnUser);

            // create the user in the YAF DB so profile can gets created...
            var yafUserId = BoardContext.Current.Get<IAspNetRolesHelper>().CreateForumUser(dnnUser, dnnUserInfo.DisplayName, boardId);

            if (yafUserId == null)
            {
                return 0;
            }

            // setup their initial profile information
            /*
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

              userProfile.Save();*/

            var autoWatchTopicsEnabled =
                boardSettings.DefaultNotificationSetting.Equals(UserNotificationSetting.TopicsIPostToOrSubscribeTo);

            // Save User
            BoardContext.Current.GetRepository<User>().Save(
                yafUserId.Value,
                boardId,
                dnnUserInfo.Username,
                dnnUserInfo.DisplayName,
                dnnUserInfo.Email,
                dnnUserInfo.Profile.PreferredTimeZone.Id,
                null,
                null,
                null,
                false);

            // save notification Settings
            BoardContext.Current.GetRepository<User>().SaveNotification(
                yafUserId.Value,
                true,
                autoWatchTopicsEnabled,
                boardSettings.DefaultNotificationSetting.ToInt(),
                boardSettings.DefaultSendDigestEmail);

            RoleSyncronizer.SynchronizeUserRoles(boardId, portalId, yafUserId.ToType<int>(), dnnUserInfo);

            return yafUserId.ToType<int>();
        }

        /// <summary>
        /// Set the User as Host user if not already
        /// </summary>
        /// <param name="yafUserId">The YAF user id.</param>
        /// <param name="boardId">The board id.</param>
        public static void SetYafHostUser(int yafUserId, int boardId)
        {
            // get this user information...
            var userInfo = BoardContext.Current.GetRepository<User>().GetById(yafUserId);

            if (userInfo == null)
            {
                return;
            }

            if (userInfo.UserFlags.IsHostAdmin)
            {
                return;
            }

            // fix the IsHostAdmin flag...
            var userFlags = new UserFlags(userInfo.Flags) { IsHostAdmin = true };

            // update...
            BoardContext.Current.GetRepository<User>().AdminSave(
                boardId,
                yafUserId,
                userInfo.Name,
                userInfo.DisplayName,
                userInfo.Email,
                userFlags.BitValue,
                userInfo.RankID);
        }
    }
}