/* Yet Another Forum.NET
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

namespace YAF.DotNetNuke.Components.Utils
{
    #region Using

    using System;
    using System.Data;
    using System.Web.Security;

    using YAF.Configuration;
    using YAF.Core;
    using YAF.Core.Extensions;
    using YAF.Core.Model;
    using YAF.Core.UsersRoles;
    using YAF.Types.Constants;
    using YAF.Types.Extensions;
    using YAF.Types.Flags;
    using YAF.Types.Interfaces;
    using YAF.Types.Models;
    using YAF.Utils;
    using YAF.Utils.Helpers;

    #endregion

    /// <summary>
    /// Helps get a complete user profile from various locations
    /// </summary>
    public class CustomCombinedUserDataHelper : IUserData
    {
        #region Constants and Fields

        /// <summary>
        ///   The _membership user.
        /// </summary>
        private MembershipUser _membershipUser;

        /// <summary>
        ///   The _user DB row.
        /// </summary>
        private DataRow _userDBRow;

        /// <summary>
        ///   The _user id.
        /// </summary>
        private int? _userId;

        /// <summary>
        /// The board id.
        /// </summary>
        private readonly int boardId;

        /// <summary>
        ///   The _user profile.
        /// </summary>
        private YafUserProfile _userProfile;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomCombinedUserDataHelper"/> class. 
        /// </summary>
        /// <param name="membershipUser">
        /// The membership user.
        /// </param>
        /// <param name="userID">
        /// The user id.
        /// </param>
        /// <param name="boardID">
        /// The board identifier.
        /// </param>
        public CustomCombinedUserDataHelper(MembershipUser membershipUser, int userID, int boardID)
        {
            this._userId = userID;
            this.MembershipUser = membershipUser;
            this.boardId = boardID;
            this.InitUserData();
        }

        #endregion

        #region Properties

        /// <summary>
        ///   Gets a value indicating whether AutoWatchTopics.
        /// </summary>
        public bool AutoWatchTopics
        {
            get
            {
                int value = this.DBRow.Field<int?>("NotificationType") ?? 0;

                return (this.DBRow.Field<bool?>("AutoWatchTopics") ?? false) || value.ToEnum<UserNotificationSetting>()
                       == UserNotificationSetting.TopicsIPostToOrSubscribeTo;
            }
        }

        /// <summary>
        ///   Gets Avatar.
        /// </summary>
        public string Avatar => this.DBRow.Field<string>("Avatar");

        /// <summary>
        ///   Gets Culture.
        /// </summary>
        public string CultureUser => this.DBRow.Field<string>("CultureUser");

        /// <summary>
        ///   Gets User's Text Editor.
        /// </summary>
        public string TextEditor => this.DBRow.Field<string>("TextEditor");

        /// <summary>
        ///   Gets DBRow.
        /// </summary>
        public DataRow DBRow
        {
            get
            {
                if (this._userDBRow == null && this._userId.HasValue)
                {
                    this._userDBRow = this.GetUserRowForID(this.boardId, this._userId.Value, true);
                }

                return this._userDBRow;
            }
        }

        /// <summary>
        ///   Gets a value indicating whether  DST is Enabled.
        /// </summary>
        public bool DSTUser => this.DBRow != null && new UserFlags(this.DBRow["Flags"]).IsDST;

        /// <summary>
        /// Gets a value indicating whether DailyDigest.
        /// </summary>
        public bool DailyDigest =>
            this.DBRow.Field<bool?>("DailyDigest")
            ?? BoardContext.Current.Get<BoardSettings>().DefaultSendDigestEmail;

        /// <summary>
        /// Enable Activity Stream (If checked you get Notifications for Mentions, Quotes and Thanks.
        /// </summary>
        public bool Activity => this.DBRow.Field<bool>("Activity");

        /// <summary>
        ///   Gets DisplayName.
        /// </summary>
        public string DisplayName => this._userId.HasValue ? this.DBRow.Field<string>("DisplayName") : this.UserName;

        /// <summary>
        ///   Gets Email.
        /// </summary>
        public string Email
        {
            get
            {
                if (this.Membership == null && !this.IsGuest)
                {
                    BoardContext.Current.Get<ILogger>().Log(
                        this.UserID,
                        "CombinedUserDataHelper.get_Email",
                        $"ATTENTION! The user with id {this.UserID} and name {this.UserName} is very possibly is not in your Membership \r\n "
                        + "data but it's still in you YAF user table. The situation should not normally happen. \r\n " + "You should create a Membership data for the user first and "
                        + "then delete him from YAF user table or leave him.");
                }

                return this.IsGuest ? this.DBRow.Field<string>("Email") : this.Membership.Email;
            }
        }

        /// <summary>
        ///   Gets a value indicating whether HasAvatarImage.
        /// </summary>
        public bool HasAvatarImage => this.DBRow != null && (this.DBRow["HasAvatarImage"].ToType<bool?>() ?? false);

        /// <summary>
        ///   Gets a value indicating whether IsActiveExcluded.
        /// </summary>
        public bool IsActiveExcluded => this.DBRow != null && new UserFlags(this.DBRow["Flags"]).IsActiveExcluded;

        /// <summary>
        ///   Gets a value indicating whether IsGuest.
        /// </summary>
        public bool IsGuest => this.DBRow != null && (this.DBRow["IsGuest"].ToType<bool?>() ?? false);

        /// <summary>
        ///   Gets Joined.
        /// </summary>
        public DateTime? Joined => this.DBRow.Field<DateTime>("Joined");

        /// <summary>
        ///   Gets LanguageFile.
        /// </summary>
        public string LanguageFile => this.DBRow.Field<string>("LanguageFile");

        /// <summary>
        ///   Gets LastVisit.
        /// </summary>
        public DateTime? LastVisit => this.DBRow.Field<DateTime>("LastVisit");

        /// <summary>
        /// Gets the last IP.
        /// </summary>
        /// <value>
        /// The last IP.
        /// </value>
        public string LastIP => this.DBRow.Field<string>("IP");

        /// <summary>
        ///   Gets Membership.
        /// </summary>
        public MembershipUser Membership => this.MembershipUser;

        /// <summary>
        /// Gets NotificationSetting.
        /// </summary>
        public UserNotificationSetting NotificationSetting
        {
            get
            {
                int value = this.DBRow.Field<int?>("NotificationType") ?? 0;

                return value.ToEnum<UserNotificationSetting>();
            }
        }

        /// <summary>
        ///   Gets Number of Posts.
        /// </summary>
        public int? NumPosts => this.DBRow.Field<int>("NumPosts");

        /// <summary>
        ///   Gets a value indicating whether PMNotification.
        /// </summary>
        public bool PMNotification => this.DBRow.Field<bool?>("PMNotification") ?? true;

        /// <summary>
        ///   Gets Points.
        /// </summary>
        public int? Points => this.DBRow.Field<int>("Points");

        /// <summary>
        ///   Gets Profile.
        /// </summary>
        public IYafUserProfile Profile
        {
            get
            {
                if (this._userProfile == null && this.UserName.IsSet())
                {
                    // init the profile...
                    this._userProfile = YafUserProfile.GetProfile(this.UserName);
                }

                return this._userProfile;
            }
        }

        /// <summary>
        ///   Gets RankName.
        /// </summary>
        public string RankName => this.DBRow.Field<string>("RankName");

        /// <summary>
        ///   Gets Signature.
        /// </summary>
        public string Signature => this.DBRow.Field<string>("Signature");

        /// <summary>
        ///   Gets ThemeFile.
        /// </summary>
        public string ThemeFile => this.DBRow.Field<string>("ThemeFile");

        /// <summary>
        /// The block.
        /// </summary>
        public UserBlockFlags Block => new UserBlockFlags(this.DBRow.Field<int>("BlockFlags"));

        /// <summary>
        ///   Gets TimeZone.
        /// </summary>
        public TimeZoneInfo TimeZoneInfo
        {
            get
            {
                TimeZoneInfo timeZoneInfo;

                try
                {
                    timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(this.DBRow.Field<string>("TimeZone"));
                }
                catch (Exception)
                {
                    timeZoneInfo = TimeZoneInfo.Local;
                }

                return timeZoneInfo;
            }
        }

        /// <summary>
        ///   Gets TimeZone.
        /// </summary>
        public int? TimeZone => DateTimeHelper.GetTimeZoneOffset(this.TimeZoneInfo);

        /// <summary>
        ///   Gets UserID.
        /// </summary>
        public int UserID => _userId?.ToType<int>() ?? 0;

        /// <summary>
        ///   Gets UserName.
        /// </summary>
        public string UserName
        {
            get
            {
                if (this.MembershipUser != null)
                {
                    return this.MembershipUser.UserName;
                }

                return this._userId.HasValue ? this.DBRow.Field<string>("Name") : null;
            }
        }

        /// <summary>
        ///   Gets or sets the membership user.
        /// </summary>
        protected MembershipUser MembershipUser
        {
            get
            {
                if (this._membershipUser == null && this._userId.HasValue)
                {
                    this._membershipUser = UserMembershipHelper.GetMembershipUserById(this._userId.Value);
                }

                return this._membershipUser;
            }

            set => this._membershipUser = value;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Initializes the user data.
        /// </summary>
        /// <exception cref="System.Exception">Cannot locate user information.</exception>
        /// <exception cref="Exception">Cannot locate user information.</exception>
        private void InitUserData()
        {
            if (this.MembershipUser != null && !this._userId.HasValue)
            {
                if (this._userId == null)
                {
                    // get the user id
                    this._userId =
                        UserMembershipHelper.GetUserIDFromProviderUserKey(this.MembershipUser.ProviderUserKey);
                }
            }

            if (!this._userId.HasValue)
            {
                throw new Exception("Cannot locate user information.");
            }
        }

        /// <summary>
        /// Gets the user row for identifier.
        /// </summary>
        /// <param name="boardID">The board identifier.</param>
        /// <param name="userID">The user identifier.</param>
        /// <param name="allowUserInfoCaching">if set to <c>true</c> [allow user information caching].</param>
        /// <returns>
        /// Returns the User Row
        /// </returns>
        private DataRow GetUserRowForID(int boardID, int userID, bool allowUserInfoCaching)
        {
            if (!allowUserInfoCaching)
            {
                return BoardContext.Current.GetRepository<User>().ListAsDataTable(boardID, userID, DBNull.Value)
                    .GetFirstRow();
            }

            // get the item cached...
            return BoardContext.Current.Get<IDataCache>().GetOrSet(
                string.Format(Constants.Cache.UserListForID, userID),
                () => BoardContext.Current.GetRepository<User>().ListAsDataTable(boardID, userID, DBNull.Value),
                TimeSpan.FromMinutes(5)).GetFirstRow();
        }

        #endregion
    }
}