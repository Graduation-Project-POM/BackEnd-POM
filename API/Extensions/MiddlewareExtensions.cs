using MIO.API.Middlewares;

namespace MIO.API.Extensions;

public static class MiddlewareExtensions
{
    public static WebApplication ConfigureMiddleware(
        this WebApplication app,
        IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseMiddleware<ExceptionMiddleWare>();

        app.UseHttpsRedirection();

        app.UseRateLimiter();

        app.UseAuthentication();

        app.UseAuthorization();

        app.MapControllers();

        return app;
    }
}