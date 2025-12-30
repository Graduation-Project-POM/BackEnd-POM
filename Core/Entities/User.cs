using MIO.Core.Enums;

namespace MIO.Core.Entities
{
    public class User
    {
        public int Id { get; set; }
        public required string FirstName { get; set; }
        public string MiddleName { get; set; }
        public required string LastName { get; set; }
        public string Email { get; set; }
        public string UserName { get; set; }
        public  byte[] PasswordHash { get; set; }
        public  byte[] PasswordSalt { get; set; }
        public required string PhoneNumber { get; set; }
        public required string Gender { get; set; }
        public string ProfileImageUrl { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastUpdatedAt { get; set; }
        public Role RoleID { get; set; }



        public Trainer Trainer { get; set; }
        public Student Student { get; set; }

    }
}
