using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace AmsaAPI.Data;

public class AmsaDbContextFactory : IDesignTimeDbContextFactory<AmsaDbContext>
{
    public AmsaDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddUserSecrets<Program>()
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<AmsaDbContext>();
        optionsBuilder.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));

        return new AmsaDbContext(optionsBuilder.Options);
    }
}
