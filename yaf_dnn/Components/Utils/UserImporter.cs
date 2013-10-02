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

namespace YAF.DotNetNuke.Components.Utils
{
    using System;
    using System.Data;
    using System.Web.Security;

    using global::DotNetNuke.Common.Utilities;
    using global::DotNetNuke.Entities.Portals;
    using global::DotNetNuke.Entities.Users;
    using global::DotNetNuke.Services.Exceptions;

    using YAF.Classes;
    using YAF.Classes.Data;
    using YAF.Core;
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
                            null,
                            null,
                            dnnUserInfo,
                            dnnUser,
                            portalId,
                            portalGUID,
                            boardId);
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