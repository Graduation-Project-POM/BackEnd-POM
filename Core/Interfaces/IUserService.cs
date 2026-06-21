using Microsoft.AspNetCore.Mvc;
using MIO.API.Authentication;
using MIO.Core.DTOs;
using MIO.Core.DTOs.UserDtos;
using MIO.Core.Entities;

namespace MIO.Core.Interfaces
{
    public interface IUserService
    {
        Task<User?> CreateUser(CreateUserDTO userDTO);

        Task<User?> RegisterStudent(CreateUserDTO userDTO);

        //Task<bool> IsUserExist(AuthenticationRequest request);

        Task<User?> GetUserService(AuthenticationRequest request);

         Task<UserResponseDTO?> GetUserByIDService(int UserId);

        

        Task<IEnumerable<UserResponseDTO>> GetUsersService();

        Task<User?> UpdateUserService(int id, UpdateUserDTO userDTO);


        Task<User?> DeleteUserService(int id);

        Task<UserResponseDTO?> GetCurrentUserService();
        Task<User?> UpdateCurrentUserService(UpdateUserDTO userDTO);

    }
}
