using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MIO.API.Authentication;
using MIO.Core.DTOs;
using MIO.Core.DTOs.UserDtos;
using MIO.Core.Entities;
using MIO.Core.Interfaces;
using MIO.Infrastructure.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace MIO.API.Controllers
{
    [ApiController]
    [Route("[Controller]")]
    [Authorize]
    public class UserController(JwtOptions jwtOptions,IUserService userServices) : ControllerBase
    {
        
        [HttpPost]
        [Route("")]
        [Authorize(Roles ="Admin")]
        public async Task<ActionResult> createUser(CreateUserDTO userDTO)
        {  
              var created=  await userServices.CreateUser(userDTO);
              
                return new OkObjectResult(new
                {
                    message = "User Created successfully",
                    userId = created.User.Id,
                    // Only set when the admin left the password empty; shown once.
                    generatedPassword = created.GeneratedPassword
                });
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register(CreateUserDTO dto)
        {
            var created = await userServices.RegisterUser(dto);
            return Ok(new
            {
                message = "Account created successfully",
                userId = created.User.Id,
                generatedPassword = created.GeneratedPassword
            });
        }

        [HttpGet]
        [Route("")]
        [Authorize(Roles ="Admin")]
        public async Task<ActionResult<IEnumerable<UserResponseDTO>>> GetUsers()
        {
            var records = await userServices.GetUsersService();
            return Ok(records);
        }


        // Sign-up requests from every self-registering role (students and
        // trainers), pending and approved, for the admin Approvals page.
        [HttpGet("approvals")]
        [Authorize(Roles ="Admin")]
        public async Task<ActionResult<IEnumerable<ApprovalResponseDTO>>> GetApprovals()
        {
            return Ok(await userServices.GetApprovalsService());
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<ActionResult> GetCurrentUser()
        {
            return Ok(await userServices.GetCurrentUserService());
        }

        [HttpPut("me")]
        [Authorize]
        public async Task<ActionResult> UpdateCurrentUser(UpdateUserDTO userDTO)
        {
            if (userDTO == null)
                return BadRequest("User data cannot be null");

            var updated = await userServices.UpdateCurrentUserService(userDTO);
            return Ok(new
            {
                message = "Profile updated successfully",
                userId = updated!.Id
            });
        }

        [HttpGet]
        [Route("{id}")]
        [Authorize(Roles ="Admin")]
        public async Task<ActionResult> GetUserById(int id)
        {
            var  user= await userServices.GetUserByIDService(id);
            return Ok(user);
        }

        [HttpPut]
        [Route("{id}")]
        [Authorize(Roles ="Admin")]
        public async Task<ActionResult> UpdateUser(int id, AdminUpdateUserDTO userDTO)
        {

            if (userDTO == null)
                return new BadRequestObjectResult("User data cannot be null");

            if (id <= 0)
                return new BadRequestObjectResult("Invalid user ID");

            
            var updatedUser = await userServices.UpdateUserService(id, userDTO);

                if (updatedUser == null)
                    return NotFound($"User with ID {id} not found");

                return new OkObjectResult(new
                {
                    message = "User updated successfully",
                    userId = updatedUser.Id
                });

        }

        [HttpDelete]
        [Route("{id}")]
        [Authorize(Roles ="Admin")]
        public async Task<ActionResult> DeleteUser(int id)
        {

            if (id <= 0)
                return new BadRequestObjectResult("Invalid user ID");

          
                var deletedUser = await userServices.DeleteUserService(id);

                return new OkObjectResult(new
                {
                    message = "User Deleted successfully",
                    userId = deletedUser.Id
                });
            

        }

    }



}
