using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using MIO.Infrastructure;
using System.Security.Claims;

namespace MIO2.API.Authorization.Contest;

// Checks whether the current user is either:
// - An Admin (full access), OR
// - The Trainer who created the requested contest (owns it)
public class ContestOwnerHandler
    : AuthorizationHandler<ContestOwnerRequirement>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly AppDbContext _dbContext;

    public ContestOwnerHandler(
        IHttpContextAccessor httpContextAccessor,
        AppDbContext dbContext)
    {
        _httpContextAccessor = httpContextAccessor;
        _dbContext = dbContext;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ContestOwnerRequirement requirement)
    {
        // Admin override
        if (context.User.IsInRole("Admin"))
        {
            context.Succeed(requirement);
            return;
        }

        var httpContext = _httpContextAccessor.HttpContext!;

        // get the {contestId} from the route
        var routeId = httpContext.Request.RouteValues["contestId"]?.ToString();
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (routeId == null || userId == null) return;

        if (!int.TryParse(routeId, out int contestId) ||
            !int.TryParse(userId, out int currentUserId)) return;

        // Check if the current user is the trainer who created this contest
        var isOwner = await _dbContext.Contests
            .AnyAsync(c => c.Id == contestId && c.trainer.UserId == currentUserId);

        if (isOwner)
        {
            context.Succeed(requirement);
            return;
        }
    }
}