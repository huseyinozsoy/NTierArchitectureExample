using System.Reflection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using N_Tier.Core.Common;
using N_Tier.Core.Entities;
using N_Tier.DataAccess.Identity;
using N_Tier.Shared.Services;

namespace N_Tier.DataAccess.Persistence;

public class DatabaseContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
{
    private readonly IClaimService _claimService;

    public DatabaseContext(DbContextOptions options, IClaimService claimService) : base(options)
    {
        _claimService = claimService;
    }

    public DbSet<TodoItem> TodoItems { get; set; }

    public DbSet<TodoList> TodoLists { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        base.OnModelCreating(builder);
    }

    public new async Task<int> SaveChangesAsync(CancellationToken cancellationToken = new())
    {
        foreach (var entry in ChangeTracker.Entries<IAuditedEntity>())
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedBy = Guid.Parse(_claimService.GetUserId());
                    entry.Entity.CreatedOn = DateTime.UtcNow;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedBy = Guid.Parse(_claimService.GetUserId());
                    entry.Entity.UpdatedOn = DateTime.UtcNow;
                    break;
            }

        return await base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            IConfigurationRoot configuration = new ConfigurationBuilder()
               .SetBasePath(Directory.GetCurrentDirectory())
               .AddJsonFile("appsettings.json")
               .Build();
            var databaseConfig = configuration.GetSection("Database").Get<DatabaseConfiguration>();
            var connectionString = configuration.GetConnectionString("DbCoreConnectionString");
            optionsBuilder.UseNpgsql(databaseConfig.ConnectionString,
                    opt => opt.MigrationsAssembly(typeof(DatabaseContext).Assembly.FullName));
        }
    }
}
