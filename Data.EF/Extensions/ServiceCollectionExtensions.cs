using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Data.EF.Abstractions;
using Data.EF.Wrappers;
using Data.EF.Entities;
using Data.Base.Models;
using Data.Base.Abstractions;

namespace Data.EF;

[ExcludeFromCodeCoverage]
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEntityFrameworkDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<DatabaseOptions>(configuration);
        var postgresConnection = configuration.GetConnectionString(nameof(DatastoreConnectionOptions.DatastoreEndpoint));
        var datastoreType = !string.IsNullOrWhiteSpace(postgresConnection) ? DatastoreType.PostgreSql : DatastoreType.Sqlite;
        services.AddEntityFrameworkDatabase(datastoreType, postgresConnection);
        //services.AddDatabaseDeveloperPageExceptionFilter();
        return services;
    }

    internal static IServiceCollection AddEntityFrameworkDatabase(this IServiceCollection services, DatastoreType datastoreType, string? postgresConnection)
    {
        switch (datastoreType)
        {
            case DatastoreType.PostgreSql:
                services.AddDbContext<DbContext, RepositoryDbContext>(builder =>
                    builder.UseNpgsql(postgresConnection));
                break;
            case DatastoreType.Sqlite:
                services.AddDbContext<DbContext, RepositoryDbContext>(builder =>
                    builder.UseSqlite(DatastoreConnectionOptions.LocalSqLiteEndpoint));
                break;
            case DatastoreType.InMemory:
            default:
                services.AddDbContext<DbContext, RepositoryDbContext>(builder =>
                    builder.UseInMemoryDatabase(DatastoreConnectionOptions.LocalDatabaseName));
                break;
        }
        services.AddHealthChecks().AddDbContextCheck<RepositoryDbContext>();
        services.AddScoped<IRepositoryWrapper<EntityItem>, RepositoryWrapper<EntityItem>>();
        services.AddSingleton<IDatastoreRepository<DatastoreItem>, EfRepositoryService>();
        return services;
    }
}
