using Scalar.AspNetCore;

namespace OpenKoqis.Api.Extensions;

public static class ScalarExtensions
{
    public static WebApplication AddScalar(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.MapScalarApiReference("/docs", options =>
            {
                options
                    .WithTitle("OpenKoqis API")
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
        return app;
    }
}