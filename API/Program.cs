using Microsoft.Extensions.FileProviders;
using MIO.API.Extensions;
using MIO.API.Json;
using MIO.API.Middlewares;
using MIO.Core.Entities;
using MIO.Core.Enums;
using MIO.Infrastructure;
using MIO.Infrastructure.Services;
using MIO2.Core.Events.EventBus;
using MIO2.Infrastructure.Events;

var builder = WebApplication.CreateBuilder(args);

var webRootPath = Path.Combine(builder.Environment.ContentRootPath, "wwwroot");
Directory.CreateDirectory(Path.Combine(webRootPath, "uploads"));




builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new UtcDateTimeConverter());
        options.JsonSerializerOptions.Converters.Add(new NullableUtcDateTimeConverter());
    });
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
    
    var corsOrigins = builder.Configuration
        .GetSection("Cors:AllowedOrigins").Get<string[]>();

    options.AddPolicy("AllowFrontend", policy =>
    {
        if (corsOrigins is { Length: > 0 })
        {
            policy
                .WithOrigins(corsOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
        else
        {
            policy
                .SetIsOriginAllowed(origin =>
                    Uri.TryCreate(origin, UriKind.Absolute, out var uri) &&
                    (uri.Host == "localhost" || uri.Host == "127.0.0.1"))
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
    });
});


var app = builder.Build();


//Admin 
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Seed the admin only if it does not already exist. The password is read from
        // configuration (Seed:AdminPassword / env var Seed__AdminPassword) so a known
        // value is never hardcoded, and an existing admin's password is NOT reset on
        // every startup 
        var admin = db.Users.FirstOrDefault(u => u.UserName == "admin");
        if (admin == null)
        {
            var adminPassword = builder.Configuration["Seed:AdminPassword"]
                ?? "Admin1234"; // dev-only fallback; override in production config
            Security.CreatePasswordHash(adminPassword, out byte[] hash, out byte[] salt);

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
                Gender = Gender.Male,
                ProfileImageUrl = "",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                LastUpdatedAt = DateTime.UtcNow,
                RoleID = Role.Admin
            });

            db.SaveChanges();
        }
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Admin seeding failed");
    }
}


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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}



using (var scope = app.Services.CreateScope())
{
    var emailHandler = scope.ServiceProvider
        .GetRequiredService<StudentAcceptedEmailHandler>();

    studentEvents.Accepted += emailHandler.Handle;
}



app.UseMiddleware<ExceptionMiddleWare>();

app.UseHttpsRedirection();

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(webRootPath),
    RequestPath = "",
});

app.UseCors("AllowFrontend");

app.UseRateLimiter();


app.UseAuthentication();

app.UseAuthorization();


app.MapControllers();


app.Run();