using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Joint.Govern.Data
{
    public class ModuleInstance
    {
        public int ModuleInstanceId { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        public string Module { get; set; }
        public bool Connected { get; set; }
        public string Endpoint { get; set; }
        public DateTime LastAccessTime { get; set; }
        public DateTime ConfigTime { get; set; }
    }

    public class ModuleConfiguration
    {
        public int Id { get; set; }
        public int ModuleInstanceId { get; set; }
        [Required]
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
            modelBuilder.Entity<ModuleConfiguration>()
                .HasIndex(nameof(ModuleConfiguration.ModuleInstanceId), nameof(ModuleConfiguration.Key))
                .IsUnique();
            modelBuilder.Entity<ModuleInstance>()
                .HasIndex(nameof(ModuleInstance.Name))
                .IsUnique();

            base.OnModelCreating(modelBuilder);
        }
    }
}
