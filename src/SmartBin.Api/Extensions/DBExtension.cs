using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SmartBin.Application.GenericRepository;
using SmartBin.Infrastructure.Services;
using SmartBin.Api.Services;
namespace SmartBin.Api.Extensions;

public static class DBExtension
{
    public static IServiceCollection AddDbContext(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<MongoDbService>();
        services.AddScoped(typeof(IRepository<>), typeof(MongoRepository<>));
        services.Configure<MongoSettings>(configuration.GetSection("MongoSettings"));
        services.AddSingleton<IMongoSettings>(provider =>
            provider.GetRequiredService<IOptions<MongoSettings>>().Value);
        return services;
    }
}