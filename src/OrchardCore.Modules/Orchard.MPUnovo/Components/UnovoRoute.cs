using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Orchard.MPUnovo.Components
{
    public class UnovoRoute : Route
    {
        public UnovoRoute(
           IRouter target,
           string routeTemplate,
           IInlineConstraintResolver inlineConstraintResolver)
            : this(
                target,
                routeTemplate,
                defaults: null,
                constraints: null,
                dataTokens: null,
                inlineConstraintResolver: inlineConstraintResolver)
        {
        }

        public UnovoRoute(
            IRouter target,
            string routeTemplate,
            RouteValueDictionary defaults,
            IDictionary<string, object> constraints,
            RouteValueDictionary dataTokens,
            IInlineConstraintResolver inlineConstraintResolver)
            : this(target, null, routeTemplate, defaults, constraints, dataTokens, inlineConstraintResolver)
        {
        }

        public UnovoRoute(
            IRouter target,
            string routeName,
            string routeTemplate,
            RouteValueDictionary defaults,
            IDictionary<string, object> constraints,
            RouteValueDictionary dataTokens,
            IInlineConstraintResolver inlineConstraintResolver)
            : base(target,
                   routeName,
                  routeTemplate,
                  defaults,
                  constraints,
                  dataTokens,
                  inlineConstraintResolver)
        {
        }
        /// <summary>
        /// 用于生成URL
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public override VirtualPathData GetVirtualPath(VirtualPathContext context)
        {
            return base.GetVirtualPath(context);
        }
        protected override VirtualPathData OnVirtualPathGenerated(VirtualPathContext context)
        {
            return base.OnVirtualPathGenerated(context);
        }
        public override Task RouteAsync(RouteContext context)
        {
            return base.RouteAsync(context);
        }
        /// <summary>
        /// 由RouteBase中的RouteAsync中调用
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        protected override Task OnRouteMatched(RouteContext context)
        {
            context.RouteData.Values["action"] = "Mobile" + context.RouteData.Values["action"];
            //在Route的OnRouteMatched方法中会调用target的RouteAsync，target(MvcRouteHandler)的RouteAsync不会调用OnRouteMatched,会给context.Handler赋予委托
            return base.OnRouteMatched(context);
        }
    }
}
