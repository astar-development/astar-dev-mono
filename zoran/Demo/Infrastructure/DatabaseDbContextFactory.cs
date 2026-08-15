using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Demo.Infrastructure;

public class DatabaseDbContextFactory : IDesignTimeDbContextFactory<DatabaseDbContext>
{
    public DatabaseDbContext CreateDbContext(params string[] args)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json")
            .Build();

        string connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection not found in configuration.");

        var optionsBuilder = new DbContextOptionsBuilder<DatabaseDbContext>();
        optionsBuilder.UseSqlServer(connectionString, x => x.MigrationsAssembly("Demo"));
        // optionsBuilder.AddInterceptors(new FreezableEntityInterceptor());

        return new DatabaseDbContext(optionsBuilder.Options);
    }
}
