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
using Microsoft.OpenApi.Models;


var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // ===============================
    // 1) Define the JWT Bearer security scheme
    // ===============================
    //
    // This tells Swagger that our API uses JWT Bearer authentication
    // through the HTTP Authorization header.
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        // The name of the HTTP header where the token will be sent.
        Name = "Authorization",


        // Indicates this is an HTTP authentication scheme.
        Type = SecuritySchemeType.Http,


        // Specifies the authentication scheme name.
        // Must be exactly "Bearer" for JWT Bearer tokens.
        Scheme = "Bearer",


        // Optional metadata to describe the token format.
        BearerFormat = "JWT",


        // Specifies that the token is sent in the request header.
        In = ParameterLocation.Header,


        // Text shown in Swagger UI to guide the user.
        Description = "Enter: Bearer {your JWT token}"
    });


    // ===============================
    // 2) Require the Bearer scheme for secured endpoints
    // ===============================
    //
    // This tells Swagger that endpoints protected by [Authorize]
    // require the Bearer token defined above.
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                // Reference the previously defined "Bearer" security scheme.
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },


            // No scopes are required for JWT Bearer authentication.
            // This array is empty because JWT does not use OAuth scopes here.
            new string[] {}
        }
    });
});


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
builder.Services.AddScoped<IProblemServices, ProblemService>();
builder.Services.AddScoped<IContestService,ContestService>();
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




//CORS Policy
// builder.Services.AddCors(options =>
// {
//     options.AddPolicy("MIOCorsPolicy", policy =>
//     {
//         policy
//             .WithOrigins(
//                 "https://localhost:7205",
//                 "http://localhost:5084"
//             )
//             .AllowAnyHeader()
//             .AllowAnyMethod();
//     });
// });


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


app.UseMiddleware<ExceptionMiddleWare>();

app.UseHttpsRedirection();
//app.UseCors("MIOCorsPolicy");

app.UseAuthentication();
app.UseAuthorization();



app.MapControllers();



app.Run();
