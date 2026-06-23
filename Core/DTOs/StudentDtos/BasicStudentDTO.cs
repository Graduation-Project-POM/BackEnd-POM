namespace MIO.Core.DTOs.StudentDtos
{
    public class BasicStudentDTO
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
    }
}
