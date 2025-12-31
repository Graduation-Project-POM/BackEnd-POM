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
            catch (Exception ex)
            {
                await HandleException(context, ex);
            }

        }


        private async Task HandleException(HttpContext context,Exception ex)
        {

            var userId=context.User.FindFirstValue(ClaimTypes.NameIdentifier);

            _logger.LogError(ex,
                 "Error occurred. UserId: {UserId}, Path: {Path}",
                 userId,
                 context.Request.Path
                );

            context.Response.ContentType = "application/json";


            var statusCode = ex switch
            {
                AppException appEx => appEx.StatusCode,
                _ => StatusCodes.Status500InternalServerError
            };


            context.Response.StatusCode = statusCode;

            var response = new
            {
                status = statusCode,
                message = ex.Message
            };

            await context.Response.WriteAsJsonAsync(response);

        }




    }
}
