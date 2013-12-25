/* Yet Another Forum.net
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
    #region Using

    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net.Mail;

    using global::DotNetNuke.Entities.Controllers;
    using global::DotNetNuke.Services.Mail;

    using YAF.Types;
    using YAF.Types.Attributes;
    using YAF.Types.Interfaces;

    using MailPriority = global::DotNetNuke.Services.Mail.MailPriority;

    #endregion

    /// <summary>
    /// Functions to send email via SMTP
    /// </summary>
    [ExportService(ServiceLifetimeScope.Singleton)]
    public class SendMail : ISendMail
    {
        #region Public Methods

        /// <summary>
        /// Creates a SMTP Client and sends a MailMessage.
        /// </summary>
        /// <param name="mailMessage">
        /// The message.
        /// </param>
        public void Send([NotNull] MailMessage mailMessage)
        {
            CodeContracts.VerifyNotNull(mailMessage, "mailMessage");

            var settings = HostController.Instance.GetSettingsDictionary();

            var body = string.Empty;

            bool mailIsHtml = false;

            if (mailMessage.AlternateViews.Count > 0)
            {
                var altView = mailMessage.AlternateViews[mailMessage.AlternateViews.Count > 1 ? 1 : 0];

                mailIsHtml = altView.ContentType.MediaType.Equals("text/html");

                using (var reader = new StreamReader(altView.ContentStream))
                {
                    body = reader.ReadToEnd();
                }
            }

            Mail.SendMail(
                mailMessage.From.Address,
                mailMessage.To.ToString(),
                string.Empty,
                string.Empty,
                MailPriority.Normal,
                mailMessage.Subject,
                mailIsHtml ? MailFormat.Html : MailFormat.Text,
                mailMessage.BodyEncoding,
                body,
                string.Empty,
                settings["SMTPServer"],
                settings["SMTPAuthentication"],
                settings["SMTPUsername"],
                settings["SMTPPassword"]);
        }

        #endregion

        #region Implemented Interfaces

        #region ISendMail

        /// <summary>
        /// Sends all MailMessages via the SMTP Client. Doesn't handle any exceptions.
        /// </summary>
        /// <param name="messages">The messages.</param>
        /// <param name="handleException"> The handle exception action.</param>
        public void SendAll([NotNull] IEnumerable<MailMessage> messages, [CanBeNull] Action<MailMessage, Exception> handleException = null)
        {
            var mailMessages = messages as IList<MailMessage> ?? messages.ToList();
            
            CodeContracts.VerifyNotNull(mailMessages, "messages");

            foreach (var mailMessage in mailMessages)
            {
                try
                {
                    // send the message...
                    this.Send(mailMessage);
                }
                catch (Exception ex)
                {
                    if (handleException != null)
                    {
                        handleException(mailMessage, ex);
                    }
                    else
                    {
                        // don't handle here...
                        throw;
                    }
                }
            }
        }

        #endregion

        #endregion
    }
}