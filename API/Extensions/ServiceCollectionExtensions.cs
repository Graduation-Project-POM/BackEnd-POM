using Microsoft.AspNetCore.Authorization;
using MIO.API.Settings;
using MIO.Core.Interfaces;
using MIO.Infrastructure.Services;
using MIO2.API.Authorization;
using MIO2.API.Authorization.Trainer;
using MIO2.API.Authorization.Contest;
using MIO2.API.Settings;
using MIO2.Core.Interfaces;
using MIO2.Infrastructure.Events;
using MIO2.Infrastructure.Services;

namespace MIO.API.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services, IConfiguration configuration)
    {
        
        
        var jwtOptions    = configuration.GetSection("Jwt").Get<JwtOptions>()!;
        var emailSettings = configuration.GetSection("Email").Get<EmailSettings>()!;
       

        services.AddSingleton(jwtOptions);
        services.AddSingleton(emailSettings);
        services.Configure<S3Settings>(configuration.GetSection("AWS"));
        
        services.AddHttpContextAccessor();

        services.AddScoped<IUserService, UserServices>();
        services.AddScoped<ITrainerService, TrainerServices>();
        services.AddScoped<IStudentService, StudentServices>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IProblemServices, ProblemService>();
        services.AddScoped<IProblemDiscussionService, ProblemDiscussionService>();
        services.AddScoped<IContestService, ContestService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IGroupService, GroupService>();
        
        services.AddScoped<IAuthorizationHandler, StudentOwnerHandler>();
        services.AddScoped<IAuthorizationHandler, TrainerOwnerHandler>();
        services.AddScoped<IAuthorizationHandler, GroupMemberHandler>();
        services.AddScoped<IAuthorizationHandler, ContestOwnerHandler>();
        
        
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IS3Service, S3Service>();
        services.AddScoped<StudentAcceptedEmailHandler>();
        return services;
    }
}