using Microsoft.Extensions.Options;
using OpenKoqis.Api.Services;
using OpenKoqis.Application.GenericRepository;

namespace OpenKoqis.Api.Extensions;

public static class DbExtension
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
