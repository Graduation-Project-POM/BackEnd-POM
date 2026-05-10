using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

        var accessToken = authService.CreateToken(jwtOptions, user);


        return Ok(accessToken);

    }
    
    [HttpPost("refresh")]
    public IActionResult Refresh([FromBody] RefreshRequest request)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        
        var refreshedToken = authService.Refresh(jwtOptions, request,userId);
        return Ok(refreshedToken);

    }

    [HttpPost("logout")]
    public IActionResult Logout([FromBody] LogoutRequest request)
    {
        
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        
        if (authService.LogOut(request,userId))
            return Ok("Logged out successfully");
        else
        {
            return Ok();
        }
    }
}