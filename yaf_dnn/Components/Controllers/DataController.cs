/* Yet Another Forum.NET
 * Copyright (C) 2003-2005 Bjørnar Henden
 * Copyright (C) 2006-2013 Jaben Cargman
 * Copyright (C) 2014-2022 Ingo Herbote
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

namespace YAF.DotNetNuke.Components.Controllers;

using System.Collections.Generic;

/// <summary>
/// Module Data Controller to Handle SQL Stuff
/// </summary>
public class DataController
{
    /// <summary>
    /// Gets the YAF board roles.
    /// </summary>
    /// <param name="boardId">The board id.</param>
    /// <returns>Returns the YAF Board Roles</returns>
    public static List<RoleInfo> GetYafBoardRoles(int boardId)
    {
        var roles = new List<RoleInfo>();

        var groups = BoardContext.Current.GetRepository<Group>().Get(g => g.BoardID == boardId);

        roles.AddRange(from Group row in groups select new RoleInfo { RoleName = row.Name, RoleID = row.ID, });

        return roles;
    }

    /// <summary>
    /// Gets the YAF board access masks.
    /// </summary>
    /// <param name="boardId">The board id.</param>
    /// <returns>Returns the YAF Board access masks</returns>
    public static List<RoleInfo> GetYafBoardAccessMasks(int boardId)
    {
        var roles = new List<RoleInfo>();

        var masks = BoardContext.Current.GetRepository<AccessMask>().Get(a => a.BoardID == boardId);

        roles.AddRange(
            from AccessMask row in masks
            select new RoleInfo { RoleName = row.Name, RoleID = row.ID, RoleGroupID = row.Flags });

        return roles;
    }

    /// <summary>
    /// Gets the YAF user roles.
    /// </summary>
    /// <param name="yafUserId">The YAF user id.</param>
    /// <returns>Returns List of YAF user roles</returns>
    public static List<RoleInfo> GetYafUserRoles(int yafUserId)
    {
        var roles = new List<RoleInfo>();

        var userGroups = BoardContext.Current.GetRepository<UserGroup>().List(yafUserId);

        roles.AddRange(from Group row in userGroups select new RoleInfo { RoleName = row.Name, RoleID = row.ID });

        return roles;
    }

    /// <summary>
    /// Imports the active forums.
    /// </summary>
    /// <param name="moduleId">
    /// The module identifier.
    /// </param>
    /// <param name="boardId">
    /// The board identifier.
    /// </param>
    /// <param name="portalSettings">
    /// The portal Settings.
    /// </param>
    public static void ImportActiveForums([NotNull] int moduleId, [NotNull] int boardId, [NotNull] PortalSettings portalSettings)
    {
        DataProvider.Instance().ExecuteNonQuery($"{Config.DatabaseObjectQualifier}ImportActiveForums", moduleId, boardId, portalSettings.PortalId);
    }
}