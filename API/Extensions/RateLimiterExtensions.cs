using System.Threading.RateLimiting;

public static class RateLimiterExtensions
{
    public static IServiceCollection AddApiRateLimiter(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy("AuthLimiter", httpContext =>
            {
                var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: ip,
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    });
            });
        });

        return services;
    }

    public static IApplicationBuilder UseRateLimiterWithMessage(this IApplicationBuilder app)
    {
        app.UseRateLimiter();

        app.Use(async (context, next) =>
        {
            await next();

            if (context.Response.StatusCode == StatusCodes.Status429TooManyRequests)
                await context.Response.WriteAsync("Too many login attempts. Please try again later.");
        });

        return app;
    }
}