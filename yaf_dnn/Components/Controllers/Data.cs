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

namespace YAF.DotNetNuke.Components.Controllers
{
    #region Using

    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;

    using global::DotNetNuke.Security.Roles;

    using YAF.Classes.Data;
    using YAF.DotNetNuke.Components.Objects;
    using YAF.Types.Extensions;
    using YAF.Types.Flags;
    using YAF.Types.Interfaces.Data;

    #endregion

    /// <summary>
    /// Module Data Controller to Handle SQL Stuff
    /// </summary>
    public class Data
    {
        #region Public Methods

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
            using (var cmd = DbHelpers.GetCommand("topic_latest"))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("BoardID", boardId);
                cmd.Parameters.AddWithValue("NumPosts", numOfPostsToRetrieve);
                cmd.Parameters.AddWithValue("PageUserID", pageUserId);
                cmd.Parameters.AddWithValue("StyledNicks", useStyledNicks);
                cmd.Parameters.AddWithValue("ShowNoCountPosts", showNoCountPosts);
                cmd.Parameters.AddWithValue("FindLastRead", findLastRead);

                return LegacyDb.DbAccess.GetData(cmd);
            }
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
            using (var cmd = DbHelpers.GetCommand("pageaccess"))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("BoardID", boardId);
                cmd.Parameters.AddWithValue("UserID", userId);
                cmd.Parameters.AddWithValue("IsGuest", isGuest);
                cmd.Parameters.AddWithValue("UTCTIMESTAMP", DateTime.UtcNow);

                return LegacyDb.DbAccess.GetData(cmd);
            }
        }

        /// <summary>
        /// Get all <see cref="Messages"/> From The Forum
        /// </summary>
        /// <returns>
        /// Message List
        /// </returns>
        public static List<Messages> YafDnnGetMessages()
        {
            var messagesList = new List<Messages>();

            using (var cmd = DbHelpers.GetCommand("YafDnn_Messages"))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                var mesagesTable = LegacyDb.DbAccess.GetData(cmd);

                messagesList.AddRange(
                    from DataRow row in mesagesTable.Rows
                    select
                        new Messages
                            {
                                Message = row["Message"].ToType<string>(),
                                MessageId = row["MessageID"].ToType<int>(),
                                TopicId = row["TopicID"].ToType<int>(),
                                Posted = row["Posted"].ToType<DateTime>()
                            });
            }

            return messagesList;
        }

        /// <summary>
        /// Get all <see cref="Messages"/> From The Forum
        /// </summary>
        /// <returns>
        /// Topics List
        /// </returns>
        public static List<Topics> YafDnnGetTopics()
        {
            var topicsList = new List<Topics>();

            using (var cmd = DbHelpers.GetCommand("YafDnn_Topics"))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                var topicsTable = LegacyDb.DbAccess.GetData(cmd);

                topicsList.AddRange(
                    from DataRow row in topicsTable.Rows
                    select
                        new Topics
                            {
                                TopicName = row["Topic"].ToType<string>(),
                                TopicId = row["TopicID"].ToType<int>(),
                                ForumId = row["ForumID"].ToType<int>(),
                                Posted = row["Posted"].ToType<DateTime>()
                            });
            }

            return topicsList;
        }

        /// <summary>
        /// Gets the YAF board roles.
        /// </summary>
        /// <param name="boardId">The board id.</param>
        /// <returns>Returns the YAF Board Roles</returns>
        public static List<RoleInfo> GetYafBoardRoles(int boardId)
        {
            var roles = new List<RoleInfo>();

            using (var cmd = DbHelpers.GetCommand("group_list"))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("BoardID", boardId);
                cmd.AddParam("GroupID", null);

                var groupsTable = LegacyDb.DbAccess.GetData(cmd);

                roles.AddRange(
                    from DataRow row in groupsTable.Rows
                    select
                        new RoleInfo { RoleName = row["Name"].ToType<string>(), RoleID = row["GroupID"].ToType<int>(), });
            }

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

            using (var cmd = DbHelpers.GetCommand("accessmask_list"))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("BoardID", boardId);
                cmd.AddParam("AccessMaskID", null);
                cmd.AddParam("ExcludeFlags", 0);

                var accessmasksTable = LegacyDb.DbAccess.GetData(cmd);

                roles.AddRange(
                    from DataRow row in accessmasksTable.Rows
                    select
                        new RoleInfo
                            {
                                RoleName = Convert.ToString(row["Name"]),
                                RoleID = row["AccessMaskID"].ToType<int>(),
                                RoleGroupID = row["Flags"].ToType<int>()
                            });
            }

            return roles;
        }

        /// <summary>
        /// Gets the YAF user roles.
        /// </summary>
        /// <param name="boardId">The board id.</param>
        /// <param name="yafUserId">The YAF user id.</param>
        /// <returns>Returns List of YAF user roles</returns>
        public static List<RoleInfo> GetYafUserRoles(int boardId, int yafUserId)
        {
            var roles = new List<RoleInfo>();

            using (var cmd = DbHelpers.GetCommand("usergroup_list"))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("UserID", yafUserId);

                var rolesTable = LegacyDb.DbAccess.GetData(cmd);

                roles.AddRange(
                    from DataRow row in rolesTable.Rows
                    select
                        new RoleInfo { RoleName = row["Name"].ToType<string>(), RoleID = row["GroupID"].ToType<int>(), });
            }

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

            using (var cmd = DbHelpers.GetCommand("GetReadAccessListForForum"))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("ForumID", forumId);

                var accessListTable = LegacyDb.DbAccess.GetData(cmd);

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
            }

            return forumAccessList;
        }

        #endregion
    }
}