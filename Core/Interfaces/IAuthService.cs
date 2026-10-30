using MIO.Core.DTOs;
using MIO.Core.Entities;

namespace MIO2.Core.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDTO> CreateToken(JwtOptions jwtOptions, User user);
    Task<AuthResponseDTO> Refresh(JwtOptions jwtOptions, RefreshRequest request);
    Task<bool> LogOut(LogoutRequest request,int userId);
}