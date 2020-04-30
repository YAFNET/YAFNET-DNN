/* Yet Another Forum.NET
 * Copyright (C) 2003-2005 Bjørnar Henden
 * Copyright (C) 2006-2013 Jaben Cargman
 * Copyright (C) 2014-2020 Ingo Herbote
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

namespace YAF.DotNetNuke.Components.Integration
{
    using System;
    using System.Linq;
    using System.Text;

    using global::DotNetNuke.Entities.Portals;
    using global::DotNetNuke.Entities.Users;
    using global::DotNetNuke.Security.Roles;
    using global::DotNetNuke.Services.Journal;

    using YAF.Core.Context;
    using YAF.Core.Extensions;
    using YAF.DotNetNuke.Components.Controllers;
    using YAF.Types.Attributes;
    using YAF.Types.Constants;
    using YAF.Types.Extensions;
    using YAF.Types.Flags;
    using YAF.Types.Interfaces;
    using YAF.Types.Models;
    using YAF.Utils;
    using YAF.Utils.Helpers;

    /// <summary>
    /// Activity Stream DNN Integration Class
    /// </summary>
    [ExportService(ServiceLifetimeScope.Singleton)]
    public class Journal : IActivityStream
    {
        /// <summary>
        /// Adds the New Topic to the User's ActivityStream
        /// </summary>
        /// <param name="forumID">The forum unique identifier.</param>
        /// <param name="topicID">The topic unique identifier.</param>
        /// <param name="messageID">The message unique identifier.</param>
        /// <param name="topicTitle">The topic title.</param>
        /// <param name="message">The message.</param>
        public void AddTopicToStream(int forumID, long topicID, int messageID, string topicTitle, string message)
        {
            message = BBCodeHelper.StripBBCode(
                    HtmlHelper.StripHtml(HtmlHelper.CleanHtmlString(message)))
                    .RemoveMultipleWhitespace();

            var user = UserController.Instance.GetCurrentUserInfo();
            var portalSettings = PortalSettings.Current;

            var ji = new JournalItem
                         {
                             PortalId = portalSettings.PortalId,
                             ProfileId = user.UserID,
                             UserId = user.UserID,
                             Title = topicTitle,
                             ItemData =
                                 new ItemData
                                     {
                                         Url = BuildLink.GetLink(ForumPages.Posts, "t={0}", topicID)
                                     },
                             Summary = message.Truncate(150),
                             Body = message,
                             JournalTypeId = 5,
                             SecuritySet = GetSecuritySet(forumID, portalSettings.PortalId),
                             ObjectKey = $"{forumID.ToString()}:{topicID.ToString()}:{messageID.ToString()}"
                         };

            if (JournalController.Instance.GetJournalItemByKey(portalSettings.PortalId, ji.ObjectKey) != null)
            {
                JournalController.Instance.DeleteJournalItemByKey(portalSettings.PortalId, ji.ObjectKey);
            }

            // TODO:
            /*if (SocialGroupId > 0)
            {
                ji.SocialGroupId = SocialGroupId;
            }*/

            JournalController.Instance.SaveJournalItem(ji, -1);
        }

        /// <summary>
        /// Adds the Reply to the User's ActivityStream
        /// </summary>
        /// <param name="forumID">The forum unique identifier.</param>
        /// <param name="topicID">The topic unique identifier.</param>
        /// <param name="messageID">The message unique identifier.</param>
        /// <param name="topicTitle">The topic title.</param>
        /// <param name="message">The message.</param>
        public void AddReplyToStream(int forumID, long topicID, int messageID, string topicTitle, string message)
        {
            message = BBCodeHelper.StripBBCode(
                    HtmlHelper.StripHtml(HtmlHelper.CleanHtmlString(message)))
                    .RemoveMultipleWhitespace();

            var user = UserController.Instance.GetCurrentUserInfo();
            var portalSettings = PortalSettings.Current;
            
            var ji = new JournalItem
                         {
                             PortalId = portalSettings.PortalId,
                             ProfileId = user.UserID,
                             UserId = user.UserID,
                             Title = topicTitle,
                             ItemData =
                                 new ItemData
                                     {
                                         Url =
                                             BuildLink.GetLink(
                                                 ForumPages.Posts,
                                                 "m={0}#post{0}",
                                                 messageID)
                                     },
                             Summary = message.Truncate(150),
                             Body = message,
                             JournalTypeId = 6,
                             SecuritySet = GetSecuritySet(forumID, portalSettings.PortalId),
                             ObjectKey = $"{forumID.ToString()}:{topicID.ToString()}:{messageID.ToString()}"
                         };

            if (JournalController.Instance.GetJournalItemByKey(portalSettings.PortalId, ji.ObjectKey) != null)
            {
                JournalController.Instance.DeleteJournalItemByKey(portalSettings.PortalId, ji.ObjectKey);
            }
            
            // TODO:
            /*if (SocialGroupId > 0)
            {
                ji.SocialGroupId = SocialGroupId;
            }*/

            JournalController.Instance.SaveJournalItem(ji, -1);
        }

        /// <summary>
        /// Add Mention to Users Stream
        /// </summary>
        /// <param name="userId">
        /// The user id.
        /// </param>
        /// <param name="topicId">
        /// The topic id.
        /// </param>
        /// <param name="messageId">
        /// The message id.
        /// </param>
        /// <param name="fromUserId">
        /// The from user id.
        /// </param>
        public void AddMentionToStream(int userId, int topicId, int messageId, int fromUserId)
        {
            var flags = new ActivityFlags { WasMentioned = true };

            var activity = new Activity
            {
                Flags = flags.BitValue,
                FromUserID = fromUserId,
                TopicID = topicId,
                MessageID = messageId,
                UserID = userId,
                Notification = true,
                Created = DateTime.UtcNow
            };

            BoardContext.Current.GetRepository<Activity>().Insert(activity);

            BoardContext.Current.Get<IDataCache>().Remove(
                string.Format(Constants.Cache.ActiveUserLazyData, userId));
        }

        /// <summary>
        /// Add Quoting to Users Stream
        /// </summary>
        /// <param name="userId">
        /// The user id.
        /// </param>
        /// <param name="topicId">
        /// The topic id.
        /// </param>
        /// <param name="messageId">
        /// The message id.
        /// </param>
        /// <param name="fromUserId">
        /// The from user id.
        /// </param>
        public void AddQuotingToStream(int userId, int topicId, int messageId, int fromUserId)
        {
            var flags = new ActivityFlags { WasQuoted = true };

            var activity = new Activity
            {
                Flags = flags.BitValue,
                FromUserID = fromUserId,
                TopicID = topicId,
                MessageID = messageId,
                UserID = userId,
                Notification = true,
                Created = DateTime.UtcNow
            };

            BoardContext.Current.GetRepository<Activity>().Insert(activity);

            BoardContext.Current.Get<IDataCache>().Remove(
                string.Format(Constants.Cache.ActiveUserLazyData, userId));
        }

        /// <summary>
        /// The add thanks received to stream.
        /// </summary>
        /// <param name="userId">
        /// The user id.
        /// </param>
        /// <param name="topicId">
        /// The topic id.
        /// </param>
        /// <param name="messageId">
        /// The message id.
        /// </param>
        /// <param name="fromUserId">
        /// The from user id.
        /// </param>
        public void AddThanksReceivedToStream(int userId, int topicId, int messageId, int fromUserId)
        {
            var flags = new ActivityFlags { ReceivedThanks = true };

            var activity = new Activity
            {
                Flags = flags.BitValue,
                FromUserID = fromUserId,
                TopicID = topicId,
                MessageID = messageId,
                UserID = userId,
                Notification = true,
                Created = DateTime.UtcNow
            };

            BoardContext.Current.GetRepository<Activity>().Insert(activity);

            BoardContext.Current.Get<IDataCache>().Remove(
                string.Format(Constants.Cache.ActiveUserLazyData, userId));
        }

        /// <summary>
        /// The add thanks given to stream.
        /// </summary>
        /// <param name="userId">
        /// The user id.
        /// </param>
        /// <param name="topicId">
        /// The topic id.
        /// </param>
        /// <param name="messageId">
        /// The message id.
        /// </param>
        /// <param name="fromUserId">
        /// The from user id.
        /// </param>
        public void AddThanksGivenToStream(int userId, int topicId, int messageId, int fromUserId)
        {
            var flags = new ActivityFlags { GivenThanks = true };

            var activity = new Activity
            {
                Flags = flags.BitValue,
                FromUserID = fromUserId,
                TopicID = topicId,
                MessageID = messageId,
                UserID = userId,
                Notification = false,
                Created = DateTime.UtcNow
            };

            BoardContext.Current.GetRepository<Activity>().Insert(activity);
        }

        /// <summary>
        /// Gets the security set for the forum.
        /// </summary>
        /// <param name="forumID">The forum unique identifier.</param>
        /// <param name="portalID">The portal unique identifier.</param>
        /// <returns>Returns The Security Set for the Forum including all Roles which have Read Access</returns>
        private static string GetSecuritySet(int forumID, int portalID)
        {
            var forumAccessList = Data.GetReadAccessListForForum(forumID);

            var dnnRoles = new RoleController().GetRoles(portalID).ToList();

            var securitySet = new StringBuilder();

            forumAccessList.ForEach(
                forumAccess =>
                    {
                        if (!forumAccess.Flags.ReadAccess)
                        {
                            return;
                        }

                        RoleInfo role = null;

                        if (dnnRoles.Any(r => r.RoleName == forumAccess.GroupName))
                        {
                            role = dnnRoles.First(r => r.RoleName == forumAccess.GroupName);
                        }

                        if (role != null)
                        {
                            securitySet.AppendFormat("R{0},", role.RoleID);
                        }

                        // Guest Access
                        if (forumAccess.GroupName == "Guests")
                        {
                            securitySet.Append("E,");
                        }
                    });

            return securitySet.ToString();
        }
    }
}