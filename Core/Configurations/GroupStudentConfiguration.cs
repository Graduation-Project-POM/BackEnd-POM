using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MIO.Core.Entities;
using MIO2.Core.Entities;

namespace MIO.Core.Configurations
{
    public class  GroupStudentConfiguration : IEntityTypeConfiguration<GroupStudent>
    {
        public void Configure(EntityTypeBuilder<GroupStudent> builder)
        {
            builder.HasOne(gs => gs.Group)
                .WithMany(g => g.GroupStudents)
                .HasForeignKey(gs => gs.GroupId);
            
            builder.HasOne(gs => gs.Student)
                .WithMany(s => s.GroupStudents)  
                .HasForeignKey(gs => gs.StudentId);
        }
    }
}