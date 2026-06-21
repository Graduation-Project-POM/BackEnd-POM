using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MIO.Core.Entities;

namespace MIO.Core.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.Property(u => u.UserName).IsRequired().HasMaxLength(64);
        builder.Property(u => u.Email).IsRequired().HasMaxLength(256);

        builder.HasIndex(u => u.UserName).IsUnique();
        builder.HasIndex(u => u.Email).IsUnique();

        builder.Property(u => u.FirstName).IsRequired().HasMaxLength(100);
        builder.Property(u => u.MiddleName).HasMaxLength(100);
        builder.Property(u => u.LastName).IsRequired().HasMaxLength(100);
        builder.Property(u => u.PhoneNumber).HasMaxLength(32);
        builder.Property(u => u.ProfileImageUrl).HasMaxLength(500);
        builder.Property(u => u.Bio).HasMaxLength(1000);
        builder.Property(u => u.RefreshTokenHash).HasMaxLength(128);

        builder.Property(u => u.Gender).HasConversion<string>().HasMaxLength(16);
    }
}
