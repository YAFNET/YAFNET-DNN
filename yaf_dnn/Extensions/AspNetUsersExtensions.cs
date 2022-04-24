﻿/* Yet Another Forum.NET
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

namespace YAF.DotNetNuke.Extensions;

/// <summary>
/// The asp net users extensions.
/// </summary>
public static class AspNetUsersExtensions
{
    #region Public Methods

    /// <summary>
    /// Converts UserInfo to AspNetUsers
    /// </summary>
    /// <param name="userInfo">
    /// The user info.
    /// </param>
    /// <returns>
    /// The <see cref="AspNetUsers"/>.
    /// </returns>
    public static AspNetUsers ToAspNetUsers(this UserInfo userInfo)
    {
        var user = new AspNetUsers
                       {
                           Id = userInfo.UserID.ToString(),
                           UserName = userInfo.Username,
                           Email = userInfo.Email,
                           IsApproved = !userInfo.IsDeleted,
                           CreateDate = userInfo.CreatedOnDate,
                           LastPasswordChangedDate = DateTime.Now,
                           LastLockoutDate = DateTime.MinValue.AddYears(1902),
                           FailedPasswordAnswerAttemptWindowStart = DateTime.MinValue.AddYears(1902),
                           FailedPasswordAttemptWindowStart = DateTime.MinValue.AddYears(1902),
                           Profile_Birthday = DateTime.MinValue.AddYears(1902)
                       };

        /*user.LastLoginDate = DateTime.Now;
        user.LastActivityDate = DateTime.Now;*/

        return user;
    }

    #endregion
}