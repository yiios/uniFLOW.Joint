using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Joint.Govern.Models
{
    public class ModuleInstance
    {
        public int ModuleInstanceId { get; set; }
        public string Name { get; set; }
        public string ModuleVersion { get; set; }
        public string ModuleName { get; set; }
        public bool Connected { get; set; }
        public string Endpoint { get; set; }
        public bool Licensed { get; set; }
        public DateTime LastAccessTime { get; set; }
        public bool ConfigChanged { get; set; }
    }

    public class ModuleConfiguration
    {
        public int Id { get; set; }
        public int ModuleInstanceId { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
    }

    public class ApplicationContext : DbContext
    {
        public DbSet<ModuleInstance> ModuleInstances { get; set; }
        public DbSet<ModuleConfiguration> ModuleConfigurations { get; set; }

        public ApplicationContext(DbContextOptions<ApplicationContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ModuleInstance>()
                .Property(nameof(ModuleConfiguration.Key))
                .IsRequired();
            modelBuilder.Entity<ModuleInstance>()
                .Property(nameof(ModuleConfiguration.ModuleInstanceId))
                .IsRequired();
            modelBuilder.Entity<ModuleInstance>()
                .HasIndex(nameof(ModuleConfiguration.ModuleInstanceId), nameof(ModuleConfiguration.Key))
                .IsUnique();

            base.OnModelCreating(modelBuilder);
        }
    }
}
