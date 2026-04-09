using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MIO.Core.Entities;

namespace MIO.Core.Configurations
{
    public class ProblemConfiguration : IEntityTypeConfiguration<Problem>
    {
        public void Configure(EntityTypeBuilder<Problem> builder)
        {
            builder.HasOne(p => p.Trainer)
                .WithMany()
                .HasForeignKey(p => p.TrainerID)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}