﻿/* Yet Another Forum.NET
 * Copyright (C) 2003-2005 Bjørnar Henden
 * Copyright (C) 2006-2013 Jaben Cargman
 * Copyright (C) 2014-2021 Ingo Herbote
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

namespace YAF.DotNetNuke.Components.WebAPI
{
    using System.Web.Http;

    using global::DotNetNuke.Web.Api;

    using YAF.Core.Context;
    using YAF.Core.Extensions;
    using YAF.Core.Model;
    using YAF.Core.Utilities.StringUtils;
    using YAF.Types;
    using YAF.Types.Extensions;
    using YAF.Types.Interfaces;
    using YAF.Types.Interfaces.Identity;
    using YAF.Types.Models;

    /// <summary>
    /// The YAF ThankYou controller.
    /// </summary>
    public class ThankYouController : DnnApiController, IHaveServiceLocator
    {
        #region Properties

        /// <summary>
        ///   Gets ServiceLocator.
        /// </summary>
        public IServiceLocator ServiceLocator => BoardContext.Current.ServiceLocator;

        #endregion

        /// <summary>
        /// Add Thanks to post
        /// </summary>
        /// <param name="messageId">
        /// The message Id.
        /// </param>
        /// <returns>
        /// Returns ThankYou Info
        /// </returns>
        [DnnAuthorize]
        [HttpPost]
        public IHttpActionResult GetThanks([NotNull] int messageId)
        {
            var membershipUser = this.Get<IAspNetUsersHelper>().GetUser();

            if (membershipUser == null)
            {
                return this.NotFound();
            }

            var message = this.GetRepository<Message>().GetById(messageId);

            var userName = this.Get<IUserDisplayName>().GetNameById(message.UserID);

            // if the user is empty, return a null object...
            return userName.IsNotSet()
                ? this.NotFound()
                : this.Ok(
                    this.Get<IThankYou>().GetThankYou(
                        new UnicodeEncoder().XSSEncode(userName),
                        "BUTTON_THANKSDELETE",
                        "BUTTON_THANKSDELETE_TT",
                        messageId));
        }

        /// <summary>
        /// Add Thanks to post
        /// </summary>
        /// <param name="id">
        /// The message Id.
        /// </param>
        /// <returns>
        /// Returns ThankYou Info
        /// </returns>
        [DnnAuthorize]
        [HttpPost]
        public IHttpActionResult AddThanks([NotNull] int id)
        {
            var membershipUser = BoardContext.Current.Get<IAspNetUsersHelper>().GetUser();

            if (membershipUser == null)
            {
                return this.NotFound();
            }

            var fromUser = BoardContext.Current.Get<IAspNetUsersHelper>()
                .GetUserFromProviderUserKey(membershipUser.Id);

            var message = this.GetRepository<Message>().GetById(id);

            var userName = this.Get<IUserDisplayName>().GetNameById(message.UserID);

            this.GetRepository<Thanks>().AddMessageThanks(fromUser.ID, message.UserID, id);

            this.Get<IActivityStream>().AddThanksReceivedToStream(message.UserID, message.TopicID, id, fromUser.ID);
            this.Get<IActivityStream>().AddThanksGivenToStream(fromUser.ID, message.TopicID, id, message.UserID);

            // if the user is empty, return a null object...
            return userName.IsNotSet()
                ? this.NotFound()
                : this.Ok(
                    this.Get<IThankYou>().CreateThankYou(
                        new UnicodeEncoder().XSSEncode(userName),
                        "BUTTON_THANKSDELETE",
                        "BUTTON_THANKSDELETE_TT",
                        id));
        }

        /// <summary>
        /// This method is called asynchronously when the user clicks on "Remove Thank" button.
        /// </summary>
        /// <param name="id">
        /// The message Id.
        /// </param>
        /// <returns>
        /// Returns ThankYou Info
        /// </returns>
        [DnnAuthorize]
        [HttpPost]
        public IHttpActionResult RemoveThanks([NotNull] int id)
        {
            var message = this.GetRepository<Message>().GetById(id);

            var userName = this.Get<IUserDisplayName>().GetNameById(message.UserID);

            this.GetRepository<Activity>().Delete(a => a.MessageID == id && (a.Flags == 1024 || a.Flags == 2048));

            return this.Ok(this.Get<IThankYou>().CreateThankYou(userName, "BUTTON_THANKS", "BUTTON_THANKS_TT", id));
        }
    }
}