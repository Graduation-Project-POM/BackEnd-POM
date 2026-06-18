using Microsoft.Extensions.FileProviders;
using MIO.API.Extensions;
using MIO.API.Middlewares;
using MIO.Core.Entities;
using MIO.Core.Enums;
using MIO.Infrastructure;
using MIO.Infrastructure.Services;
using MIO2.Core.Events.EventBus;
using MIO2.Infrastructure.Events;

var builder = WebApplication.CreateBuilder(args);

// Single source of truth for where uploaded files (problem images) live, so the
// save path and the static-serve path can never drift apart.
var webRootPath = Path.Combine(builder.Environment.ContentRootPath, "wwwroot");
Directory.CreateDirectory(Path.Combine(webRootPath, "uploads"));




builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();


builder.Services.AddDatabase(builder.Configuration);

builder.Services.AddScoped<EmailService>();

builder.Services.AddApplicationServices(builder.Configuration);

builder.Services.AddJwtAuthentication(builder.Configuration);

builder.Services.AddCustomAuthorization();

builder.Services.AddSwaggerDocumentation();

builder.Services.AddApiRateLimiter();









builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .SetIsOriginAllowed(origin =>
                Uri.TryCreate(origin, UriKind.Absolute, out var uri) &&
                (uri.Host == "localhost" || uri.Host == "127.0.0.1"))
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});


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

// Backfill Student/Trainer profiles for users that have those roles but no
// entity row yet (so they appear in GET /Student and GET /Trainer).
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var studentUserIds = db.Students.Select(s => s.UserId).ToHashSet();
        var trainerUserIds = db.Trainers.Select(tr => tr.UserId).ToHashSet();

        foreach (var u in db.Users.ToList())
        {
            if (u.RoleID == Role.Student && !studentUserIds.Contains(u.Id))
            {
                db.Students.Add(new Student
                {
                    UserId = u.Id,
                    GradeLevel = "",
                    City = "",
                    SchoolName = "",
                    NationalId = "",
                    IsAccepted = true
                });
            }
            else if (u.RoleID == Role.Trainer && !trainerUserIds.Contains(u.Id))
            {
                db.Trainers.Add(new Trainer { UserId = u.Id, Specialization = "" });
            }
        }

        db.SaveChanges();
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Role profile backfill failed");
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

// Serve uploaded files (problem images, etc.). Explicit provider so it works
// regardless of how the host resolved WebRootPath.
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(webRootPath),
    RequestPath = "",
});

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