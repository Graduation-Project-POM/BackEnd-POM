using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MIO2.Core.Entities;

namespace MIO.Core.Configurations;

public class GalleryConfiguration : IEntityTypeConfiguration<Gallery>
{
    public void Configure(EntityTypeBuilder<Gallery> builder)
    {
        builder.Property(g => g.FileKey).IsRequired().HasMaxLength(500);
        builder.Property(g => g.RefType).HasMaxLength(50);
        builder.Property(g => g.Title).HasMaxLength(200);
        builder.Property(g => g.Description).HasMaxLength(2000);

        builder.HasOne(g => g.User)
            .WithMany()
            .HasForeignKey(g => g.UploadedByID)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
