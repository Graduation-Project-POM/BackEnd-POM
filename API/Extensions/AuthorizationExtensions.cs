using MIO2.API.Authorization;
using MIO2.API.Authorization.Trainer;
using MIO2.API.Authorization.Contest;
using Microsoft.AspNetCore.Authorization;

namespace MIO.API.Extensions;

public static class AuthorizationExtensions
{
    public static IServiceCollection AddCustomAuthorization(
        this IServiceCollection services)
    {
        // Policies
        services.AddAuthorization(options =>
        {
            options.AddPolicy(
                "StudentOwnerOrAdminOrTrainer",
                policy =>
                    policy.Requirements.Add(
                        new StudentOwnerRequirement()));

            options.AddPolicy(
                "TrainerOwnerOrAdmin",
                policy =>
                    policy.Requirements.Add(
                        new TrainerOwnerRequirement()));
            
            options.AddPolicy(
                "GroupMemberOrAdminOrTrainer",
                policy =>
                    policy.Requirements.Add(
                        new GroupMemberRequirment()));

            options.AddPolicy(
                "ContestOwnerOrAdmin",
                policy =>
                    policy.Requirements.Add(
                        new ContestOwnerRequirement()));

        });

        return services;
    }
}