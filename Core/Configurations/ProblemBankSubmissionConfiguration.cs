using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MIO2.Core.Entities;

namespace MIO.Core.Configurations
{
    public class ProblemBankSubmissionConfiguration : IEntityTypeConfiguration<ProblemBankSubmission>
    {
        public void Configure(EntityTypeBuilder<ProblemBankSubmission> builder)
        {
            builder.HasKey(s => s.Id);

            builder.HasOne(s => s.Problem)
                .WithMany()
                .HasForeignKey(s => s.ProblemId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(s => s.Student)
                .WithMany()
                .HasForeignKey(s => s.StudentId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
