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
        var configurationBuilder = new ConfigurationBuilder();
        var settingsDirectory = FindApiSettingsDirectory(environmentName);
        if (settingsDirectory is not null)
        {
            configurationBuilder
                .SetBasePath(settingsDirectory)
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile($"appsettings.{environmentName}.json", optional: true);
        }

        var configuration = configurationBuilder
            .AddEnvironmentVariables()
            .Build();
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection is not configured. Set the ConnectionStrings__DefaultConnection environment variable.");
        }

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName))
            .Options;

        return new AppDbContext(options);
    }

    private static string? FindApiSettingsDirectory(string environmentName)
    {
        var startDirectories = new[]
        {
            Directory.GetCurrentDirectory(),
            AppContext.BaseDirectory
        };

        foreach (var startDirectory in startDirectories)
        {
            var directory = new DirectoryInfo(startDirectory);
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
                    File.Exists(Path.Combine(path, "appsettings.json"))
                    || File.Exists(Path.Combine(path, $"appsettings.{environmentName}.json")));
                if (apiDirectory is not null)
                {
                    return apiDirectory;
                }

                directory = directory.Parent;
            }
        }

        return null;
    }
}
