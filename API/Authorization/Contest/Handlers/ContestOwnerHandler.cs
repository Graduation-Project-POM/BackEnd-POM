using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using MIO.Infrastructure;
using System.Security.Claims;

namespace MIO2.API.Authorization.Contest;

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
        if (context.User.IsInRole("Admin"))
        {
            context.Succeed(requirement);
            return;
        }

        var httpContext = _httpContextAccessor.HttpContext!;

        var routeId = httpContext.Request.RouteValues["contestId"]?.ToString();
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (routeId == null || userId == null) return;

        if (!int.TryParse(routeId, out int contestId) ||
            !int.TryParse(userId, out int currentUserId)) return;

        var isOwner = await _dbContext.Contests
            .AnyAsync(c => c.Id == contestId && c.trainer.UserId == currentUserId);

        if (!isOwner)
        {
            isOwner = await _dbContext.GroupContests
                .AnyAsync(gc => gc.ContestId == contestId && gc.Group.Trainer.UserId == currentUserId);
        }

        if (isOwner)
        {
            context.Succeed(requirement);
            return;
        }
    }
}