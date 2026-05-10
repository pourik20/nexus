using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Nexus.Api.Domain;

namespace Nexus.Api.Infrastructure.Mongo;

public static class MongoRegistration
{
    public static IServiceCollection AddMongo(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<MongoSettings>(config.GetSection("Mongo"));

        services.AddSingleton<IMongoClient>(sp =>
        {
            var s = sp.GetRequiredService<IOptions<MongoSettings>>().Value;
            return new MongoClient(s.ConnectionString);
        });

        services.AddSingleton(sp =>
        {
            var s = sp.GetRequiredService<IOptions<MongoSettings>>().Value;
            return sp.GetRequiredService<IMongoClient>().GetDatabase(s.Database);
        });

        services.AddSingleton<IMongoCollection<Dataset>>(sp =>
            sp.GetRequiredService<IMongoDatabase>().GetCollection<Dataset>("datasets"));

        services.AddSingleton<IMongoCollection<Pipeline>>(sp =>
            sp.GetRequiredService<IMongoDatabase>().GetCollection<Pipeline>("pipelines"));

        services.AddSingleton<IMongoCollection<JobRun>>(sp =>
            sp.GetRequiredService<IMongoDatabase>().GetCollection<JobRun>("jobRuns"));

        services.AddSingleton<IMongoCollection<JobRunStep>>(sp =>
            sp.GetRequiredService<IMongoDatabase>().GetCollection<JobRunStep>("jobRunSteps"));

        return services;
    }
}
