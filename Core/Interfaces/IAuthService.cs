using MIO.Core.DTOs;
using MIO.Core.Entities;

namespace MIO2.Core.Interfaces;

public interface IAuthService
{
    AuthResponseDTO CreateToken(JwtOptions jwtOptions, User user);
    AuthResponseDTO Refresh(JwtOptions jwtOptions, RefreshRequest request,int userId);
    bool LogOut(LogoutRequest request,int userId);
}