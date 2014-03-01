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

namespace YAF.DotNetNuke.Components.Utils
{
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Web.Security;

    using global::DotNetNuke.Common;
    using global::DotNetNuke.Common.Utilities;
    using global::DotNetNuke.Entities.Modules;
    using global::DotNetNuke.Entities.Users;

    using YAF.Classes;
    using YAF.Classes.Data;
    using YAF.Core;
    using YAF.DotNetNuke.Components.Controllers;
    using YAF.Types;
    using YAF.Types.Constants;
    using YAF.Types.EventProxies;
    using YAF.Types.Extensions;
    using YAF.Types.Interfaces;
    using YAF.Utils;

    /// <summary>
    /// YAF DNN Profile Synchronization 
    /// </summary>
    public class ProfileSyncronizer : PortalModuleBase
    {
        /// <summary>
        /// Synchronizes The YAF Profile with DNN Profile or reverse if
        /// one profile is newer then the other
        /// </summary>
        /// <param name="yafUserId">The YAF UserId</param>
        /// <param name="yafUserProfile">The YAF user profile.</param>
        /// <param name="yafCurrentUserData">The YAF current user data.</param>
        /// <param name="dnnUserInfo">DNN UserInfo of current User</param>
        /// <param name="membershipUser">MemberShip of current User</param>
        /// <param name="portalID">The portal ID.</param>
        /// <param name="portalGuid">The portal GUID.</param>
        /// <param name="boardId">The board Id.</param>
        /// <param name="ignoreLastUpdated">if set to <c>true</c> [ignore last updated].</param>
        public static void UpdateUserProfile(
            [NotNull] int yafUserId,
            [CanBeNull] YafUserProfile yafUserProfile,
            [CanBeNull] IUserData yafCurrentUserData,
            [NotNull] UserInfo dnnUserInfo,
            [NotNull] MembershipUser membershipUser,
            [NotNull] int portalID,
            [NotNull] Guid portalGuid,
            [NotNull] int boardId,
            [CanBeNull] bool ignoreLastUpdated = false)
        {
            try
            {
                if (yafUserProfile == null)
                {
                    yafUserProfile = YafUserProfile.GetProfile(membershipUser.UserName);
                }

                var yafTime = yafUserProfile.LastUpdatedDate;
                var dnnTime = Profile.YafDnnGetLastUpdatedProfile(dnnUserInfo.UserID);

                if (dnnTime <= yafTime & !ignoreLastUpdated)
                {
                    return;
                }

                if (yafCurrentUserData == null)
                {
                    yafCurrentUserData = new CombinedUserDataHelper(yafUserId);
                }

                SyncYafProfile(
                    yafUserId,
                    yafUserProfile,
                    yafCurrentUserData,
                    dnnUserInfo,
                    portalGuid,
                    boardId);
            }
            catch (Exception ex)
            {
                var logger = YafContext.Current.Get<ILogger>();

                if (logger != null)
                {
                    logger.Log(
                        "Error while Syncing dnn userprofile with Yaf",
                        EventLogTypes.Error,
                        null,
                        "Profile Syncronizer",
                        ex);
                }
            }
        }

        /*
        /// <summary>
        /// The get user time zone offset.
        /// </summary>
        /// <param name="userInfo">
        /// The user info.
        /// </param>
        /// <param name="portalSettings">
        /// Current Portal Settings
        /// </param>
        /// <returns>
        /// Returns the User Time Zone Offset Value
        /// </returns>
        public static int GetUserTimeZoneOffset(UserInfo userInfo, PortalSettings portalSettings)
        {
            int timeZone;

            if ((userInfo != null) && (userInfo.UserID != Null.NullInteger))
            {
                timeZone = userInfo.Profile.TimeZone;
            }
            else
            {
                if (portalSettings != null)
                {
                    timeZone = portalSettings.TimeZoneOffset;
                }
                else
                {
                    timeZone = -480;
                }
            }

            return timeZone;
        }*/

        /// <summary>
        /// Gets the name of the region info from country (English Name).
        /// </summary>
        /// <param name="countryEnglishName">Name of the country english.</param>
        /// <returns>The RegionInfo for the Country</returns>
        public static RegionInfo GetRegionInfoFromCountryName([NotNull]string countryEnglishName)
        {
            return
                CultureInfo.GetCultures(CultureTypes.SpecificCultures).Select(ci => new RegionInfo(ci.LCID)).FirstOrDefault(region => region.EnglishName.Equals(countryEnglishName));
        }

