namespace YAF.DotNetNuke.Components.WebAPI
{
    using System.Web;
    using System.Web.Http;
    using System.Web.Http.WebHost;
    using System.Web.Routing;
    using System.Web.SessionState;

    using global::DotNetNuke.Web.Api;

    using YAF.Types.Extensions;

    /// <summary>
    /// The route mapper.
    /// </summary>
    public class RouteMapper : IServiceRouteMapper
    {
        #region IServiceRouteMapper

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
                new[] { "YAF.DotNetNuke.Components.WebAPI" });

            routeFavorite.ForEach(r => r.RouteHandler = new SessionBasedControllerRouteHandler());

            var route = mapRouteManager.MapHttpRoute(
                "YetAnotherForumDotNet",
                "default",
                "{controller}/{action}",
                new[] { "YAF.DotNetNuke.Components.WebAPI" });

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

        #endregion
    }
}