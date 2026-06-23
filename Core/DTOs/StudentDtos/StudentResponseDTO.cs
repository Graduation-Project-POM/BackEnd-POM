using MIO.Core.DTOs.UserDtos;

namespace MIO.Core.DTOs.StudentDTO
{
    public class StudentResponseDTO
    {
        public int Id { get; set; }
        public string GradeLevel { get; set; }
        public string City { get; set; }
        public string SchoolName { get; set; }
        public string NationalId { get; set; }
        public int ProblemSolved { get; set; }
        public int Score { get; set; }

        public int TotalPoints { get; set; }
        public int Rank { get; set; }

        public DateOnly JoinYear { get; set; }
        public bool IsAccepted { get; set; }
        public BasicUserDTO BasicUser { get; set; }
    }
}
