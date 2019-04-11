using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UniFlowGW.Exceptions;
using UniFlowGW.ViewModels;

namespace UniFlowGW.Controllers
{
    public class ErrorController : Controller
    {

        [Route("/Error/500")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error500()
        {
            var exceptionFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();

            var errorModel = new ErrorViewModel
            {
                //RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                Message = "系统异常，请稍后再试！"
            };
            if (exceptionFeature != null && typeof(CustomException).IsInstanceOfType(exceptionFeature.Error))
            {
                //ViewBag.ErrorMessage = exceptionFeature.Error.Message;
                //ViewBag.RouteOfException = exceptionFeature.Path;

                errorModel.Message = exceptionFeature.Error.Message;
            }


            return View(errorModel);
        }


        [Route("/Error/404")]
        public IActionResult Error404()
        {
            return View();
        }
    }



}
