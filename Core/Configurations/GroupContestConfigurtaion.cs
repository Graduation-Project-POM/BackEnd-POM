using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MIO.Core.Entities;
using MIO2.Core.Entities;

namespace MIO.Core.Configurations
{
    public class  GroupContestConfigurtaion : IEntityTypeConfiguration<GroupContest>
    {
        public void Configure(EntityTypeBuilder<GroupContest> builder)
        {
            builder.HasOne(gc => gc.Group)
                .WithMany(g => g.GroupContests)
                .HasForeignKey(gc => gc.GroupId);

            builder.HasOne(gc => gc.Contest)
                .WithMany()
                .HasForeignKey(gc => gc.ContestId);
        }
    }
}