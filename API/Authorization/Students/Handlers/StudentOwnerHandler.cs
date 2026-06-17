using Microsoft.EntityFrameworkCore;
using MIO.Infrastructure;

namespace MIO2.API.Authorization;

using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

// This authorization handler enforces the ownership rule for student resources.
// It checks whether the current user is either:
// - An Admin (full access), OR
// - The owner of the student record being requested
public class StudentOwnerHandler
    : AuthorizationHandler<StudentOwnerRequirement>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly AppDbContext _dbContext;

    public StudentOwnerHandler(
        IHttpContextAccessor httpContextAccessor,
        AppDbContext dbContext)
    {
        _httpContextAccessor = httpContextAccessor;
        _dbContext = dbContext;
    }

    protected async override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        StudentOwnerRequirement requirement)
    {
        if (context.User.IsInRole("Admin") || context.User.IsInRole("Trainer"))
        {
            context.Succeed(requirement);
            return;
        }

        var httpContext = _httpContextAccessor.HttpContext!;
        
        // get the {id} from the route
        var routeId = httpContext.Request.RouteValues["id"]?.ToString();

        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (routeId == null || userId == null) return;

        if (!int.TryParse(routeId, out int studentId) ||
            !int.TryParse(userId, out int currentUserId)) return;
        
        var isOwner=await _dbContext.Students
            .AnyAsync(s => s.Id == studentId && s.UserId == currentUserId);
        
        
        if (isOwner)
        {
            context.Succeed(requirement);
            return;
        }

        
    }
}