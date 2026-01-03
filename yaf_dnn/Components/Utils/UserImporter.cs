/* Yet Another Forum.NET
 * Copyright (C) 2003-2005 Bjørnar Henden
 * Copyright (C) 2006-2013 Jaben Cargman
 * Copyright (C) 2014-2026 Ingo Herbote
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

namespace YAF.DotNetNuke.Components.Utils;

using global::DotNetNuke.Common.Utilities;

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
        var boardSettings = BoardContext.Current is null
                                ? BoardContext.Current.Get<BoardSettingsService>().LoadBoardSettings(boardId, null)
                                : BoardContext.Current.Get<BoardSettings>();

        var rolesChanged = false;

        try
        {
            users.Cast<UserInfo>().ForEach(
                dnnUserInfo =>
                    {
                        var yafUser = BoardContext.Current.GetRepository<User>()
                            .GetUserByProviderKey(boardId, dnnUserInfo.UserID.ToString());

                        if (yafUser != null)
                        {
                            rolesChanged = RoleSyncronizer.SynchronizeUserRoles(
                                boardId,
                                portalId,
                                yafUser.ID,
                                dnnUserInfo);

                            // super admin check...
                            if (dnnUserInfo.IsSuperUser)
                            {
                                SetYafHostUser(yafUser.ID, boardId);
                            }
                        }
                        else
                        {
                            // Update UserID fom YAF < 3
                            yafUser = BoardContext.Current.GetRepository<User>().GetSingle(
                                u => u.Name == dnnUserInfo.Username && u.Email == dnnUserInfo.Email);

                            if (yafUser != null)
                            {
                                // update provider Key
                                BoardContext.Current.GetRepository<User>().UpdateOnly(
                                    () => new User { ProviderUserKey = dnnUserInfo.UserID.ToString() },
                                    u => u.ID == yafUser.ID);

                                rolesChanged = RoleSyncronizer.SynchronizeUserRoles(
                                    boardId,
                                    portalId,
                                    yafUser.ID,
                                    dnnUserInfo);
                            }
                            else
                            {
                                // Create user if Not Exist
                                CreateYafUser(dnnUserInfo, boardId, portalId, boardSettings);
                                newUserCount++;
                            }
                        }
                    });

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
    /// <param name="boardId">The board ID.</param>
    /// <param name="portalId">The portal identifier.</param>
    /// <param name="boardSettings">The board settings.</param>
    /// <returns>
    /// Returns the User ID of the new User
    /// </returns>
    public static int CreateYafUser(
        UserInfo dnnUserInfo,
        int boardId,
        int portalId,
        BoardSettings boardSettings)
    {
        // create the user in the YAF DB so profile can gets created...
        var yafUserId = BoardContext.Current.Get<IAspNetRolesHelper>().CreateForumUser(
            dnnUserInfo.ToAspNetUsers(),
            dnnUserInfo.DisplayName,
            boardId);

        if (yafUserId is null)
        {
            return 0;
        }

        var autoWatchTopicsEnabled =
            boardSettings.DefaultNotificationSetting.Equals(UserNotificationSetting.TopicsIPostToOrSubscribeTo);

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

        if (userInfo is null)
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
            userFlags.BitValue,
            userInfo.RankID);
    }
}