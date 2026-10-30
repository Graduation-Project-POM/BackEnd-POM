using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MIO.API.Authentication;
using MIO.Core.DTOs;
using MIO.Core.Interfaces;
using MIO2.Core.Interfaces;

namespace MIO2.API.Controllers;
[ApiController]
[Route("[Controller]")]
[Authorize]
public class AuthController(JwtOptions jwtOptions,IUserService userServices, IAuthService authService) : ControllerBase
{
    
    [HttpPost]
    [Route("auth")]
    [EnableRateLimiting("AuthLimiter")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDTO>>AuthenticateUser([FromBody]AuthenticationRequest? request)
    {

        if (request == null ||
            string.IsNullOrWhiteSpace(request.UserName) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest("Please enter your login data");
        }


        var user= await userServices.GetUserService(request);

        if (user ==null)
            return Unauthorized("Invalid username or password");

        var accessToken = await authService.CreateToken(jwtOptions, user);


        return Ok(accessToken);

    }
    
    [HttpPost("refresh")]
    [EnableRateLimiting("AuthLimiter")]
    // Anonymous on purpose: it's called once the access token has expired. The
    // refresh token itself is the credential.
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
    {
        var refreshedToken = await authService.Refresh(jwtOptions, request);
        return Ok(refreshedToken);

    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
    {

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        if (await authService.LogOut(request,userId))
            return Ok("Logged out successfully");
        else
        {
            return Ok();
        }
    }
}