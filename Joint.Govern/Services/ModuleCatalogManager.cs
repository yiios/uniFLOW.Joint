using Joint.Core.Govern;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Joint.Govern.Services
{
    public enum ModuleCatalogMode
    {
        FileSystem
    }
    public class ModuleCatalogOptions
    {
        public ModuleCatalogMode Mode { get; set; } = ModuleCatalogMode.FileSystem;
        public string Source { get; set; } = "catalogs";
    }

    public interface IModuleCatalogManager
    {
        ICollection<ConfigCatalog> Catalogs { get; }
        ICollection<ModuleIdentifier> Modules { get; }
        ConfigCatalog GetCompatibleCatalog(ModuleIdentifier module);
        ConfigCatalog GetCatalog(ModuleIdentifier module);
        bool HasCompatibleCatalog(ModuleIdentifier module);
        Task LoadModuleCatalogCache(bool force = false);
    }

    public interface IModuleCatalogProvider
    {
        ModuleCatalogMode Mode { get; }

        IList<ModuleIdentifier> GetAvailableModules();

        ConfigCatalog LoadCatalogForModule(ModuleIdentifier module);
    }

    public class ModuleCatalogManager : IModuleCatalogManager
    {
        readonly ILogger<ModuleCatalogManager> logger;
        readonly IOptionsMonitor<ModuleCatalogOptions> optionsMonitor;
        readonly IServiceProvider serviceProvider;

        ModuleCatalogMode mode;

        public ModuleCatalogManager(
            ILogger<ModuleCatalogManager> logger,
            IOptionsMonitor<ModuleCatalogOptions> optionsMonitor,
            IServiceProvider serviceProvider
            )
        {
            this.logger = logger;
            this.optionsMonitor = optionsMonitor;
            this.serviceProvider = serviceProvider;

            mode = optionsMonitor.CurrentValue.Mode;

            optionsMonitor.OnChange(opt =>
            {
                mode = opt.Mode;
            });

            catalogCache = new SortedList<ModuleIdentifier, ConfigCatalog>();
        }

        IDictionary<ModuleIdentifier, ConfigCatalog> catalogCache;
        public ICollection<ConfigCatalog> Catalogs => catalogCache.Values;
        public ICollection<ModuleIdentifier> Modules => catalogCache.Keys;

        public Task LoadModuleCatalogCache(bool force)
        {
            if (force || catalogCache.Count == 0)
                return Task.Run(() => LoadModules());
            return Task.CompletedTask;
        }
        public bool HasCompatibleCatalog(ModuleIdentifier module)
        {
            if (catalogCache.ContainsKey(module)) return true;
            return catalogCache.Keys.Any(
                m => string.Compare(m.Name, module.Name,
                    StringComparison.OrdinalIgnoreCase) == 0 &&
                    m.Version.Major == module.Version.Major);
        }

        public ConfigCatalog GetCompatibleCatalog(ModuleIdentifier module)
        {
            if (catalogCache.TryGetValue(module, out var catalog))
                return catalog;
            var found = catalogCache.Keys
                .Where(m => string.Compare(m.Name, module.Name,
                    StringComparison.OrdinalIgnoreCase) == 0)
                .OrderByDescending(m => m.Version)
                .FirstOrDefault(m => m.Version.Major == module.Version.Major);
            return catalogCache.TryGetValue(found, out catalog) ? catalog : null;
        }
        public ConfigCatalog GetCatalog(ModuleIdentifier module)
            => catalogCache.TryGetValue(module, out var catalog) ? catalog : null;

        void LoadModules()
        {
            var provider = serviceProvider.GetServices<IModuleCatalogProvider>()
                .FirstOrDefault(p => p.Mode == optionsMonitor.CurrentValue.Mode);
            Debug.Assert(provider != null);
            var modules = provider.GetAvailableModules();
            catalogCache.Clear();
            foreach (var module in modules)
            {
                var catalog = provider.LoadCatalogForModule(module);
                catalogCache.Add(module, catalog);
            }
        }
    }

    public class FileSystemModuleCatalogProvider : IModuleCatalogProvider
    {
        readonly IOptionsMonitor<ModuleCatalogOptions> optionsMonitor;

        public ModuleCatalogMode Mode { get; } = ModuleCatalogMode.FileSystem;

        readonly ILogger<FileSystemModuleCatalogProvider> logger;
        public FileSystemModuleCatalogProvider(
            IOptionsMonitor<ModuleCatalogOptions> optionsMonitor,
            ILogger<FileSystemModuleCatalogProvider> logger
            )
        {
            this.logger = logger;
            this.optionsMonitor = optionsMonitor;
        }

        public IList<ModuleIdentifier> GetAvailableModules()
        {
            var dir = optionsMonitor.CurrentValue.Source;
            if (!Directory.Exists(dir))
            {
                logger.LogError("Config catalog directory not found: " + dir);
                throw new ApplicationException("Config catalog directory not found");
            }

            var list = new List<ModuleIdentifier>();

            foreach (var file in Directory.GetFiles(dir))
            {
                if (Path.GetExtension(file).ToLower() != ".json") continue;
                var name = Path.GetFileNameWithoutExtension(file);
                if (ModuleIdentifier.TryParse(name, out var identifier))
                    list.Add(identifier);
            }
            foreach (var file in Directory.GetDirectories(dir))
            {
                var name = Path.GetFileName(file);
                if (ModuleIdentifier.TryParse(name, out var identifier))
                    list.Add(identifier);
            }
            return list;
        }

        public ConfigCatalog LoadCatalogForModule(ModuleIdentifier module)
        {
            var dir = optionsMonitor.CurrentValue.Source;
            var catalog = new ConfigCatalog() { ModuleInfo = new ModuleInfo() { Identifier = module } };

            var basename = Path.Combine(dir, module.ToString());
            if (File.Exists(basename + ".json"))
            {
                logger.LogTrace($"load catalog file [{basename}.json]");
                string content = File.ReadAllText(basename + ".json");
                catalog = JsonConvert.DeserializeObject<ConfigCatalog>(content);
            }
            if (Directory.Exists(basename))
            {
                logger.LogTrace($"load catalog dir [{basename}]");
                var sections = new List<ConfigCatalog.Section>();
                foreach (var file in Directory.GetFiles(basename))
                {
                    logger.LogTrace($"-> loading {file}");
                    if (Path.GetExtension(file).ToLower() != ".json") break;
                    string content = File.ReadAllText(file);
                    if (Path.GetFileNameWithoutExtension(file) == "moduleinfo")
                    {
                        catalog.ModuleInfo = JsonConvert.DeserializeObject<ModuleInfo>(content);
                    }
                    else
                    {
                        var section = JsonConvert.DeserializeObject<ConfigCatalog.Section>(content);
                        sections.Add(section);
                    }
                }
                catalog.Sections = sections.ToArray();
            }

            return catalog;
        }
    }

    public static class MCMExtension
    {
        public static IServiceCollection AddModuleCatalogManager
            (this IServiceCollection services)
        {
            services.AddSingleton<IModuleCatalogManager, ModuleCatalogManager>();
            services.AddSingleton<IModuleCatalogProvider, FileSystemModuleCatalogProvider>();
            //services.AddSingleton<IModuleCatalogProvider>(
            //    provider =>
            //    {
            //        var opt = provider.GetService<IOptions<ModuleCatalogOptions>>();

            //        return new FileSystemModuleCatalogProvider(
            //            provider.GetRequiredService<ILogger<FileSystemModuleCatalogProvider>>()
            //        );
            //    });
            return services;
        }
    }
}
