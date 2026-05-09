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

builder.Configuration
    .AddEnvironmentVariables()
    .AddDotNetEnv();

builder.Host.UseSerilog((context, configuration) =>
                            configuration.ReadFrom.Configuration(context.Configuration));

builder.Services.AddDbContext(builder.Configuration);
builder.Services.AddAuthorizationSecPolicies(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services
    .AddScoped<MqttClientService>()
    .AddScoped<IUserService, UserService>()
    .AddScoped<IBinService, BinService>()
    .AddScoped<IJwtService, JwtService>()
    .AddScoped<IPasswordHasher, BCryptPasswordHasher>()
    .AddScoped<IAlertService, AlertService>();


var app = builder.Build();

app.UseSerilogRequestLogging();

app.AddScalar();
app.MapControllers();
app.MapGet("/hello", () => "Hello").Stable();
app.MapGet("/health", () => Results.Ok()).Stable();
app.Run();
