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
        [Route("auth")]
        [AllowAnonymous]
        public async Task<ActionResult<AuthResponseDTO>>AuthenticateUser([FromBody]AuthenticationRequest? request)
        {

            if(request == null) 
            {
                    return BadRequest("Please enter your login data");
            }

           var user= await userServices.GetUserService(request);

            if (user ==null)
                return Unauthorized("Invalid username or password");

            var accessToken = userServices.CreateToken(jwtOptions, user);


            return Ok(accessToken);

        }


        [HttpPost]
        [Route("")]
        
        public async Task<ActionResult> createUser(CreateUserDTO userDTO)
        {  
              var user=  await userServices.CreateUser(userDTO);
                return new OkObjectResult(new
                {
                    message = "User Created successfully",
                    userId = user.Id
                });      
        }

        [HttpGet]
        [Route("")]
       
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
          


            var records = await userServices.GetUsersService();
            return Ok(records);
        }


        [HttpGet]
        [Route("{id}")]

        public async Task<ActionResult> GetUserById(int id)
        {
          

            var  user= await userServices.GetUserByIDService(id);

          
            return Ok(user);
        }

        [HttpPut]
        [Route("{id}")]

        public async Task<ActionResult> UpdateUser(int id, UpdateUserDTO userDTO)
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
