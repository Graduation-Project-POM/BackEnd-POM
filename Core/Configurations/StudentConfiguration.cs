using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MIO.Core.Entities;

namespace MIO.Core.Configurations
{
    public class StudentConfiguration : IEntityTypeConfiguration<Student>
    {
        public void Configure(EntityTypeBuilder<Student> builder)
        {
            builder
                .HasOne(u=>u.User)
                .WithOne(s=>s.Student)
                .HasForeignKey<Student>(t=>t.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Property(s => s.GradeLevel).HasMaxLength(50);
            builder.Property(s => s.City).HasMaxLength(100);
            builder.Property(s => s.SchoolName).HasMaxLength(150);
            builder.Property(s => s.NationalId).HasMaxLength(20);
        }
    }
}
