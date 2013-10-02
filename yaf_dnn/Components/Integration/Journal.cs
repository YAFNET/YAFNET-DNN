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

namespace YAF.DotNetNuke.Components.Integration
{
    using global::DotNetNuke.Entities.Portals;
    using global::DotNetNuke.Entities.Users;
    using global::DotNetNuke.Services.Journal;

    using YAF.Types.Attributes;
    using YAF.Types.Constants;
    using YAF.Types.Extensions;
    using YAF.Types.Interfaces;
    using YAF.Utils;

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
            var user = UserController.GetCurrentUserInfo();
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
                                         Url = YafBuildLink.GetLink(ForumPages.posts, "t={0}", topicID)
                                     },
                             Summary = message.Truncate(150),
                             Body = message,
                             JournalTypeId = 5,
                             ObjectKey =
                                 "{0}:{1}:{2}".FormatWith(
                                     forumID.ToString(),
                                     topicID.ToString(),
                                     messageID.ToString())
                         };

            if (JournalController.Instance.GetJournalItemByKey(portalSettings.PortalId, ji.ObjectKey) != null)
            {
                JournalController.Instance.DeleteJournalItemByKey(portalSettings.PortalId, ji.ObjectKey);
            }

            /*string roles = string.Empty;
            if (!(string.IsNullOrEmpty(ReadRoles)))
            {
                if (ReadRoles.Contains("|"))
                {
                    roles = ReadRoles.Substring(0, ReadRoles.IndexOf("|", StringComparison.Ordinal) - 1);
                }
            }

            foreach (string s in roles.Split(';'))
            {
                if ((s == "-1") | (s == "-3"))
                {
                    if ((ji.SecuritySet != null) && (!ji.SecuritySet.Contains("E,")))
                    {
                        ji.SecuritySet += "E,";
                    }
                }
                else
                {
                    ji.SecuritySet += "R" + s + ",";
                }
            }*/

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
            var user = UserController.GetCurrentUserInfo();
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
                                             YafBuildLink.GetLink(
                                                 ForumPages.posts,
                                                 "m={0}#post{0}",
                                                 messageID)
                                     },
                             Summary = message.Truncate(150),
                             Body = message,
                             JournalTypeId = 6,
                             ObjectKey =
                                 "{0}:{1}:{2}".FormatWith(
                                     forumID.ToString(),
                                     topicID.ToString(),
                                     messageID.ToString())
                         };

            if (JournalController.Instance.GetJournalItemByKey(portalSettings.PortalId, ji.ObjectKey) != null)
            {
                JournalController.Instance.DeleteJournalItemByKey(portalSettings.PortalId, ji.ObjectKey);
            }
            
            /*string roles = string.Empty;
            if (!(string.IsNullOrEmpty(ReadRoles)))
            {
                if (ReadRoles.Contains("|"))
                {
                    roles = ReadRoles.Substring(0, ReadRoles.IndexOf("|", StringComparison.Ordinal) - 1);
                }
            }

            foreach (string s in roles.Split(';'))
            {
                if ((s == "-1") | (s == "-3"))
                {
                    if ((ji.SecuritySet != null) && (!ji.SecuritySet.Contains("E,")))
                    {
                        ji.SecuritySet += "E,";
                    }
                }
                else
                {
                    ji.SecuritySet += "R" + s + ",";
                }
            }*/

            // TODO:
            /*if (SocialGroupId > 0)
            {
                ji.SocialGroupId = SocialGroupId;
            }*/

            JournalController.Instance.SaveJournalItem(ji, -1);
        }
    }
}