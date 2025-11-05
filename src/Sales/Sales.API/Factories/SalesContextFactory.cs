using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Sales.API.Context;

namespace Sales.API.Factories;

public class SalesContextFactory : IDesignTimeDbContextFactory<SalesContext>
{
    public SalesContext CreateDbContext(string[] args)
    {
        // Obtém o diretório atual (onde o comando 'dotnet ef' é executado)
        var basePath = Directory.GetCurrentDirectory();
        
        // Constrói a configuração para ler o appsettings.json
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json")
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<SalesContext>();

        var connectionString = configuration.GetConnectionString("StandardConnection");

        optionsBuilder.UseSqlServer(connectionString);

        return new SalesContext(optionsBuilder.Options);
    }
}

