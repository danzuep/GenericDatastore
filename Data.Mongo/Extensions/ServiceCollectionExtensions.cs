using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Data.Mongo.Config;
using Data.Mongo.Abstractions;
using Data.Mongo.Wrappers;
using Data.Mongo.Models;

namespace Data.Mongo;

[ExcludeFromCodeCoverage]
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDefaultDatastore(this IServiceCollection services, IConfiguration configuration)
    {
        _ = services.Configure<MongoDbOptions>(configuration);
        _ = services.AddMongoDb();
        return services;
    }

    public static IServiceCollection AddDatastoreWithOptions(this IServiceCollection services, IConfiguration configuration)
    {
        //var connectionString = configuration.GetConnectionString("DefaultConnection") ??
        //    throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        _ = services.AddOptions<MongoDbOptions>().Bind(configuration)
            .Configure<IServiceProvider>((options, provider) => { }).ValidateDataAnnotations()
            .Validate(o => o.UseDatastore, "Misconfigured datastore connection string");
        return services;
    }

    public static IServiceCollection AddMongoDb(this IServiceCollection services)
    {
        services.AddSingleton<IMongoClientWrapper<EntityItem>, WorkItemMongoClientWrapper>();
        services.AddHealthChecks().AddCheck<WorkItemMongoClientWrapper>("MongoDBConnectionCheck");
        //services.AddSingleton<IDatastoreRepository<EntityItem, DatastoreItem>, JobQueueMongoService>();
        return services;
    }
}
