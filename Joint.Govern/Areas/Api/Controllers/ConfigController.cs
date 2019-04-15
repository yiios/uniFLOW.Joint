using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Joint.Govern.Data;
using Joint.Govern.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

/// <summary>
/// get, put/set, delete/reset
/// </summary>
namespace Joint.Govern.Areas.Api.Controllers
{
    [Area("Api")]
    [Route("api/[controller]")]
    [ApiController]
    public class ConfigController : ControllerBase
    {
        readonly IConfiguration config;
        readonly ApplicationContext context;
        readonly IModuleCatalogManager moduleCatalogManager;
        readonly ILogger<ConfigController> logger;
        public ConfigController(
            ILogger<ConfigController> logger,
            IConfiguration configuration,
            IModuleCatalogManager moduleCatalogManager,
            ApplicationContext context)
        {
            this.logger = logger;
            this.config = configuration;
            this.context = context;
            this.moduleCatalogManager = moduleCatalogManager;
        }

        class Opt { public PropertyAccessMode x { get; set; } public int y { get; set; } public override string ToString() => $"x = {x}, y = {y}"; }
        [HttpGet("config")]
        public ActionResult<string> Config()
        {
            var opt = new Opt { x = PropertyAccessMode.Field, y = 6 };
            config.GetSection("test").Bind(opt);

            return opt.ToString();
        }

        [HttpGet("{id:int}")]
        public ActionResult GetById(int id)
        {
            var instance = context.ModuleInstances.Find(id);
            if (instance == null)
                return NotFound();

            var configuration = context.ModuleConfigurations
                .Where(cfg => cfg.ModuleInstanceId == id)
                .ToDictionaryAsync(cfg => cfg.Key, cfg => cfg.Value);
            return new JsonResult(configuration);
        }

        [HttpGet("{ns}/{name}")]
        public ActionResult<IEnumerable<string>> GetByName(string ns, string name)
        {
            return new string[] { ns, name };
        }

    }
}