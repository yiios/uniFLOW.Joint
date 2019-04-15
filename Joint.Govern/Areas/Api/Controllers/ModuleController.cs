using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Joint.Core;
using Microsoft.Extensions.Configuration;
using Joint.Govern.Data;
using Joint.Core.Api;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Joint.Govern.Services;
using Joint.Core.Govern;

/// <summary>
/// list, get, create, delete, announce, pick
/// </summary>
namespace Joint.Govern.Areas.Api.Controllers
{
    [Area("Api")]
    [Route("api/[controller]")]
    [ApiController]
    public class ModuleController : ControllerBase
    {
        readonly IConfiguration config;
        readonly ApplicationContext context;
        readonly IModuleCatalogManager moduleCatalogManager;
        readonly ILogger<ModuleController> logger;
        public ModuleController(
            ILogger<ModuleController> logger,
            IConfiguration configuration,
            IModuleCatalogManager moduleCatalogManager,
            ApplicationContext context)
        {
            this.logger = logger;
            this.config = configuration;
            this.context = context;
            this.moduleCatalogManager = moduleCatalogManager;
            //ChangeToken.OnChange(
            //    () => config.GetSection("test").GetReloadToken(),
            //    () => logger.LogInformation("test changed"));
        }

        // GET api/values
        [HttpGet]
        public ActionResult<IDictionary<int, string>> GetModuleList()
        {
            return context.ModuleInstances.ToDictionary(
                m => m.ModuleInstanceId,
                m => m.Module
                );
        }

        [HttpPut("{ns}/{name?}")]
        public Task<ActionResult> Create(string ns, string name) => CreateGet(ns, name);
        [HttpGet("create")]
        public async Task<ActionResult> CreateGet(string ns, string name)
        {
            if (string.IsNullOrEmpty(name)) name = "(default)";
            if (!ModuleIdentifier.TryParse(ns, out var module))
            {
                logger.LogDebug("invalid module");
                return BadRequest();
            }
            await moduleCatalogManager.LoadModuleCatalogCache();

            if (!moduleCatalogManager.HasCompatibleCatalog(module))
                return NotFound("No such module catalog.");

            var exist = await context.ModuleInstances
                .AnyAsync(m => m.Module == ns.ToLower() && m.Name == name.ToLower());
            if (exist)
                return Conflict($"Already exists: {ns}/{name}");

            logger.LogDebug($"Create for {ns}:{name}");
            await context.ModuleInstances.AddAsync(new ModuleInstance
            {
                Module = ns.ToLower(),
                Name = name.ToLower(),
            });
            await context.SaveChangesAsync();
            return Ok();
        }

        [HttpDelete("{ns}/{name?}")]
        public Task<ActionResult> Delete(string ns, string name) => DeleteGet(ns, name);
        [HttpGet("delete")]
        public async Task<ActionResult> DeleteGet(string ns, string name)
        {
            if (string.IsNullOrEmpty(name)) name = "(default)";
            if (!ModuleIdentifier.TryParse(ns, out var module))
            {
                logger.LogDebug("invalid module");
                return BadRequest();
            }

            logger.LogDebug($"Delete {ns}:{name}");
            var found = await context.ModuleInstances.FirstOrDefaultAsync(
                m => m.Module == ns.ToLower() && m.Name == name.ToLower()
            );
            if (found == null)
                return NotFound($"No such module: {ns}/{name}");
            context.Remove(found);
            await context.SaveChangesAsync();
            return Ok();
        }

        [HttpGet("{id:int}")]
        public ActionResult<ModuleInstance> GetById(int id)
        {
            var instance = context.ModuleInstances.Find(id);
            if (instance == null)
                return NotFound($"No such module: [{id}]");
            return instance;
        }

        [HttpGet("{ns}/{name?}")]
        public async Task<ActionResult<ModuleInstance>> GetByNameAsync(string ns, string name)
        {
            if (string.IsNullOrEmpty(name)) name = "(default)";
            if (!ModuleIdentifier.TryParse(ns, out var module))
            {
                logger.LogDebug("invalid module");
                return BadRequest();
            }

            logger.LogDebug($"Delete {ns}:{name}");
            var found = await context.ModuleInstances.FirstOrDefaultAsync(
                m => m.Module == ns.ToLower() && m.Name == name.ToLower()
            );
            if (found == null)
                return NotFound($"No such module: {ns}/{name}");
            return found;
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public ActionResult<string> Get(int id)
        {
            return "value";
        }

        // POST api/values
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