        /// <summary>
        /// DNN Profile is newer, sync YAF now
        /// NOTE : no need to manually sync Email Address
        /// </summary>
        /// <param name="yafUserId">The YAF user id.</param>
        /// <param name="yafUserProfile">The YAF user profile.</param>
        /// <param name="yafUserData">The YAF user data.</param>
        /// <param name="dnnUserInfo">The DNN user info.</param>
        /// <param name="portalGUID">The portal GUID.</param>
        /// <param name="boardId">The board Id.</param>
        private static void SyncYafProfile(int yafUserId, YafUserProfile yafUserProfile, IUserData yafUserData, UserInfo dnnUserInfo, Guid portalGUID, int boardId)
        {
            /*var userCuluture = new YafCultureInfo
            {
                LanguageFile = yafUserData.LanguageFile,
                Culture = yafUserData.CultureUser
            };

            if (dnnUserInfo.Profile.PreferredLocale.IsSet())
            {
                CultureInfo newCulture = new CultureInfo(dnnUserInfo.Profile.PreferredLocale);

                foreach (DataRow row in
                    StaticDataHelper.Cultures().Rows.Cast<DataRow>().Where(
                        row => dnnUserInfo.Profile.PreferredLocale == row["CultureTag"].ToString() || newCulture.TwoLetterISOLanguageName == row["CultureTag"].ToString()))
                {
                    userCuluture.LanguageFile = row["CultureFile"].ToString();
                    userCuluture.Culture = row["CultureTag"].ToString();
                }
            }*/

            LegacyDb.user_save(
                yafUserId,
                boardId,
                null,
                dnnUserInfo.DisplayName,
                null,
                yafUserData.TimeZone,
                yafUserData.LanguageFile,
                yafUserData.CultureUser,
                yafUserData.ThemeFile,
                yafUserData.TextEditor,
                yafUserData.UseMobileTheme,
                null,
                null,
                null,
                yafUserData.DSTUser,
                yafUserData.IsActiveExcluded,
                null);

            if (dnnUserInfo.Profile.FullName.IsSet())
            {
                yafUserProfile.RealName = dnnUserInfo.Profile.FullName;
            }

            if (dnnUserInfo.Profile.Country.IsSet() && !dnnUserInfo.Profile.Country.Equals("N/A"))
            {
                var regionInfo = GetRegionInfoFromCountryName(dnnUserInfo.Profile.Country);
                
                if (regionInfo != null)
                {
                    yafUserProfile.Country = regionInfo.TwoLetterISORegionName;
                }
            }

            if (dnnUserInfo.Profile.City.IsSet())
            {
                yafUserProfile.City = dnnUserInfo.Profile.City;
            } 

            if (dnnUserInfo.Profile.Website.IsSet())
            {
                yafUserProfile.Homepage = dnnUserInfo.Profile.Website;
            } 

            yafUserProfile.Save();

            yafUserProfile.Save();

            if (dnnUserInfo.Profile.Photo.IsSet())
            {
                SaveDnnAvatar(dnnUserInfo.Profile.Photo, yafUserId, portalGUID);
            }

            // clear the cache for this user...)
            YafContext.Current.Get<IRaiseEvent>().Raise(new UpdateUserEvent(yafUserId));

            YafContext.Current.Get<IDataCache>().Clear();
        }

        /// <summary>
        /// Save DNN Avatar as YAF Remote Avatar with relative Path.
        /// </summary>
        /// <param name="fileId">The file id.</param>
        /// <param name="yafUserId">The YAF user id.</param>
        /// <param name="portalGUID">The portal GUID.</param>
        private static void SaveDnnAvatar(string fileId, int yafUserId, Guid portalGUID)
        {
            var dnnAvatarUrl =
                Globals.ResolveUrl(
                    "~/LinkClick.aspx?fileticket={0}".FormatWith(
                        UrlUtils.EncryptParameter(fileId, portalGUID.ToString())));

            // update
            LegacyDb.user_saveavatar(
                yafUserId,
                "{0}{1}".FormatWith(BaseUrlBuilder.BaseUrl, dnnAvatarUrl),
                null,
                null);

            // clear the cache for this user...
            YafContext.Current.Get<IRaiseEvent>().Raise(new UpdateUserEvent(yafUserId));
        }
    }
}