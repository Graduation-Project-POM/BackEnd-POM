using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MIO.Core.Entities;

namespace MIO.Core.Configurations;

public class GroupContestConfiguration : IEntityTypeConfiguration<GroupContest>
{
    public void Configure(EntityTypeBuilder<GroupContest> builder)
    {
        builder.HasIndex(gc => new { gc.GroupId, gc.ContestId }).IsUnique();
    }
}
