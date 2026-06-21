using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MIO.Core.Entities;
using MIO.Core.Enums;
using MIO2.Core.Entities;

public class Problem
{
    [Key]
    public int ID { get; set; }

    [Required, MaxLength(255)]
    public string Title { get; set; }

    public string Statement { get; set; }

    public Difficulty Difficulty { get; set; }

    [MaxLength(255)]
    public string Topic { get; set; }


    [MaxLength(500)]
    public string? PhotoFileKey { get; set; }
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [Required]
    public DateTime? UpdatedAt { get; set; }= DateTime.UtcNow;

    public bool IsPublished { get; set; }

   
    public int? TrainerID { get; set; }

    [ForeignKey(nameof(TrainerID))]
    public Trainer? Trainer { get; set; }
    
    public ICollection<Contest> Contests { get; set; } = [];
    public ICollection<ProblemStudent> ProblemStudents { get; set; } = [];
}