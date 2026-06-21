using MIO.Core.Interfaces;
using System.Security.Claims;

namespace MIO.Infrastructure.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            var user = httpContextAccessor.HttpContext?.User;

            if (user?.Identity?.IsAuthenticated != true)
                return;

   
            if (int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var id))
                UserId = id;
            UserName = user.Identity.Name;
            Role = user.FindFirstValue(ClaimTypes.Role);

        }

        public  int UserId { get; }
        public string? UserName { get; }
        public string? Role { get; }
    }
}
