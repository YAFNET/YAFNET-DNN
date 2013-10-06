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

namespace YAF.DotNetNuke.Components.Controllers
{
    #region Using

    using System;
    using System.Collections.Generic;
    using System.Data;

    using global::DotNetNuke.Data;
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

            using (var dataReader = DataProvider.Instance().ExecuteReader("YafDnn_Messages"))
            {
                while (dataReader.Read())
                {
                    var message = new Messages
                        {
                            Message = dataReader["Message"].ToType<string>(),
                            MessageId = dataReader["MessageID"].ToType<int>(),
                            TopicId = dataReader["TopicID"].ToType<int>(),
                            Posted = dataReader["Posted"].ToType<DateTime>()
                        };

                    messagesList.Add(message);
                }
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

            using (var dataReader = DataProvider.Instance().ExecuteReader("YafDnn_Topics"))
            {
                while (dataReader.Read())
                {
                    var topic = new Topics
                        {
                            TopicName = dataReader["Topic"].ToType<string>(),
                            TopicId = dataReader["TopicID"].ToType<int>(),
                            ForumId = dataReader["ForumID"].ToType<int>(),
                            Posted = dataReader["Posted"].ToType<DateTime>()
                        };

                    topicsList.Add(topic);
                }
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

            using (var dataReader = DataProvider.Instance().ExecuteReader("yaf_group_list", boardId, null))
            {
                while (dataReader.Read())
                {
                    var role = new RoleInfo
                    {
                        RoleName = dataReader["Name"].ToType<string>(),
                        RoleID = dataReader["GroupID"].ToType<int>(),
                    };

                    roles.Add(role);
                }
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

            using (var dataReader = DataProvider.Instance().ExecuteReader("yaf_accessmask_list", boardId, null, 0))
            {
                while (dataReader.Read())
                {
                    var role = new RoleInfo
                    {
                        RoleName = Convert.ToString(dataReader["Name"]),
                        RoleID = dataReader["AccessMaskID"].ToType<int>(),
                        RoleGroupID = dataReader["Flags"].ToType<int>()
                    };

                    roles.Add(role);
                }
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

            using (var dataReader = DataProvider.Instance().ExecuteReader("yaf_usergroup_list", yafUserId))
            {
                while (dataReader.Read())
                {
                    var role = new RoleInfo
                    {
                        RoleName = dataReader["Name"].ToType<string>(),
                        RoleID = dataReader["GroupID"].ToType<int>(),
                    };

                    roles.Add(role);
                }
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

            using (var dataReader = DataProvider.Instance().ExecuteReader("yaf_GetReadAccessListForForum", forumId))
            {
                while (dataReader.Read())
                {
                    var forumAccess = new ForumAccess
                    {
                        GroupID = dataReader["GroupID"].ToType<int>(),
                        GroupName = dataReader["GroupName"].ToType<string>(),
                        AccessMaskName = dataReader["AccessMaskName"].ToType<string>(),
                        Flags = new AccessFlags(dataReader["Flags"])
                    };

                    forumAccessList.Add(forumAccess);
                }
            }

            return forumAccessList;
        }

        #endregion
    }
}