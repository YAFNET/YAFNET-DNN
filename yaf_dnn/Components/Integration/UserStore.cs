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
using System.Security.Claims;

/// <summary>
/// The user store.
/// </summary>
[ExportService(ServiceLifetimeScope.Singleton)]
public class UserStore : IUserLoginStore<AspNetUsers>,
                         IUserClaimStore<AspNetUsers>,
                         IUserRoleStore<AspNetUsers>,
                         IUserPasswordStore<AspNetUsers>,
                         IUserSecurityStampStore<AspNetUsers>,
                         IQueryableUserStore<AspNetUsers>,
                         IUserEmailStore<AspNetUsers>,
                         IUserPhoneNumberStore<AspNetUsers>,
                         IUserTwoFactorStore<AspNetUsers, string>,
                         IUserLockoutStore<AspNetUsers, string>,
                         IUserStore<AspNetUsers>,
                         IHaveServiceLocator
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserStore"/> class.
    /// </summary>
    /// <param name="serviceLocator">
    /// The service locator.
    /// </param>
    public UserStore([NotNull] IServiceLocator serviceLocator)
    {
        this.ServiceLocator = serviceLocator;
    }

    /// <summary>
    /// Gets the service locator.
    /// </summary>
    public IServiceLocator ServiceLocator { get; }

    /// <summary>
    /// The users.
    /// </summary>
    public virtual IQueryable<AspNetUsers> Users => this.GetRepository<AspNetUsers>().GetAll().AsQueryable();

    /// <summary>
    /// The add login async.
    /// </summary>
    /// <param name="user">
    /// The user.
    /// </param>
    /// <param name="login">
    /// The login.
    /// </param>
    /// <returns>
    /// The <see cref="Task"/>.
    /// </returns>
    public virtual Task AddLoginAsync([NotNull] AspNetUsers user, [NotNull] UserLoginInfo login)
    {
        CodeContracts.VerifyNotNull(user);
        CodeContracts.VerifyNotNull(login);

        var userLogin = new AspNetUserLogins
                            {
                                UserId = user.Id, ProviderKey = login.ProviderKey, LoginProvider = login.LoginProvider
                            };

        this.GetRepository<AspNetUserLogins>().Insert(userLogin, false);

        return Task.FromResult(0);
    }

    /// <summary>
    /// The find async.
    /// </summary>
    /// <param name="login">
    /// The login.
    /// </param>
    /// <returns>
    /// The <see cref="Task"/>.
    /// </returns>
    public virtual Task<AspNetUsers> FindAsync([NotNull] UserLoginInfo login)
    {
        var userLogin = this.GetRepository<AspNetUserLogins>().GetSingle(
            i => i.LoginProvider == login.LoginProvider && i.ProviderKey == login.ProviderKey);

        return userLogin is null ? null : Task.FromResult(this.GetRepository<AspNetUsers>().GetSingle(u => u.Id == userLogin.UserId));
    }

    /// <summary>
    /// The get logins async.
    /// </summary>
    /// <param name="user">
    /// The user.
    /// </param>
    /// <returns>
    /// The <see cref="Task"/>.
    /// </returns>
    public virtual Task<IList<UserLoginInfo>> GetLoginsAsync([NotNull] AspNetUsers user)
    {
        CodeContracts.VerifyNotNull(user);

        var logins = this.GetRepository<AspNetUserLogins>().Get(l => l.UserId == user.Id)
            .Select(l => new UserLoginInfo(l.LoginProvider, l.ProviderKey));

        IList<UserLoginInfo> result = logins.ToList();
        return Task.FromResult(result);
    }

    /// <summary>
    /// The remove login async.
    /// </summary>
    /// <param name="user">
    /// The user.
    /// </param>
    /// <param name="login">
    /// The login.
    /// </param>
    /// <returns>
    /// The <see cref="Task"/>.
    /// </returns>
    public virtual Task RemoveLoginAsync([NotNull] AspNetUsers user, [NotNull] UserLoginInfo login)
    {
        CodeContracts.VerifyNotNull(user);
        CodeContracts.VerifyNotNull(login);

        return Task.FromResult(this.GetRepository<AspNetUserLogins>().Delete(
            l => l.UserId == user.Id && l.ProviderKey == login.ProviderKey &&
                 l.LoginProvider == login.LoginProvider));
    }

    /// <summary>
    /// The create async.
    /// </summary>
    /// <param name="user">
    /// The user.
    /// </param>
    /// <returns>
    /// The <see cref="Task"/>.
    /// </returns>
    public virtual Task CreateAsync([NotNull] AspNetUsers user)
    {
        CodeContracts.VerifyNotNull(user);

        this.GetRepository<AspNetUsers>().Insert(user, false);

        return Task.FromResult(0);
    }

    /// <summary>
    /// The delete async.
    /// </summary>
    /// <param name="user">
    /// The user.
    /// </param>
    /// <returns>
    /// The <see cref="Task"/>.
    /// </returns>
    public virtual Task DeleteAsync([NotNull] AspNetUsers user)
    {
        CodeContracts.VerifyNotNull(user);

        return Task.FromResult(this.GetRepository<AspNetUsers>().Delete(u => u.Id == user.Id));
    }

    /// <summary>
    /// The find by id async.
    /// </summary>
    /// <param name="userId">
    /// The user id.
    /// </param>
    /// <returns>
    /// The <see cref="Task"/>.
    /// </returns>
    public virtual Task<AspNetUsers> FindByIdAsync([NotNull] string userId)
    {
        CodeContracts.VerifyNotNull(userId);

        var dnnUser = UserController.GetUserById(PortalController.Instance.GetCurrentSettings().PortalId, userId.ToType<int>());

        return Task.FromResult(dnnUser.ToAspNetUsers());
    }

    /// <summary>
    /// The find by name async.
    /// </summary>
    /// <param name="userName">
    /// The user name.
    /// </param>
    /// <returns>
    /// The <see cref="Task"/>.
    /// </returns>
    public virtual Task<AspNetUsers> FindByNameAsync([NotNull] string userName)
    {
        CodeContracts.VerifyNotNull(userName);

        var dnnUser = UserController.GetUserByName(userName);

        return Task.FromResult(dnnUser.ToAspNetUsers());
    }

    /// <summary>
    /// The update async.
    /// </summary>
    /// <param name="user">
    /// The user.
    /// </param>
    /// <returns>
    /// The <see cref="Task"/>.
    /// </returns>
    public virtual Task UpdateAsync([NotNull] AspNetUsers user)
    {
        CodeContracts.VerifyNotNull(user);

        return this.UpdateUser(user);
    }

    /// <summary>
    /// The dispose.
    /// </summary>
    public virtual void Dispose()
    {
    }

    /// <summary>
    /// The add claim async.
    /// </summary>
    /// <param name="user">
    /// The user.
    /// </param>
    /// <param name="claim">
    /// The claim.
    /// </param>
    /// <returns>
    /// The <see cref="Task"/>.
    /// </returns>
    public virtual Task AddClaimAsync([NotNull] AspNetUsers user, [NotNull] Claim claim)
    {
        CodeContracts.VerifyNotNull(user);
        CodeContracts.VerifyNotNull(claim);

        var userClaim = new AspNetUserClaims
                            {
                                UserId = user.Id, ClaimType = claim.ValueType, ClaimValue = claim.Value
                            };

        var result = this.GetRepository<AspNetUserClaims>().Insert(userClaim, false);
        this.UpdateUser(user);
        return Task.FromResult(result);
    }

    /// <summary>
    /// The get claims async.
    /// </summary>
    /// <param name="user">
    /// The user.
    /// </param>
    /// <returns>
    /// The <see cref="Task"/>.
    /// </returns>
    public virtual Task<IList<Claim>> GetClaimsAsync([NotNull] AspNetUsers user)
    {
        CodeContracts.VerifyNotNull(user);

        var claims = this.GetRepository<AspNetUserClaims>().Get(l => l.UserId == user.Id)
            .Select(c => new Claim(c.ClaimType, c.ClaimValue));

        IList<Claim> result = claims.ToList();
        return Task.FromResult(result);
    }

    /// <summary>
    /// The remove claim async.
    /// </summary>
    /// <param name="user">
    /// The user.
    /// </param>
    /// <param name="claim">
    /// The claim.
    /// </param>
    /// <returns>
    /// The <see cref="Task"/>.
    /// </returns>
    public virtual Task RemoveClaimAsync([NotNull] AspNetUsers user, [NotNull] Claim claim)
    {
        CodeContracts.VerifyNotNull(user);
        CodeContracts.VerifyNotNull(claim);

        var result = this.GetRepository<AspNetUserClaims>().Delete(
            c => c.UserId == user.Id && c.ClaimValue == claim.Value && c.ClaimType == claim.Type);

        this.UpdateUser(user);

        return Task.FromResult(result);
    }

    /// <summary>
    /// The add to role async.
    /// </summary>
    /// <param name="user">
    /// The user.
    /// </param>
    /// <param name="roleName">
    /// The role name.
    /// </param>
    /// <returns>
    /// The <see cref="Task"/>.
    /// </returns>
    public virtual Task AddToRoleAsync([NotNull] AspNetUsers user, [NotNull] string roleName)
    {
        CodeContracts.VerifyNotNull(user);
        CodeContracts.VerifyNotNull(roleName);

        var role = this.GetRepository<Roles>().GetSingle(r => r.Name == roleName);

        if (role is null)
        {
            return Task.FromResult(0);
        }

        var newUserRole = new UserRoles { RoleID = role.Id, UserID = user.Id.ToType<int>() };

        this.GetRepository<UserRoles>().Insert(newUserRole, false);
        this.UpdateUser(user);

        return Task.FromResult(0);
    }

    /// <summary>
    /// The get roles async.
    /// </summary>
    /// <param name="user">
    /// The user.
    /// </param>
    /// <returns>
    /// The <see cref="Task"/>.
    /// </returns>
    public virtual Task<IList<string>> GetRolesAsync([NotNull] AspNetUsers user)
    {
        CodeContracts.VerifyNotNull(user);

        var roles = this.GetRepository<UserRoles>().Get(r => r.UserID == user.Id.ToType<int>())
            .Select(r => r.RoleID).ToArray();

        var roleNames = this.GetRepository<Roles>().Get(r => roles.Contains(r.Id)).Select(r => r.Name)
            .ToList();

        return Task.FromResult<IList<string>>(roleNames);
    }

    /// <summary>
    /// The is in role async.
    /// </summary>
    /// <param name="user">
    /// The user.
    /// </param>
    /// <param name="roleName">
    /// The role name.
    /// </param>
    /// <returns>
    /// The <see cref="Task"/>.
    /// </returns>
    public virtual Task<bool> IsInRoleAsync([NotNull] AspNetUsers user, [NotNull] string roleName)
    {
        CodeContracts.VerifyNotNull(user);
        CodeContracts.VerifyNotNull(roleName);

        var role = this.GetRepository<Roles>().GetSingle(r => r.Name == roleName);

        var isInRole = false;

        if (role != null)
        {
            isInRole =
                this.GetRepository<UserRoles>().Count(r => r.RoleID == role.Id && r.UserID == user.Id.ToType<int>()) > 0;
        }

        return Task.FromResult(isInRole);
    }

    /// <summary>
    /// The remove from role async.
    /// </summary>
    /// <param name="user">
    /// The user.
    /// </param>
    /// <param name="roleName">
    /// The role name.
    /// </param>
    /// <returns>
    /// The <see cref="Task"/>.
    /// </returns>
    public virtual Task RemoveFromRoleAsync([NotNull] AspNetUsers user, [NotNull] string roleName)
    {
        CodeContracts.VerifyNotNull(user);
        CodeContracts.VerifyNotNull(roleName);

        var role = this.GetRepository<Roles>().GetSingle(r => r.Name == roleName);

        if (role is null)
        {
            return Task.FromResult(0);
        }

        var result = this.GetRepository<UserRoles>().Delete(r => r.UserID == user.Id.ToType<int>() && r.RoleID == role.Id);
        await this.UpdateUser(user).ConfigureAwait(false);
        return Task.FromResult(result);
    }

    /// <summary>
    /// The get password hash async.
    /// </summary>
    /// <param name="user">
    /// The user.
    /// </param>
    /// <returns>
    /// The <see cref="Task"/>.
    /// </returns>
    public virtual Task<string> GetPasswordHashAsync([NotNull] AspNetUsers user)
    {
        CodeContracts.VerifyNotNull(user);

        return Task.FromResult(user.PasswordHash);
    }

    /// <summary>
    /// The has password async.
    /// </summary>
    /// <param name="user">
    /// The user.
    /// </param>
    /// <returns>
    /// The <see cref="Task"/>.
    /// </returns>
    public virtual async Task<bool> HasPasswordAsync([NotNull] AspNetUsers user)
    {
        var passwordHash = await this.GetPasswordHashAsync(user);
        return passwordHash.IsNotSet();
    }

    /// <summary>
    /// The set password hash async.
    /// </summary>
    /// <param name="user">
    /// The user.
    /// </param>
    /// <param name="passwordHash">
    /// The password hash.
    /// </param>
    /// <returns>
    /// The <see cref="Task"/>.
    /// </returns>
    public virtual Task SetPasswordHashAsync([NotNull] AspNetUsers user, [NotNull] string passwordHash)
    {
        CodeContracts.VerifyNotNull(user);

        user.PasswordHash = passwordHash;
        return this.UpdateUser(user);
    }

    /// <summary>
    /// The get security stamp async.
    /// </summary>
    /// <param name="user">
    /// The user.
    /// </param>
    /// <returns>
    /// The <see cref="Task"/>.
    /// </returns>
    public virtual Task<string> GetSecurityStampAsync([NotNull] AspNetUsers user)
    {
        CodeContracts.VerifyNotNull(user);

        return Task.FromResult(user.SecurityStamp);
    }

    /// <summary>
    /// The set security stamp async.
    /// </summary>
    /// <param name="user">
    /// The user.
    /// </param>
    /// <param name="stamp">
    /// The stamp.
    /// </param>
    /// <returns>
    /// The <see cref="Task"/>.
    /// </returns>
    public virtual Task SetSecurityStampAsync([NotNull] AspNetUsers user, [NotNull] string stamp)
    {
        CodeContracts.VerifyNotNull(user);

        user.SecurityStamp = stamp;
        return this.UpdateUser(user);
    }

    /// <summary>
    /// The find by email async.
    /// </summary>
    /// <param name="email">
    /// The email.
    /// </param>
    /// <returns>
    /// The <see cref="Task"/>.
    /// </returns>
    public virtual Task<AspNetUsers> FindByEmailAsync([NotNull] string email)
    {
        return Task.FromResult(this.GetRepository<AspNetUsers>().GetSingle(u => u.Email == email));
    }

    /// <summary>
    /// The get email async.
    /// </summary>
    /// <param name="user">
    /// The user.
    /// </param>
    /// <returns>
    /// The <see cref="Task"/>.
    /// </returns>
    public virtual Task<string> GetEmailAsync([NotNull] AspNetUsers user)
    {
        CodeContracts.VerifyNotNull(user);

        return Task.FromResult(user.Email);
    }

    /// <summary>
    /// The get email confirmed async.
    /// </summary>
    /// <param name="user">
    /// The user.
    /// </param>
    /// <returns>
    /// The <see cref="Task"/>.
    /// </returns>
    public virtual Task<bool> GetEmailConfirmedAsync([NotNull] AspNetUsers user)
    {
        CodeContracts.VerifyNotNull(user);

        return Task.FromResult(user.EmailConfirmed);
    }

    /// <summary>
    /// The set email async.
    /// </summary>
    /// <param name="user">
    /// The user.
    /// </param>
    /// <param name="email">
    /// The email.
    /// </param>
    /// <returns>
    /// The <see cref="Task"/>.
    /// </returns>
    public virtual Task SetEmailAsync([NotNull] AspNetUsers user, string email)
    {
        CodeContracts.VerifyNotNull(user);

        user.Email = email;
        return this.UpdateUser(user);
    }

    /// <summary>
    /// The set email confirmed async.
    /// </summary>
    /// <param name="user">
    /// The user.
    /// </param>
    /// <param name="confirmed">
    /// The confirmed.
    /// </param>
    /// <returns>
    /// The <see cref="Task"/>.
    /// </returns>
    public virtual Task SetEmailConfirmedAsync([NotNull] AspNetUsers user, bool confirmed)
    {
        CodeContracts.VerifyNotNull(user);

        user.EmailConfirmed = confirmed;
        return this.UpdateUser(user);
    }

    /// <summary>
    /// The get phone number async.
    /// </summary>
    /// <param name="user">
    /// The user.
    /// </param>
    /// <returns>
    /// The <see cref="Task"/>.
    /// </returns>
    public virtual Task<string> GetPhoneNumberAsync([NotNull] AspNetUsers user)
    {
        CodeContracts.VerifyNotNull(user);

        return Task.FromResult(user.PhoneNumber);
    }

    /// <summary>
    /// The get phone number confirmed async.
    /// </summary>
    /// <param name="user">
    /// The user.
    /// </param>
    /// <returns>
    /// The <see cref="Task"/>.
    /// </returns>
    public virtual Task<bool> GetPhoneNumberConfirmedAsync([NotNull] AspNetUsers user)
    {
        CodeContracts.VerifyNotNull(user);

        return Task.FromResult(user.PhoneNumberConfirmed);
    }

    /// <summary>
    /// The set phone number async.
    /// </summary>
    /// <param name="user">
    /// The user.
    /// </param>
    /// <param name="phoneNumber">
    /// The phone number.
    /// </param>
    /// <returns>
    /// The <see cref="Task"/>.
    /// </returns>
    public virtual Task SetPhoneNumberAsync([NotNull] AspNetUsers user, string phoneNumber)
    {
        CodeContracts.VerifyNotNull(user);

        user.PhoneNumber = phoneNumber;
        return this.UpdateUser(user);
    }

    /// <summary>
    /// The set phone number confirmed async.
    /// </summary>
    /// <param name="user">
    /// The user.
    /// </param>
    /// <param name="confirmed">
    /// The confirmed.
    /// </param>
    /// <returns>
    /// The <see cref="Task"/>.
    /// </returns>
    public virtual Task SetPhoneNumberConfirmedAsync([NotNull] AspNetUsers user, bool confirmed)
    {
        CodeContracts.VerifyNotNull(user);

        user.PhoneNumberConfirmed = confirmed;
        return this.UpdateUser(user);
    }

    /// <summary>
    /// The get two factor enabled async.
    /// </summary>
    /// <param name="user">
    /// The user.
    /// </param>
    /// <returns>
    /// The <see cref="Task"/>.
    /// </returns>
    public virtual Task<bool> GetTwoFactorEnabledAsync([NotNull] AspNetUsers user)
    {
        CodeContracts.VerifyNotNull(user);

        return Task.FromResult(user.TwoFactorEnabled);
    }

    /// <summary>
    /// The set two factor enabled async.
    /// </summary>
    /// <param name="user">
    /// The user.
    /// </param>
    /// <param name="enabled">
    /// The enabled.
    /// </param>
    /// <returns>
    /// The <see cref="Task"/>.
    /// </returns>
    public virtual Task SetTwoFactorEnabledAsync([NotNull] AspNetUsers user, bool enabled)
    {
        CodeContracts.VerifyNotNull(user);

        user.TwoFactorEnabled = enabled;
        return this.UpdateUser(user);
    }

    /// <summary>
    /// The get access failed count async.
    /// </summary>
    /// <param name="user">
    /// The user.
    /// </param>
    /// <returns>
    /// The <see cref="Task"/>.
    /// </returns>
    public virtual Task<int> GetAccessFailedCountAsync([NotNull] AspNetUsers user)
    {
        CodeContracts.VerifyNotNull(user);

        return Task.FromResult(user.AccessFailedCount);
    }

    /// <summary>
    /// The get lockout enabled async.
    /// </summary>
    /// <param name="user">
    /// The user.
    /// </param>
    /// <returns>
    /// The <see cref="Task"/>.
    /// </returns>
    public virtual Task<bool> GetLockoutEnabledAsync([NotNull] AspNetUsers user)
    {
        CodeContracts.VerifyNotNull(user);

        return Task.FromResult(user.LockoutEnabled);
    }

    /// <summary>
    /// The get lockout end date async.
    /// </summary>
    /// <param name="user">
    /// The user.
    /// </param>
    /// <returns>
    /// The <see cref="Task"/>.
    /// </returns>
    public virtual Task<DateTimeOffset> GetLockoutEndDateAsync([NotNull] AspNetUsers user)
    {
        CodeContracts.VerifyNotNull(user);

        return Task.FromResult(
            user.LockoutEndDateUtc.HasValue
                ? new DateTimeOffset(DateTime.SpecifyKind(user.LockoutEndDateUtc.Value, DateTimeKind.Utc))
                : new DateTimeOffset());
    }

    /// <summary>
    /// The increment access failed count async.
    /// </summary>
    /// <param name="user">
    /// The user.
    /// </param>
    /// <returns>
    /// The <see cref="Task"/>.
    /// </returns>
    public virtual async Task<int> IncrementAccessFailedCountAsync([NotNull] AspNetUsers user)
    {
        CodeContracts.VerifyNotNull(user);

        user.AccessFailedCount++;

        await this.UpdateUser(user).ConfigureAwait(false);

        return user.AccessFailedCount;
    }

    /// <summary>
    /// The reset access failed count async.
    /// </summary>
    /// <param name="user">
    /// The user.
    /// </param>
    /// <returns>
    /// The <see cref="Task"/>.
    /// </returns>
    public virtual Task ResetAccessFailedCountAsync([NotNull] AspNetUsers user)
    {
        CodeContracts.VerifyNotNull(user);

        user.AccessFailedCount = 0;
        return this.UpdateUser(user);
    }

    /// <summary>
    /// The set lockout enabled async.
    /// </summary>
    /// <param name="user">
    /// The user.
    /// </param>
    /// <param name="enabled">
    /// The enabled.
    /// </param>
    /// <returns>
    /// The <see cref="Task"/>.
    /// </returns>
    public virtual Task SetLockoutEnabledAsync([NotNull] AspNetUsers user, bool enabled)
    {
        CodeContracts.VerifyNotNull(user);

        user.LockoutEnabled = enabled;
        return this.UpdateUser(user);
    }

    /// <summary>
    /// The set lockout end date async.
    /// </summary>
    /// <param name="user">
    /// The user.
    /// </param>
    /// <param name="lockoutEnd">
    /// The lockout end.
    /// </param>
    /// <returns>
    /// The <see cref="Task"/>.
    /// </returns>
    public virtual Task SetLockoutEndDateAsync([NotNull] AspNetUsers user, DateTimeOffset lockoutEnd)
    {
        CodeContracts.VerifyNotNull(user);

        user.LockoutEndDateUtc = lockoutEnd.UtcDateTime;
        return this.UpdateUser(user);
    }

    /// <summary>
    /// The update user.
    /// </summary>
    /// <param name="user">
    /// The user.
    /// </param>
    /// <returns>
    /// The <see cref="Task"/>.
    /// </returns>
    private Task UpdateUser([NotNull] AspNetUsers user)
    {
        this.GetRepository<AspNetUsers>().Update(user);

        return Task.FromResult(0);
    }
}