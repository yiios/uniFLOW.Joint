using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Joint.Govern.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Joint.Govern.Areas.Admin.Controllers
{
    [Area("web")]
    public class AccountController : Controller
    {
        readonly ILogger<AccountController> _logger;
        readonly ApplicationContext _ctx;

        public AccountController(
            ApplicationContext ctx, IServiceProvider serviceProvider,
            ILogger<AccountController> logger
            )
        {
            _ctx = ctx;
            _logger = logger;
        }

        public ActionResult Login()
        {
            return View();
        }
    }
}