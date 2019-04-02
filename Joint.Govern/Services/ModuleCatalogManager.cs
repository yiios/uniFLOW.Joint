using Joint.Core.Govern;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

    }

    public interface IModuleCatalogProvider
    {

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
        }


    }
    public class FileSystemModuleCatalogProvider : IModuleCatalogProvider
    {
        readonly ILogger<FileSystemModuleCatalogProvider> logger;
        public FileSystemModuleCatalogProvider(
            ILogger<FileSystemModuleCatalogProvider> logger
            )
        {
            this.logger = logger;
        }

        //public IList<ConfigCatalog> 
    }

    public static class MCMExtension
    {
        public static IServiceCollection AddModuleCatalogManager
            (this IServiceCollection services)
        {
            services.AddSingleton<IModuleCatalogManager, ModuleCatalogManager>();
            services.AddSingleton<IModuleCatalogProvider>(
                provider =>
                {
                    var opt = provider.GetService<IOptions<ModuleCatalogOptions>>();

                    return new FileSystemModuleCatalogProvider(
                        provider.GetRequiredService<ILogger<FileSystemModuleCatalogProvider>>()
                    );
                });
            return services;
        }
    }
}
