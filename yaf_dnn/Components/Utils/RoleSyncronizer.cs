﻿/* Yet Another Forum.NET
 * Copyright (C) 2003-2005 Bjørnar Henden
 * Copyright (C) 2006-2013 Jaben Cargman
 * Copyright (C) 2014-2023 Ingo Herbote
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

using System.Collections.Generic;

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
        ImportDnnRoles(boardId, dnnUserInfo.Roles);

        var yafUserRoles = DataController.GetYafUserRoles(yafUserId);

        var yafBoardRoles = BoardContext.Current.GetRepository<Group>().List(boardId: boardId);

        var rolesChanged = false;

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

                UpdateUserRole(role, yafUserId, true);

                rolesChanged = true;
            }
            else
            {
                // ADD dnn super user manually to the administrator role
                if (role.RoleName.Equals("Administrators") && dnnUserInfo.IsSuperUser)
                {
                    if (!yafUserRoles.Any(existRole => existRole.RoleName.Equals(role.RoleName)))
                    {
                        UpdateUserRole(role, yafUserId, true);
                    }
                }

                if (!dnnUserInfo.Roles.Any(dnnRole => dnnRole.Equals(boardRole.Name)))
                {
                    continue;
                }

                if (yafUserRoles.Any(existRole => existRole.RoleName.Equals(role.RoleName)))
                {
                    continue;
                }

                UpdateUserRole(role, yafUserId, true);

                rolesChanged = true;
            }
        }

        var roleController = new RoleController();

        // Remove user from dnn role if no longer included
        foreach (
            var role in
            roleController.GetRoles(portalId)
                .Where(
                    role =>
                        !dnnUserInfo.Roles.Any(existRole => existRole.Equals(role.RoleName))
                        && yafUserRoles.Any(existRole => existRole.RoleName.Equals(role.RoleName))))
        {
            UpdateUserRole(role, yafUserId, false);

            rolesChanged = true;
        }

        // empty out access table
        if (!rolesChanged || BoardContext.Current is null)
        {
            return rolesChanged;
        }

        BoardContext.Current.GetRepository<ActiveAccess>().DeleteAll();
        BoardContext.Current.GetRepository<Active>().DeleteAll();

        return true;
    }

    /// <summary>
    /// Checks if the <paramref name="roles" /> exists, in YAF, the user is in
    /// </summary>
    /// <param name="boardId">The board id.</param>
    /// <param name="roles">The <paramref name="roles" />.</param>
    public static void ImportDnnRoles(int boardId, string[] roles)
    {
        var yafBoardRoles = DataController.GetYafBoardRoles(boardId);

        var yafBoardAccessMasks = DataController.GetYafBoardAccessMasks(boardId);

        // Check If Dnn Roles Exists in Yaf
        (from role in roles
         where role.IsSet()
         let any = yafBoardRoles.Any(yafRole => yafRole.RoleName.Equals(role))
         where !any
         select role).ForEach(role => CreateYafRole(role, boardId, yafBoardAccessMasks));
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

        var groupFlags = new GroupFlags();

        // Role exists in membership but not in yaf itself simply add it to yaf
        return BoardContext.Current.GetRepository<Group>().Save(
            null,
            boardId,
            roleName,
            groupFlags,
            accessMaskId,
            0,
            null,
            100,
            null,
            0,
            null,
            0,
            0);
    }

    /// <summary>
    /// Updates the user <paramref name="role"/>.
    /// </summary>
    /// <param name="role">
    /// The <paramref name="role"/>.
    /// </param>
    /// <param name="yafUserId">
    /// The YAF user id.
    /// </param>
    /// <param name="addRole">
    /// if set to true [add role].
    /// </param>
    public static void UpdateUserRole(RoleInfo role, int yafUserId, bool addRole)
    {
        // save user in role
        BoardContext.Current.GetRepository<UserGroup>().AddOrRemove(yafUserId, role.RoleID, addRole);
    }
}