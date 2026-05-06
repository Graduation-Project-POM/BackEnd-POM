using MIO.Core.Entities;

namespace MIO2.Core.Entities;

public class ContestStudent
{
        public int Id { get; set; }
        
        public int StudentId { get; set; }
        public Student Student { get; set; }
        
        public int ContestId { get; set; }
        public Contest Contest { get; set; }
    
       
    
        public int ProblemSolved { get; set; }
        public int Score { get; set; }
        public double TimeToFinishTheContest { get; set; }
    }
