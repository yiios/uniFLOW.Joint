using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Joint.Govern.Utilities
{
    public class PermissionRequiredAttribute : ActionFilterAttribute
    {

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var isDefined = false;
            var controllerActionDescriptor = filterContext.ActionDescriptor as ControllerActionDescriptor;
            if (controllerActionDescriptor != null)
            {
                isDefined = controllerActionDescriptor.MethodInfo.GetCustomAttributes(inherit: true)
                   .Any(a => a.GetType().Equals(typeof(NoPermissionRequiredAttribute)));
            }
            if (isDefined) return;

            if (!filterContext.HttpContext.Session.HasAdminLogin())
            {
                filterContext.Result = new RedirectResult("/web/account/login");
            }
            base.OnActionExecuting(filterContext);

        }

    }

    //不需要登入

    public class NoPermissionRequiredAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            base.OnActionExecuting(filterContext);
        }

    }
}
