using System.ComponentModel.DataAnnotations.Schema;
using MIO2.Core.Entities;

namespace MIO.Core.Entities
{
    public class Trainer
    {
        public int Id { get; set; }

        public string Specialization { get; set; }
        public int UserId { get; set; }
        public User User { get; set; }

        public ICollection<Contest> Contests { get; set; } = [];
    }
}
