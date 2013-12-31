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
    using System.Linq;
    using System.Text;

    using global::DotNetNuke.Entities.Portals;
    using global::DotNetNuke.Entities.Users;
    using global::DotNetNuke.Security.Roles;
    using global::DotNetNuke.Services.Journal;

    using YAF.DotNetNuke.Components.Controllers;
    using YAF.Types.Attributes;
    using YAF.Types.Constants;
    using YAF.Types.Extensions;
    using YAF.Types.Interfaces;
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
                             SecuritySet = this.GetSecuritySet(forumID, portalSettings.PortalId),
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
                             SecuritySet = this.GetSecuritySet(forumID, portalSettings.PortalId),
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
            
            // TODO:
            /*if (SocialGroupId > 0)
            {
                ji.SocialGroupId = SocialGroupId;
            }*/

            JournalController.Instance.SaveJournalItem(ji, -1);
        }

        /// <summary>
        /// Gets the security set for the forum.
        /// </summary>
        /// <param name="forumID">The forum unique identifier.</param>
        /// <param name="portalID">The portal unique identifier.</param>
        /// <returns>Returns The Security Set for the Forum including all Roles which have Read Access</returns>
        private string GetSecuritySet(int forumID, int portalID)
        {
            var forumAccessList = Data.GetReadAccessListForForum(forumID);

            var dnnRoles = new RoleController().GetPortalRoles(portalID).Cast<RoleInfo>().ToList();

            var securitySet = new StringBuilder();

            foreach (var forumAccess in forumAccessList)
            {
                if (!forumAccess.Flags.ReadAccess)
                {
                    continue;
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
            }

            return securitySet.ToString();
        }
    }
}