using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MIO2.Core.Entities;

namespace MIO.Core.Configurations
{
    public class ProblemCommentConfiguration : IEntityTypeConfiguration<ProblemComment>
    {
        public void Configure(EntityTypeBuilder<ProblemComment> builder)
        {
            builder.HasKey(c => c.Id);

            builder.Property(c => c.Body).HasMaxLength(4000);

            builder.HasOne(c => c.Problem)
                .WithMany()
                .HasForeignKey(c => c.ProblemId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(c => c.ParentComment)
                .WithMany(c => c.Replies)
                .HasForeignKey(c => c.ParentCommentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(c => new { c.ProblemId, c.CreatedAt });
        }
    }
}
