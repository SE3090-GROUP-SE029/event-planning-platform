using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Data;

public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var environmentConfiguration = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();
        var environmentName = environmentConfiguration["ASPNETCORE_ENVIRONMENT"]
            ?? environmentConfiguration["DOTNET_ENVIRONMENT"]
            ?? "Development";
        var configuration = new ConfigurationBuilder()
            .SetBasePath(FindApiSettingsDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{environmentName}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection is missing from the API appsettings configuration.");
        }

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName))
            .Options;

        return new AppDbContext(options);
    }

    private static string FindApiSettingsDirectory()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory is not null)
        {
            var candidates = new[]
            {
                directory.FullName,
                Path.Combine(directory.FullName, "src", "Api"),
                Path.Combine(directory.FullName, "Api"),
                Path.Combine(directory.FullName, "backend", "src", "Api")
            };

            var apiDirectory = candidates.FirstOrDefault(path =>
                File.Exists(Path.Combine(path, "appsettings.json")));
            if (apiDirectory is not null)
            {
                return apiDirectory;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            "Could not locate backend/src/Api/appsettings.json. Run EF Core commands from the backend or repository directory.");
    }
}
