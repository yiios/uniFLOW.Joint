using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Joint.Core.Govern;
using Joint.Govern.Data;
using Joint.Govern.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Joint.Govern.Areas.Api.Controllers
{
    [Area("Api")]
    [Route("api/[controller]")]
    [ApiController]
    public class CatalogController : ControllerBase
    {
        readonly IConfiguration config;
        readonly ApplicationContext context;
        readonly IModuleCatalogManager moduleCatalogManager;
        readonly ILogger<CatalogController> logger;
        public CatalogController(
            IModuleCatalogManager moduleCatalogManager,
            IConfiguration configuration,
            ApplicationContext context,
            ILogger<CatalogController> logger
            )
        {
            this.moduleCatalogManager = moduleCatalogManager;
            this.logger = logger;
            this.config = configuration;
            this.context = context;
        }

        // GET api/values
        [HttpGet]
        public async Task<ActionResult<IEnumerable<string>>> GetAvailableModules()
        {
            await moduleCatalogManager.LoadModuleCatalogCache();
            return moduleCatalogManager.Modules.Select(m => m.ToString()).ToList();
        }

        // GET api/values
        [HttpGet("{ns}")]
        public async Task<ActionResult<ConfigCatalog>> GetCatalog(string ns)
        {
            if (!ModuleIdentifier.TryParse(ns, out var module))
            {
                logger.LogDebug("invalid module name");
                return BadRequest();
            }
            await moduleCatalogManager.LoadModuleCatalogCache();

            var catalog = moduleCatalogManager.GetCatalog(module);
            if (catalog == null)
                return NotFound();
            return catalog;
        }

        // GET api/values
        [HttpGet("compatible/{ns}")]
        public async Task<ActionResult<ConfigCatalog>> GetCompatibleCatalog(string ns)
        {
            if (!ModuleIdentifier.TryParse(ns, out var module))
            {
                logger.LogDebug("invalid module name");
                return BadRequest();
            }
            await moduleCatalogManager.LoadModuleCatalogCache();

            var catalog = moduleCatalogManager.GetCompatibleCatalog(module);
            if (catalog == null)
                return NotFound();
            return catalog;
        }
    }
}