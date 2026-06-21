using System.Security.Claims;
using MIO.API.Settings;

namespace MIO.API.Middlewares
{
    public class ExceptionMiddleWare
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleWare> _logger;
        private readonly IHostEnvironment _env;

        public ExceptionMiddleWare(RequestDelegate next, ILogger<ExceptionMiddleWare> logger, IHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {

                await _next(context);
            }
            catch (AppException ex)
            {
                context.Response.StatusCode = ex.StatusCode;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new
                {
                    status = ex.StatusCode,
                    code = ex.ErrorCode,
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
                _logger.LogError(ex,
                    "Unhandled exception. UserId: {UserId}, Method: {Method}, Path: {Path}",
                    userId, context.Request.Method, context.Request.Path);

                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new
                {
                    status = 500,
                    code = "SERVER_ERROR",
                    message = _env.IsDevelopment()
                        ? (ex.InnerException?.Message ?? ex.Message)
                        : "An unexpected error occurred.",
                    detail = _env.IsDevelopment() ? ex.ToString() : null
                });
            }

        }
    }
}
