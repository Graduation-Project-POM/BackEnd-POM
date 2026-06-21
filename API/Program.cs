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