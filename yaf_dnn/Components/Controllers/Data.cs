/* Yet Another Forum.NET
 * Copyright (C) 2003-2005 Bjørnar Henden
 * Copyright (C) 2006-2013 Jaben Cargman
 * Copyright (C) 2014-2017 Ingo Herbote
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
    #region Using

    using System.Collections.Generic;
    using System.Data;
    using System.Linq;

    using global::DotNetNuke.Security.Roles;

    using YAF.Core;
    using YAF.Core.Extensions;
    using YAF.Core.Model;
    using YAF.Types;
    using YAF.Types.Extensions;
    using YAF.Types.Flags;
    using YAF.Types.Interfaces;
    using YAF.Types.Interfaces.Data;
    using YAF.Types.Models;

    using ForumAccess = YAF.DotNetNuke.Components.Objects.ForumAccess;

    #endregion

    /// <summary>
    /// Module Data Controller to Handle SQL Stuff
    /// </summary>
    public class Data
    {
        #region Public Methods

        /// <summary>
        /// Get The list of all boards
        /// </summary>
        /// <returns>
        /// Returns the List of all boards
        /// </returns>
        public static List<Board> ListBoards()
        {
            var boards = new List<Board>();

            var messagesTable = YafContext.Current.GetRepository<Board>().List();

            boards.AddRange(from DataRow row in messagesTable.Rows select new Board { ID = row["ID"].ToType<int>() });

            return boards;
        }

        /// <summary>
        /// Get The Latest Post from SQL
        /// </summary>
        /// <param name="boardId">The Board Id of the Board</param>
        /// <param name="numOfPostsToRetrieve">How many post should been retrieved</param>
        /// <param name="pageUserId">Current Users Id</param>
        /// <param name="useStyledNicks">if set to <c>true</c> [use styled nicks].</param>
        /// <param name="showNoCountPosts">if set to <c>true</c> [show no count posts].</param>
        /// <param name="findLastRead">if set to <c>true</c> [find last read].</param>
        /// <returns>
        /// Returns the Table of Latest Posts
        /// </returns>
        public static DataTable TopicLatest(
            object boardId,
            object numOfPostsToRetrieve,
            object pageUserId,
            bool useStyledNicks,
            bool showNoCountPosts,
            bool findLastRead = false)
        {
            return YafContext.Current.GetRepository<Topic>().LatestAsDataTable(
                boardId.ToType<int>(),
                numOfPostsToRetrieve.ToType<int>(),
                pageUserId.ToType<int>(),
                useStyledNicks,
                showNoCountPosts,
                findLastRead);
        }

        /// <summary>
        /// Add active access row for the current user outside of YAF
        /// </summary>
        /// <param name="boardId">The board id.</param>
        /// <param name="userId">The user id.</param>
        /// <param name="isGuest">if set to <c>true</c> [is guest].</param>
        /// <returns>
        /// Returns the Table of the Active Access User Table
        /// </returns>
        public static DataTable ActiveAccessUser(object boardId, object userId, bool isGuest)
        {
            return YafContext.Current.GetRepository<ActiveAccess>().PageAccessAsDataTable(boardId, userId, isGuest);
        }

        /// <summary>
        /// Gets the YAF board roles.
        /// </summary>
        /// <param name="boardId">The board id.</param>
        /// <returns>Returns the YAF Board Roles</returns>
        public static List<RoleInfo> GetYafBoardRoles(int boardId)
        {
            var roles = new List<RoleInfo>();

            var groups = YafContext.Current.GetRepository<Group>().Get(g => g.BoardID == boardId);

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

            var masks = YafContext.Current.GetRepository<AccessMask>().Get(a => a.BoardID == boardId);

            roles.AddRange(
                from AccessMask row in masks
                select
                    new RoleInfo
                        {
                            RoleName = row.Name,
                            RoleID = row.ID,
                            RoleGroupID = row.Flags
                        });

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

            var rolesTable = YafContext.Current.GetRepository<UserGroup>().ListAsDataTable(yafUserId);

            roles.AddRange(
                from DataRow row in rolesTable.Rows
                select
                    new RoleInfo { RoleName = row["Name"].ToType<string>(), RoleID = row["GroupID"].ToType<int>(), });

            return roles;
        }

        /// <summary>
        /// Gets the read access list for forum.
        /// </summary>
        /// <param name="forumId">The forum unique identifier.</param>
        /// <returns>Returns the read access list for forum</returns>
        public static List<ForumAccess> GetReadAccessListForForum(int forumId)
        {
            var forumAccessList = new List<ForumAccess>();

            var accessListTable = (DataTable)YafContext.Current.Get<IDbFunction>().GetData.GetReadAccessListForForum(ForumID: forumId);

            forumAccessList.AddRange(
                from DataRow row in accessListTable.Rows
                select
                    new ForumAccess
                        {
                            GroupID = row["GroupID"].ToType<int>(),
                            GroupName = row["GroupName"].ToType<string>(),
                            AccessMaskName = row["AccessMaskName"].ToType<string>(),
                            Flags = new AccessFlags(row["Flags"])
                        });

            return forumAccessList;
        }

        /// <summary>
        /// Imports the active forums.
        /// </summary>
        /// <param name="moduleId">The module identifier.</param>
        /// <param name="boardId">The board identifier.</param>
        public static void ImportActiveForums([NotNull] int moduleId, [NotNull] int boardId)
        {
            YafContext.Current.Get<IDbFunction>().Scalar
                .ImportActiveForums(oModuleID: moduleId, tplBoardID: boardId);
        }

        #endregion
    }
}