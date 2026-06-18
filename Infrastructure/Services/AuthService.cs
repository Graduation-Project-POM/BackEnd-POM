using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.IdentityModel.Tokens;
using MIO.API.Settings;
using MIO.Core.DTOs;
using MIO.Core.Entities;
using MIO.Infrastructure;
using MIO.Infrastructure.Services;
using MIO2.Core.Interfaces;
using Org.BouncyCastle.Crypto.Generators;

namespace MIO2.Infrastructure.Services;

public class AuthService :IAuthService
{
    private readonly AppDbContext _dbContext;
  

    public AuthService(AppDbContext dbContext)
    {
        _dbContext=dbContext;
    }
    
    public AuthResponseDTO CreateToken(JwtOptions jwtOptions,User user)
    { 
        var expires = DateTime.UtcNow.AddMinutes(jwtOptions.lifetime);


        var accessToken = GenerateAccessToken(jwtOptions, user);

        
        var refreshToken = GenerateRefreshToken();
        
        user.RefreshTokenHash = Security.HashRefreshToken(refreshToken);
        user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7);
        user.RefreshTokenRevokedAt = null;
        _dbContext.SaveChanges();

        var trainerId = _dbContext.Trainers
            .Where(t => t.UserId == user.Id)
            .Select(t => (int?)t.Id)
            .FirstOrDefault();

        return new AuthResponseDTO
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = expires,
            UserId = user.Id,
            UserName = user.UserName,
            Role = user.RoleID.ToString(),
            TrainerId = trainerId
        };
    }
    
    public AuthResponseDTO Refresh(JwtOptions jwtOptions,RefreshRequest request,int userId)
    {
        var user = _dbContext.Users
            .FirstOrDefault(u => u.Id == userId);
      
    
        if (user == null)
            throw new AppException("Invalid refresh token", StatusCodes.Status401Unauthorized);
    
        if (user.RefreshTokenRevokedAt != null)
            throw new AppException("Refresh token has been revoked",StatusCodes.Status401Unauthorized);
    
        if (user.RefreshTokenExpiresAt == null || user.RefreshTokenExpiresAt <= DateTime.UtcNow)
            throw new AppException("Refresh token has expired",StatusCodes.Status401Unauthorized);
    
        var incomingHash = Security.HashRefreshToken(request.RefreshToken);
        if (incomingHash != user.RefreshTokenHash)
            throw new AppException("Invalid refresh token",StatusCodes.Status401Unauthorized);
    
        // Rotate refresh token
        var newRefreshToken = GenerateRefreshToken();
        user.RefreshTokenHash = Security.HashRefreshToken(newRefreshToken);
        user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7);
        user.RefreshTokenRevokedAt = null;
    
        _dbContext.SaveChanges();
    
        var newAccessToken = GenerateAccessToken(jwtOptions, user);
    
        return new AuthResponseDTO
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken
        };
    }

    public bool LogOut(LogoutRequest request,int userId)
    {
        var user = _dbContext.Users
            .FirstOrDefault(u => u.Id == userId);
        
        
        if (user == null )
            throw new AppException("Invalid refresh token", StatusCodes.Status401Unauthorized);
        
        
        var incomingHash = Security.HashRefreshToken(request.RefreshToken);
        if (incomingHash != user.RefreshTokenHash)
            throw new AppException("Invalid refresh token", StatusCodes.Status401Unauthorized);
        
        if (user.RefreshTokenRevokedAt != null)
            return false;
        
        user.RefreshTokenRevokedAt = DateTime.UtcNow;
        _dbContext.SaveChanges(); 
        return true;
        
    }
    
    private static string GenerateRefreshToken()
    {
        var bytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }
    
    
    private string GenerateAccessToken(JwtOptions jwtOptions, User user)
    {
        var expires = DateTime.UtcNow.AddMinutes(jwtOptions.lifetime);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.UserName),
            new Claim(ClaimTypes.Role, user.RoleID.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: jwtOptions.Issuer,
            audience: jwtOptions.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.signingkey)),
                SecurityAlgorithms.HmacSha256)
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
    
    
    
}