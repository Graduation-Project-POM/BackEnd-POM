using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MIO2.Core.Entities;

namespace MIO.Core.Configurations;

public class ContestConfiguration :IEntityTypeConfiguration<Contest>
{
    public void Configure(EntityTypeBuilder<Contest> builder)
    {
        builder.HasOne(c => c.trainer)
            .WithMany(t => t.Contests)
            .HasForeignKey(c => c.TrainerId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasMany(c => c.ContestStudents)
            .WithOne(cs => cs.Contest)
            .HasForeignKey(cs => cs.ContestId)
            .OnDelete(DeleteBehavior.Cascade);
        
    }

   
}