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

namespace YAF.DotNetNuke.Components.Integration;

using System.Collections.Generic;
using System.Net.Mail;

using global::DotNetNuke.Collections;
using global::DotNetNuke.Entities.Host;
using global::DotNetNuke.Services.Mail;

using MailPriority = global::DotNetNuke.Services.Mail.MailPriority;

/// <summary>
/// Functions to send email via SMTP
/// </summary>
[ExportService(ServiceLifetimeScope.Singleton)]
public class SendMail : IMailService, IHaveServiceLocator
{
    /// <summary>
    ///     Gets the service locator.
    /// </summary>
    public IServiceLocator ServiceLocator => BoardContext.Current.ServiceLocator;

    /// <summary>
    /// Creates a SMTP Client and sends a MailMessage.
    /// </summary>
    /// <param name="mailMessage">
    /// The message.
    /// </param>
    public void Send([NotNull] MailMessage mailMessage)
    {
        CodeContracts.VerifyNotNull(mailMessage);

        var body = string.Empty;

        var mailIsHtml = false;

        if (mailMessage.AlternateViews.Count > 0)
        {
            var altView = mailMessage.AlternateViews[mailMessage.AlternateViews.Count > 1 ? 1 : 0];

            mailIsHtml = altView.ContentType.MediaType.Equals("text/html");

            using var reader = new StreamReader(altView.ContentStream);

            body = reader.ReadToEnd();
        }

        string fromAddress;

        try
        {
            fromAddress = BoardContext.Current.BoardSettings.ForumEmail;
        }
        catch (Exception)
        {
            fromAddress = mailMessage.From.Address;
        }

        Mail.SendMail(
            fromAddress,
            mailMessage.To.ToString(),
            string.Empty,
            string.Empty,
            MailPriority.Normal,
            mailMessage.Subject,
            mailIsHtml ? MailFormat.Html : MailFormat.Text,
            mailMessage.BodyEncoding,
            body,
            string.Empty,
            Host.SMTPServer,
            Host.SMTPAuthentication,
            Host.SMTPUsername,
            Host.SMTPPassword);
    }

    /// <summary>
    /// Sends all MailMessages via the SMTP Client. Doesn't handle any exceptions.
    /// </summary>
    /// <param name="messages">The messages.</param>
    public void SendAll([NotNull] IEnumerable<MailMessage> messages)
    {
        var mailMessages = messages as IList<MailMessage> ?? messages.ToList();

        CodeContracts.VerifyNotNull(mailMessages);

        mailMessages.ForEach(
            mailMessage =>
                {
                    try
                    {
                        // send the message...
                        this.Send(mailMessage);
                    }
                    catch (Exception ex)
                    {
                        this.Get<ILoggerService>().Log("Mail Error", EventLogTypes.Error, null, null, ex);
                    }
                });
    }
}