using MIO.API.Extensions;
using MIO.API.Middlewares;
using MIO.Core.Entities;
using MIO.Core.Enums;
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
        policy
            // Allow any localhost / 127.0.0.1 origin regardless of port,
            // so the Vite dev server works whatever port it lands on.
            .SetIsOriginAllowed(origin =>
                Uri.TryCreate(origin, UriKind.Absolute, out var uri) &&
                (uri.Host == "localhost" || uri.Host == "127.0.0.1"))
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});


// ======================================
// Build App
// ======================================

var app = builder.Build();


//Admin 
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Security.CreatePasswordHash("Admin1234", out byte[] hash, out byte[] salt);

        var admin = db.Users.FirstOrDefault(u => u.UserName == "admin");
        if (admin == null)
        {
            db.Users.Add(new User
            {
                FirstName = "System",
                MiddleName = "",
                LastName = "Admin",
                Email = "admin@pmo.ps",
                UserName = "admin",
                PasswordHash = hash,
                PasswordSalt = salt,
                PhoneNumber = null,
                Gender = "male",
                ProfileImageUrl = "",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                LastUpdatedAt = DateTime.UtcNow,
                RoleID = Role.Admin
            });
        }
        else
        {
            admin.PasswordHash = hash;
            admin.PasswordSalt = salt;
            admin.RoleID = Role.Admin;
            admin.IsActive = true;
        }

        db.SaveChanges();
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Admin seeding failed");
    }
}

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