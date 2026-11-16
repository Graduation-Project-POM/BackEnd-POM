namespace MIO.Core.DTOs.UserDtos
{
    // One sign-up request on the admin Approvals page, whatever the role.
    public class ApprovalResponseDTO
    {
        public int UserId { get; set; }
        // Student.Id or Trainer.Id: what the accept endpoints and profile pages use.
        public int ProfileId { get; set; }
        // "Student" or "Trainer".
        public string Role { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string? PhoneNumber { get; set; }
        public bool IsAccepted { get; set; }
        public DateTime CreatedAt { get; set; }
        // Role-specific detail: grade level for students, specialization for trainers.
        public string? GradeLevel { get; set; }
        public string? Specialization { get; set; }
    }
}
