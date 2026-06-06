using DotNetEnv.Configuration;
using Microsoft.Extensions.Options;
using Scalar.AspNetCore;
using Serilog;
using SmartBin.Api.Extensions;
using SmartBin.Api.Mqtt;
using SmartBin.Api.Services;
using SmartBin.Application.GenericRepository;
using SmartBin.Application.Services;
using SmartBin.Domain.Models;
using SmartBin.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.CaptureStartupErrors(true);
builder.WebHost.UseSetting("detailedErrors", "true");

builder.Services.AddOpenApi();

builder.Configuration
    .AddEnvironmentVariables()
    .AddDotNetEnv();

builder.Host.UseSerilog((context, configuration) =>
                            configuration.ReadFrom.Configuration(context.Configuration));
builder.Services.AddScoped<MqttClientService>();
builder.Services.AddSingleton<MongoDbService>();
builder.Services.AddScoped(typeof(IRepository<>), typeof(MongoRepository<>));
builder.Services.Configure<MongoSettings>(
    builder.Configuration.GetSection("MongoSettings"));

builder.Services.AddSingleton<IMongoSettings>(provider =>
    provider.GetRequiredService<IOptions<MongoSettings>>().Value);
builder.Services.AddControllers();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IBinService, BinService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
builder.Services.AddScoped<IAlertService, AlertService>();


builder.Services.AddAuthorization(options =>
{
   
    options.AddPolicy("MinimumRole_Admin", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireAssertion(context => context.User.ValidateToken(AdminRole.Instance));
    });

    options.AddPolicy("MinimumRole_SalesManager", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireAssertion(context => context.User.ValidateToken(SalesManagerRole.Instance));
    });

    options.AddPolicy("MinimumRole_Guest", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireAssertion(context => context.User.ValidateToken(GuestRole.Instance));
    });
});


var app = builder.Build();
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference("/docs", options =>
    {
        options
            .WithTitle("SmartBin API")
            .WithTheme(ScalarTheme.DeepSpace)
            .ShowOperationId()
            .WithDefaultHttpClient(ScalarTarget.Node, ScalarClient.Undici)
            .AddPreferredSecuritySchemes("BearerAuth")
            .AddHttpAuthentication("BearerAuth", auth =>
            {
                auth.Token = "Your jwt token";
            });
    });
}
app.MapControllers();
app.MapGet("/hello", () => "Hello").Stable();
app.MapGet("/health", () => Results.Ok()).Stable();

app.Run();
