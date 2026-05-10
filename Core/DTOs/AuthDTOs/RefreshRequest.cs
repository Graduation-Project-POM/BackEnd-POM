using System.ComponentModel.DataAnnotations;

namespace MIO.Core.DTOs;

public class RefreshRequest
{
    [Required]
    public string RefreshToken { get; set; }
}