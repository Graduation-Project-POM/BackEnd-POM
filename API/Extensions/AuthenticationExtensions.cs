using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MIO.API.Settings;
using MIO.Infrastructure;
using System.Security.Claims;
using System.Text;

namespace MIO.API.Extensions;

public static class AuthenticationExtensions
{
    private const string AccountInactive = "ACCOUNT_INACTIVE";

    // Primary-key seek that reads a single bit. Compiled once so the per-request
    // cost is just the round trip. Deleted users count as inactive.
    private static readonly Func<AppDbContext, int, Task<bool>> IsUserActive =
        EF.CompileAsyncQuery((AppDbContext db, int userId) =>
            db.Users.Any(u => u.Id == userId && u.IsActive));

    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtOptions = configuration
            .GetSection("Jwt")
            .Get<JwtOptions>()
            ?? throw new InvalidOperationException("Missing 'Jwt' configuration section.");

        services.AddSingleton(jwtOptions);

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.SaveToken = true;

                options.TokenValidationParameters =
                    new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = jwtOptions.Issuer,

                        ValidateAudience = true,
                        ValidAudience = jwtOptions.Audience,

                        ValidateIssuerSigningKey = true,

                        IssuerSigningKey =
                            new SymmetricSecurityKey(
                                Encoding.UTF8.GetBytes(jwtOptions.signingkey)),
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.Zero
                    };

                options.Events = new JwtBearerEvents
                {
                    // Runs on every request carrying a valid token, before any
                    // [Authorize] check, so deactivation takes effect immediately
                    // instead of when the access token expires. Checked live
                    // (no cache) so there is no window where it's stale.
                    OnTokenValidated = async context =>
                    {
                        var subject = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                        if (!int.TryParse(subject, out var userId))
                        {
                            context.Fail("Token has no valid user id.");
                            return;
                        }

                        var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
                        if (!await IsUserActive(db, userId))
                            context.Fail(AccountInactive);
                    },

                    // Give the inactive case the API's usual error shape so the
                    // client can tell it apart from an expired/invalid token.
                    OnChallenge = async context =>
                    {
                        if (context.AuthenticateFailure?.Message != AccountInactive)
                            return;

                        context.HandleResponse();
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        await context.Response.WriteAsJsonAsync(new
                        {
                            status = StatusCodes.Status401Unauthorized,
                            code = AccountInactive,
                            message = "Your account has been deactivated"
                        });
                    }
                };
            });

        return services;
    }
}
