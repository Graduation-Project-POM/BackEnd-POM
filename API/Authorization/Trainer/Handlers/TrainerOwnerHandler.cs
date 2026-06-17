namespace MIO2.API.Authorization.Trainer;

using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;


public class TrainerOwnerHandler
    : AuthorizationHandler<TrainerOwnerRequirement, int>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        TrainerOwnerRequirement requirement,
        int trainerId)
    {
        // Admin override
        if (context.User.IsInRole("Admin"))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // Ownership check
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (int.TryParse(userId, out int authenticatedStudentId) &&
            authenticatedStudentId == trainerId)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
