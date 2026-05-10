using System.ComponentModel.DataAnnotations;

namespace MIO.Core.DTOs;

public class LogoutRequest
{
  
    
    [Required]
    public string RefreshToken { get; set; }
}