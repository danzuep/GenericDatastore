using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Data.EF.Abstractions;
using Data.EF.Wrappers;
using Data.EF.Entities;
using Data.Base.Models;

namespace Data.EF;

[ExcludeFromCodeCoverage]
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEntityFrameworkDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<DatabaseOptions>(configuration);
        var postgresConnection = configuration.GetConnectionString(nameof(DatastoreConnectionOptions.DatastoreEndpoint));
        var datastoreType = !string.IsNullOrWhiteSpace(postgresConnection) ? DatastoreType.PostgreSql : DatastoreType.Sqlite;
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
        services.AddSingleton<IRepositoryWrapper<EntityItem>, RepositoryWrapper<EntityItem>>();
        //services.AddSingleton<IDatastoreRepository<DatastoreItem, DatastoreItem>>((sp) =>
        //    new ItemDbService(sp.GetRequiredService<IRepositoryWrapper<EntityItem>>()));
        //services.AddDatabaseDeveloperPageExceptionFilter();
        return services;
    }
}
