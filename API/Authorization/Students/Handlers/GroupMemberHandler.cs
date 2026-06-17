using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using MIO.Infrastructure;
using System.Security.Claims;

namespace MIO2.API.Authorization;

// Checks whether the current user is either:
// - An Admin or Trainer (full access), OR
// - A Student who belongs to the requested group
public class GroupMemberHandler
    : AuthorizationHandler<GroupMemberRequirment>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly AppDbContext _dbContext;

    public GroupMemberHandler(
        IHttpContextAccessor httpContextAccessor,
        AppDbContext dbContext)
    {
        _httpContextAccessor = httpContextAccessor;
        _dbContext = dbContext;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        GroupMemberRequirment requirement)
    {
        if (context.User.IsInRole("Admin") || context.User.IsInRole("Trainer"))
        {
            context.Succeed(requirement);
            return;
        }

        var httpContext = _httpContextAccessor.HttpContext!;

        var routeId = httpContext.Request.RouteValues["id"]?.ToString();
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (routeId == null || userId == null) return;

        if (!int.TryParse(routeId, out int groupId) ||
            !int.TryParse(userId, out int currentUserId)) return;

        // Check if the current user is a student that belongs to this group
        var isMember = await _dbContext.GroupStudents
            .AnyAsync(gs => gs.GroupId == groupId && gs.Student.UserId == currentUserId);

        if (isMember)
        {
            context.Succeed(requirement);
            return;
        }
    }
}