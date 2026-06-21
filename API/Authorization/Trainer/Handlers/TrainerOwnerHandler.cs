namespace MIO2.API.Authorization.Trainer;

using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using MIO.Infrastructure;
using System.Security.Claims;

public class TrainerOwnerHandler
    : AuthorizationHandler<TrainerOwnerRequirement, int>
{
    private readonly AppDbContext _dbContext;

    public TrainerOwnerHandler(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        TrainerOwnerRequirement requirement,
        int trainerId)
    {
        // Admin override
        if (context.User.IsInRole("Admin"))
        {
            context.Succeed(requirement);
            return;
        }

        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userId, out int currentUserId))
            return;

        var isOwner = await _dbContext.Trainers
            .AnyAsync(t => t.Id == trainerId && t.UserId == currentUserId);

        if (isOwner)
            context.Succeed(requirement);
    }
}
