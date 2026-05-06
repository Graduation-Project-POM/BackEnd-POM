using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MIO2.Core.Entities;

public class ContestStudentConfiguration : IEntityTypeConfiguration<ContestStudent>
{
    public void Configure(EntityTypeBuilder<ContestStudent> builder)
    {
        builder.HasKey(cs => cs.Id);

        builder.HasOne(cs => cs.Student)
            .WithMany(s => s.ContestStudents)
            .HasForeignKey(cs => cs.StudentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}