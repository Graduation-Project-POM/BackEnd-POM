using MIO.API.Extensions;
using MIO.API.Middlewares;
using MIO.Infrastructure;
using MIO.Infrastructure.Services;
using MIO2.Core.Events.EventBus;
using MIO2.Infrastructure.Events;

var builder = WebApplication.CreateBuilder(args);


// ======================================
// Controllers
// ======================================

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();


// ======================================
// Custom Extensions
// ======================================

builder.Services.AddDatabase(builder.Configuration);

builder.Services.AddScoped<EmailService>();

builder.Services.AddApplicationServices(builder.Configuration);

builder.Services.AddJwtAuthentication(builder.Configuration);

builder.Services.AddCustomAuthorization();

builder.Services.AddSwaggerDocumentation();

builder.Services.AddApiRateLimiter();




// // ======================================
// // Cors Policy
// // ======================================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000") // replace with your frontend URL
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});


// ======================================
// Build App
// ======================================

var app = builder.Build();


// ======================================
// Swagger
// ======================================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


// ======================================
// Event Subscriptions
// ======================================

using (var scope = app.Services.CreateScope())
{
    var emailHandler = scope.ServiceProvider
        .GetRequiredService<StudentAcceptedEmailHandler>();

    studentEvents.Accepted += emailHandler.Handle;
}





// ======================================
// Middlewares
// ======================================

app.UseMiddleware<ExceptionMiddleWare>();

app.UseHttpsRedirection();

app.UseCors("AllowFrontend");

app.UseRateLimiter();

//app.UseRateLimiterWithMessage();

app.UseAuthentication();

app.UseAuthorization();


// ======================================
// Endpoints
// ======================================

app.MapControllers();


// ======================================
// Run App
// ======================================

app.Run();