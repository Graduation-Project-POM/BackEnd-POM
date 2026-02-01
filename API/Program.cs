using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MIO.API.Middlewares;
using MIO.API.Settings;
using MIO.Core.Interfaces;
using MIO.Infrastructure;
using MIO.Infrastructure.Services;
using MIO2.API.Settings;
using MIO2.Core.Events.EventBus;
using MIO2.Core.Interfaces;
using MIO2.Infrastructure.Events;
using MIO2.Infrastructure.Services;
using System;
using System.Text;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(connectionString);
});

var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>();
var emailSettings = builder.Configuration.GetSection("Email").Get<EmailSettings>();
var s3Settings= builder.Configuration.GetSection("AWS").Get<S3Settings>();

//builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));

//builder.Services.Configure<S3Settings>(builder.Configuration.GetSection("Email"));

//builder.Services.Configure<S3Settings>(builder.Configuration.GetSection("AWS"));

//services
builder.Services.AddHttpContextAccessor();

builder.Services.AddSingleton(jwtOptions);
builder.Services.AddSingleton(emailSettings);
builder.Services.AddSingleton(s3Settings);
builder.Services.AddScoped<IUserService, UserServices>();
builder.Services.AddScoped<ITrainerService, TrainerServices>();
builder.Services.AddScoped<IStudentService, StudentServices>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<StudentAcceptedEmailHandler>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IS3Service, S3Service>();
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtOptions.signingkey))
        };
    });



var app = builder.Build();


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



    app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.UseMiddleware<ExceptionMiddleWare>();

app.Run();
