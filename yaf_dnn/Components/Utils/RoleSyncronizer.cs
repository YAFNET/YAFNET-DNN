/* Yet Another Forum.NET
 * Copyright (C) 2003-2005 Bjørnar Henden
 * Copyright (C) 2006-2013 Jaben Cargman
 * Copyright (C) 2014-2016 Ingo Herbote
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
    using System.Linq;

    using global::DotNetNuke.Entities.Modules;
    using global::DotNetNuke.Entities.Users;
    using global::DotNetNuke.Security.Roles;

    using YAF.Classes.Data;
    using YAF.Core;
    using YAF.Core.Extensions;
    using YAF.Core.Model;
    using YAF.DotNetNuke.Components.Controllers;
    using YAF.Types.Extensions;
    using YAF.Types.Flags;
    using YAF.Types.Interfaces;
    using YAF.Types.Models;

    /// <summary>
    /// YAF DNN Profile Synchronization 
    /// </summary>
    public class RoleSyncronizer : PortalModuleBase
    {
        /// <summary>
        /// Synchronizes the user roles.
        /// </summary>
        /// <param name="boardId">The board id.</param>
        /// <param name="portalId">The portal id.</param>
        /// <param name="yafUserId">The YAF user id.</param>
        /// <param name="dnnUserInfo">The DNN user info.</param>
        /// <returns>
        /// Returns if the Roles where synched or not
        /// </returns>
        public static bool SynchronizeUserRoles(int boardId, int portalId, int yafUserId, UserInfo dnnUserInfo)
        {
            // Make sure are roles exist
            ImportDNNRoles(boardId, dnnUserInfo.Roles);

            var yafUserRoles = Data.GetYafUserRoles(boardId, yafUserId);

            var yafBoardRoles = YafContext.Current.GetRepository<Group>().ListTyped(boardId: boardId);

            var rolesChanged = false;

            // TODO : Move code to sql SP

            // add yaf only roles to yaf
            foreach (var boardRole in yafBoardRoles)
            {
                var roleFlags = new GroupFlags(boardRole.Flags);

                var role = new RoleInfo
                {
                    RoleName = boardRole.Name,
                    RoleID = boardRole.ID
                };

                if (roleFlags.IsGuest)
                {
                    continue;
                }

                if (roleFlags.IsStart)
                {
                    if (yafUserRoles.Any(existRole => existRole.RoleName.Equals(role.RoleName)))
                    {
                        continue;
                    }

                    UpdateUserRole(role, yafUserId, dnnUserInfo.Username, true);

                    rolesChanged = true;
                }
                else
                {
                    if (!dnnUserInfo.Roles.Any(dnnRole => dnnRole.Equals(boardRole.Name)))
                    {
                        continue;
                    }

                    if (yafUserRoles.Any(existRole => existRole.RoleName.Equals(role.RoleName)))
                    {
                        continue;
                    }

                    UpdateUserRole(role, yafUserId, dnnUserInfo.Username, true);

                    rolesChanged = true;
                }
            }

            var roleController = new RoleController();

            // Remove user from dnn role if no longer included
            foreach (
                RoleInfo role in
                    roleController.GetPortalRoles(portalId)
                                  .Cast<RoleInfo>()
                                  .Where(
                                      role =>
                                      !dnnUserInfo.Roles.Any(existRole => existRole.Equals(role.RoleName))
                                      && yafUserRoles.Any(existRole => existRole.RoleName.Equals(role.RoleName))))
            {
                UpdateUserRole(role, yafUserId, dnnUserInfo.Username, false);

                rolesChanged = true;
            }

            // empty out access table
            if (rolesChanged && YafContext.Current != null)
            {
                YafContext.Current.GetRepository<ActiveAccess>().DeleteAll();
                YafContext.Current.GetRepository<Active>().DeleteAll();
            }

            return rolesChanged;
        }

        /// <summary>
        /// Checks if the <paramref name="roles" /> exists, in YAF, the user is in
        /// </summary>
        /// <param name="boardId">The board id.</param>
        /// <param name="roles">The <paramref name="roles" />.</param>
        public static void ImportDNNRoles(int boardId, string[] roles)
        {
            var yafBoardRoles = Data.GetYafBoardRoles(boardId);

            var yafBoardAccessMasks = Data.GetYafBoardAccessMasks(boardId);

            // Check If Dnn Roles Exists in Yaf
            foreach (var role in from role in roles
                                    where role.IsSet()
                                    let any = yafBoardRoles.Any(yafRole => yafRole.RoleName.Equals(role))
                                    where !any
                                    select role)
            {
                CreateYafRole(role, boardId, yafBoardAccessMasks);
            }
        }

        /// <summary>
        /// Creates the YAF role.
        /// </summary>
        /// <param name="roleName">Name of the role.</param>
        /// <param name="boardId">The board id.</param>
        /// <param name="yafBoardAccessMasks">The YAF board access masks.</param>
        /// <returns>
        /// Returns the Role id of the created Role
        /// </returns>
        public static long CreateYafRole(string roleName, int boardId, List<RoleInfo> yafBoardAccessMasks)
        {
            // If not Create Role in YAF
            if (!RoleMembershipHelper.RoleExists(roleName))
            {
                RoleMembershipHelper.CreateRole(roleName);
            }

            int accessMaskId;

            try
            {
                // Give the default DNN Roles Member Access, unknown roles get "No Access Mask"
                accessMaskId = roleName.Equals("Registered Users") || roleName.Equals("Subscribers")
                               ? yafBoardAccessMasks.Find(mask => mask.RoleName.Equals("Member Access")).RoleID
                               : yafBoardAccessMasks.Find(mask => mask.RoleName.Equals("No Access Mask")).RoleID;
            }
            catch (Exception)
            {
                accessMaskId = yafBoardAccessMasks.Find(mask => mask.RoleGroupID.Equals(0)).RoleID;
            }

            // Role exists in membership but not in yaf itself simply add it to yaf
            return LegacyDb.group_save(
                DBNull.Value,
                boardId,
                roleName,
                false,
                false,
                false,
                false,
                accessMaskId,
                0,
                null,
                100,
                null,
                0,
                null,
                null,
                0,
                0);
        }

        /// <summary>
        /// Updates the user <paramref name="role" />.
        /// </summary>
        /// <param name="role">The <paramref name="role" />.</param>
        /// <param name="yafUserId">The YAF user id.</param>
        /// <param name="userName">Name of the user.</param>
        /// <param name="addRole">if set to true [add role].</param>
        public static void UpdateUserRole(RoleInfo role, int yafUserId, string userName, bool addRole)
        {
            // save user in role
            LegacyDb.usergroup_save(yafUserId, role.RoleID, addRole);

            if (addRole && !RoleMembershipHelper.IsUserInRole(userName, role.RoleName))
            {
                RoleMembershipHelper.AddUserToRole(userName, role.RoleName);
            }
            else if (!addRole && RoleMembershipHelper.IsUserInRole(userName, role.RoleName))
            {
                RoleMembershipHelper.RemoveUserFromRole(userName, role.RoleName);
            }
        }
    }
}