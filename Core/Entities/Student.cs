using MIO2.Core.Entities;

namespace MIO.Core.Entities
{
    public class Student
    {
        public int Id { get; set; }

        public string GradeLevel { get; set; }
        public string City { get; set; }
        public string SchoolName { get; set; }
        public string NationalId { get; set; }
        public int ProblemSolved { get; set; } = 0;
        public int Score { get; set; } = 0;
        public DateOnly JoinYear { get; set; }= DateOnly.FromDateTime(DateTime.Now);
        public bool IsAccepted { get; set; }= false;

        public int UserId { get; set; }
        public User User { get; set; }
        
        public ICollection<ContestStudent> ContestStudents { get; set; } = [];
        public ICollection<GroupStudent> GroupStudents { get; set; } = [];
        public ICollection<ProblemStudent> ProblemStudents { get; set; } = [];
    }
}
