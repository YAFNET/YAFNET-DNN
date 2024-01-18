/* Yet Another Forum.NET
 * Copyright (C) 2003-2005 Bjørnar Henden
 * Copyright (C) 2006-2013 Jaben Cargman
 * Copyright (C) 2014-2024 Ingo Herbote
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

namespace YAF.DotNetNuke.Components.WebAPI;

using System.Web.Http.WebHost;
using System.Web.Routing;
using System.Web.SessionState;

/// <summary>
/// The route mapper.
/// </summary>
public class RouteMapper : IServiceRouteMapper
{
    /// <summary>
    /// The register routes.
    /// </summary>
    /// <param name="mapRouteManager">
    /// The map route manager.
    /// </param>
    public void RegisterRoutes(IMapRoute mapRouteManager)
    {
        var routeFavorite = mapRouteManager.MapHttpRoute(
            "YetAnotherForumDotNet",
            "favorite",
            "{controller}/{action}/{id}",
            new { action = RouteParameter.Optional, id = RouteParameter.Optional },
            ["YAF.DotNetNuke.Components.WebAPI"]);

        routeFavorite.ForEach(r => r.RouteHandler = new SessionBasedControllerRouteHandler());

        var route = mapRouteManager.MapHttpRoute(
            "YetAnotherForumDotNet",
            "default",
            "{controller}/{action}",
            ["YAF.DotNetNuke.Components.WebAPI"]);

        route.ForEach(r => r.RouteHandler = new SessionBasedControllerRouteHandler());
    }

    /// <summary>
    /// The session based controller handler.
    /// </summary>
    public class SessionBasedControllerHandler : HttpControllerHandler, IRequiresSessionState
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SessionBasedControllerHandler"/> class.
        /// </summary>
        /// <param name="routeData">
        /// The route data.
        /// </param>
        public SessionBasedControllerHandler(RouteData routeData) : base(routeData)
        {
        }
    }

    /// <summary>
    /// The session based controller route handler.
    /// </summary>
    public class SessionBasedControllerRouteHandler : HttpControllerRouteHandler
    {
        /// <summary>
        /// The get http handler.
        /// </summary>
        /// <param name="requestContext">
        /// The request context.
        /// </param>
        /// <returns>
        /// The <see cref="IHttpHandler"/>.
        /// </returns>
        protected override IHttpHandler GetHttpHandler(RequestContext requestContext)
        {
            return new SessionBasedControllerHandler(requestContext.RouteData);
        }
    }
}