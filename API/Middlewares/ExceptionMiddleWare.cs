using System.Security.Claims;
using MIO.API.Settings;

namespace MIO.API.Middlewares
{
    public class ExceptionMiddleWare
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleWare> _logger;

        public ExceptionMiddleWare(RequestDelegate next,ILogger<ExceptionMiddleWare> logger) 
        {
            _next = next;
            _logger = logger;
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
                _logger.LogError(ex, "Unhandled exception");
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new
                {
                    status = 500,
                    code = "SERVER_ERROR",
                    message = "An unexpected error occurred."
                });
            }

        }


        private async Task HandleException(HttpContext context, Exception ex)
        {
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

            _logger.LogError(ex,
                "Error occurred. UserId: {UserId}, Path: {Path}",
                userId,
                context.Request.Path
            );

            context.Response.ContentType = "application/json";

            var (statusCode, message) = ex switch
            {
                AppException appEx => (appEx.StatusCode, ex.Message),
                _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.") 
            };

            context.Response.StatusCode = statusCode;

            var response = new
            {
                status = statusCode,
                message
            };

            await context.Response.WriteAsJsonAsync(response);
        }




    }
}
