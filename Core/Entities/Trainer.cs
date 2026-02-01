using System.ComponentModel.DataAnnotations.Schema;

namespace MIO.Core.Entities
{
    public class Trainer
    {
        public int Id { get; set; }

        public string Specialization { get; set; }
        public int UserId { get; set; }
        public User User { get; set; }


    }
}
