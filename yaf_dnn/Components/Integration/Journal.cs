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

namespace YAF.DotNetNuke.Components.Integration
{
    using System.Linq;
    using System.Text;

    using global::DotNetNuke.Entities.Modules;
    using global::DotNetNuke.Entities.Portals;
    using global::DotNetNuke.Entities.Users;
    using global::DotNetNuke.Security.Roles;
    using global::DotNetNuke.Services.Journal;

    using YAF.Core.Context;
    using YAF.Core.Extensions;
    using YAF.Core.Helpers;
    using YAF.Core.Model;
    using YAF.Core.Services;
    using YAF.Types.Attributes;
    using YAF.Types.Constants;
    using YAF.Types.Extensions;
    using YAF.Types.Flags;
    using YAF.Types.Interfaces;
    using YAF.Types.Interfaces.Services;
    using YAF.Types.Models;

    using DateTime = System.DateTime;

    /// <summary>
    /// Activity Stream DNN Integration Class
    /// </summary>
    [ExportService(ServiceLifetimeScope.Singleton)]
    public class Journal : PortalModuleBase, IActivityStream, IHaveServiceLocator
    {
        /// <summary>
        /// Gets or sets ServiceLocator.
        /// </summary>
        public IServiceLocator ServiceLocator => BoardContext.Current.ServiceLocator;

        /// <summary>
        /// Adds the New Watch Topic to the User's ActivityStream
        /// </summary>
        /// <param name="userId">
        /// The user Id.
        /// </param>
        /// <param name="topicId">
        /// The topic unique identifier.
        /// </param>
        /// <param name="messageId">
        /// The message unique identifier.
        /// </param>
        /// <param name="topicTitle">
        /// The topic title.
        /// </param>
        /// <param name="message">
        /// The message.
        /// </param>
        /// <param name="fromUserId">
        /// The from User Id.
        /// </param>
        public void AddWatchTopicToStream(int userId, int topicId, int messageId, string topicTitle, string message, int fromUserId)
        {
            var flags = new ActivityFlags { WatchForumReply = true };

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

            this.GetRepository<Activity>().Insert(activity);
        }

        /// <summary>
        /// Adds the Watch Reply to the User's ActivityStream
        /// </summary>
        /// <param name="userId">
        /// The user Id.
        /// </param>
        /// <param name="topicId">
        /// The topic unique identifier.
        /// </param>
        /// <param name="messageId">
        /// The message unique identifier.
        /// </param>
        /// <param name="topicTitle">
        /// The topic title.
        /// </param>
        /// <param name="message">
        /// The message.
        /// </param>
        /// <param name="fromUserId">
        /// The from User Id.
        /// </param>
        public void AddWatchReplyToStream(int userId, int topicId, int messageId, string topicTitle, string message, int fromUserId)
        {
            var flags = new ActivityFlags { WatchTopicReply = true };

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

            this.GetRepository<Activity>().Insert(activity);
        }

        /// <summary>
        /// Adds the New Topic to the User's ActivityStream
        /// </summary>
        /// <param name="forumId">
        /// The forum Id.
        /// </param>
        /// <param name="topicId">
        /// The topic Id.
        /// </param>
        /// <param name="messageId">
        /// The message Id.
        /// </param>
        /// <param name="topicTitle">
        /// The topic title.
        /// </param>
        /// <param name="message">
        /// The message.
        /// </param>
        public void AddTopicToStream(int forumId, int topicId, int messageId, string topicTitle, string message)
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
                ItemData = new ItemData { Url = BoardContext.Current.Get<LinkBuilder>().GetTopicLink(topicId, topicTitle) },
                Summary = message.Truncate(150),
                Body = message,
                JournalTypeId = 5,
                SecuritySet = GetSecuritySet(forumId, portalSettings.PortalId),
                ObjectKey = $"{forumId}:{topicId}:{messageId}"
            };

            if (JournalController.Instance.GetJournalItemByKey(portalSettings.PortalId, ji.ObjectKey) != null)
            {
                JournalController.Instance.DeleteJournalItemByKey(portalSettings.PortalId, ji.ObjectKey);
            }

            JournalController.Instance.SaveJournalItem(ji, this.ModuleConfiguration);
        }

        /// <summary>
        /// Adds the Reply to the User's ActivityStream
        /// </summary>
        /// <param name="forumId">The forum unique identifier.</param>
        /// <param name="topicId">The topic unique identifier.</param>
        /// <param name="messageId">The message unique identifier.</param>
        /// <param name="topicTitle">The topic title.</param>
        /// <param name="message">The message.</param>
        public void AddReplyToStream(int forumId, int topicId, int messageId, string topicTitle, string message)
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
                        Url = BoardContext.Current.Get<LinkBuilder>().GetLink(
                            ForumPages.Posts,
                            new { m = messageId, name = topicTitle })
                    },
                Summary = message.Truncate(150),
                Body = message,
                JournalTypeId = 6,
                SecuritySet = GetSecuritySet(forumId, portalSettings.PortalId),
                ObjectKey = $"{forumId}:{topicId}:{messageId}"
            };

            if (JournalController.Instance.GetJournalItemByKey(portalSettings.PortalId, ji.ObjectKey) != null)
            {
                JournalController.Instance.DeleteJournalItemByKey(portalSettings.PortalId, ji.ObjectKey);
            }

            JournalController.Instance.SaveJournalItem(ji, this.ModuleConfiguration);
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
        /// <param name="forumId">The forum unique identifier.</param>
        /// <param name="portalId">The portal unique identifier.</param>
        /// <returns>Returns The Security Set for the Forum including all Roles which have Read Access</returns>
        private static string GetSecuritySet(int forumId, int portalId)
        {
            var forumAccessList = BoardContext.Current.GetRepository<ForumAccess>().GetReadAccessList(forumId);

            var dnnRoles = new RoleController().GetRoles(portalId).ToList();

            var securitySet = new StringBuilder();

            forumAccessList.Where(x => x.Item2.AccessFlags.ReadAccess).ForEach(
                forumAccess =>
                    {
                        RoleInfo role = null;

                        if (dnnRoles.Any(r => r.RoleName == forumAccess.Item3.Name))
                        {
                            role = dnnRoles.First(r => r.RoleName == forumAccess.Item3.Name);
                        }

                        if (role != null)
                        {
                            securitySet.AppendFormat("R{0},", role.RoleID);
                        }

                        // Guest Access
                        if (forumAccess.Item3.Name == "Guests")
                        {
                            securitySet.Append("E,");
                        }
                    });

            return securitySet.ToString();
        }
    }
}