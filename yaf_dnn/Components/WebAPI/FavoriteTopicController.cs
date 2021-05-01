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

namespace YAF.DotNetNuke.Components.WebAPI
{
    using System;
    using System.Web.Http;

    using global::DotNetNuke.Services.Exceptions;
    using global::DotNetNuke.Web.Api;

    using YAF.Core.Context;
    using YAF.Types.Interfaces;

    /// <summary>
    /// The favorite topic controller.
    /// </summary>
    public class FavoriteTopicController : DnnApiController, IHaveServiceLocator
    {
        #region Properties

        /// <summary>
        ///   Gets ServiceLocator.
        /// </summary>
        public IServiceLocator ServiceLocator => BoardContext.Current.ServiceLocator;

        #endregion

        /// <summary>
        /// The add favorite topic.
        /// </summary>
        /// <param name="id">
        /// The id.
        /// </param>
        /// <returns>
        /// The <see cref="IHttpActionResult"/>.
        /// </returns>
        [HttpPost]
        [DnnAuthorize]
        public IHttpActionResult AddFavoriteTopic(int id)
        {
            try
            {
                return this.Ok(this.Get<IFavoriteTopic>().AddFavoriteTopic(id));
            }
            catch (Exception ex)
            {
                Exceptions.LogException(ex);

                return this.NotFound();
            }
        }

        /// <summary>
        /// The remove favorite topic.
        /// </summary>
        /// <param name="id">
        /// The favorite topic id.
        /// </param>
        /// <returns>
        /// The remove favorite topic JS.
        /// </returns>
        [HttpPost]
        [DnnAuthorize]
        public int RemoveFavoriteTopic(int id)
        {
            return this.Get<IFavoriteTopic>().RemoveFavoriteTopic(id);
        }
    }
}
