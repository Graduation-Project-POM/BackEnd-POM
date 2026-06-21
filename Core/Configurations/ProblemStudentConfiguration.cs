using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MIO2.Core.Entities;

namespace MIO.Core.Configurations;

public class ProblemStudentConfiguration : IEntityTypeConfiguration<ProblemStudent>
{
    public void Configure(EntityTypeBuilder<ProblemStudent> builder)
    {
        
        builder.HasIndex(ps => new { ps.ProblemId, ps.StudentId }).IsUnique();

        builder.HasOne(ps => ps.Problem)
            .WithMany(p => p.ProblemStudents)
            .HasForeignKey(ps => ps.ProblemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ps => ps.Student)
            .WithMany(s => s.ProblemStudents)
            .HasForeignKey(ps => ps.StudentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
