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

    public class Admin
    {
        public int AdminId { get; set; }
        public string Login { get; set; }
        public string PasswordHash { get; set; }
    }

    public class ApplicationContext : DbContext
    {
        public DbSet<Admin> Admins { get; set; }

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

            modelBuilder.Entity<Admin>().HasData(
                new Admin { AdminId = 1, Login = "admin", PasswordHash = "F4E1B9EB0780D62BDB3B6193829F1721" }
                );

            base.OnModelCreating(modelBuilder);
        }
    }
}
