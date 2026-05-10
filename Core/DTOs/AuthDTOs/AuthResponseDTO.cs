namespace MIO.Core.DTOs
{
    public class AuthResponseDTO
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public DateTime ExpiresAt { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string Role { get; set; }
    }
}
