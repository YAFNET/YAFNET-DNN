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

 * http://www.apache.org/licenses/LICENSE-2.0

 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 */

namespace YAF.DotNetNuke.Components.Integration;

using System.Collections.Generic;

/// <summary>
/// The asp net role manager.
/// </summary>
[ExportService(ServiceLifetimeScope.Singleton)]
public class AspNetRoleManager : RoleManager<AspNetRoles>, IAspNetRoleManager
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AspNetRoleManager"/> class.
    /// </summary>
    /// <param name="store">
    /// The store.
    /// </param>
    public AspNetRoleManager(IRoleStore<AspNetRoles, string> store)
        : base(store)
    {
    }

    /// <summary>
    /// The roles.
    /// </summary>
    public virtual IQueryable<AspNetRoles> AspNetRoles => base.Roles;

    /// <summary>
    /// The get roles.
    /// </summary>
    /// <param name="user">
    /// The user.
    /// </param>
    /// <returns>
    /// The <see cref="IList"/>.
    /// </returns>
    public IList<string> GetRoles(AspNetUsers user)
    {
        var roles = BoardContext.Current.GetRepository<UserRoles>().Get(r => r.UserID == user.Id.ToType<int>())
            .Select(r => r.RoleID).ToArray();

        var roleNames = BoardContext.Current.GetRepository<Roles>().GetAll().Where(r => roles.Contains(r.Id))
            .Select(r => r.Name).ToList();

        return Task.FromResult<IList<string>>(roleNames).Result;
    }

    /// <summary>
    /// The find by name.
    /// </summary>
    /// <param name="roleName">
    /// The role name.
    /// </param>
    /// <returns>
    /// The <see cref="AspNetRoles"/>.
    /// </returns>
    public AspNetRoles FindByName(string roleName)
    {
        return this.FindByNameAsync(roleName).Result;
    }

    /// <summary>
    /// Create a role
    /// </summary>
    /// <param name="role">
    /// The role.
    /// </param>
    /// <returns>
    /// The <see cref="IdentityResult"/>.
    /// </returns>
    public IdentityResult Create(
        AspNetRoles role)
    {
        return this.CreateAsync(role).Result;
    }

    /// <summary>
    /// Delete a role
    /// </summary>
    /// <param name="role">
    /// The role.
    /// </param>
    /// <returns>
    /// The <see cref="IdentityResult"/>.
    /// </returns>
    public IdentityResult Delete(
        AspNetRoles role)
    {
        return this.DeleteAsync(role).Result;
    }

    /// <summary>
    /// Returns true if the role exists
    /// </summary>
    /// <param name="roleName">
    /// The role Name.
    /// </param>
    /// <returns>
    /// The <see cref="bool"/>.
    /// </returns>
    public bool RoleExists(
        string roleName)
    {
        return this.RoleExistsAsync(roleName).Result;
    }
}