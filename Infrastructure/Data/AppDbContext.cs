using Microsoft.EntityFrameworkCore;
using MIO.Core.Entities;
using MIO2.Core.Entities;


namespace MIO.Infrastructure
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);    
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Trainer> Trainers { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<Gallery> Gallery { get; set; }
        public DbSet<Problem> Problems { get; set; }
        public DbSet<Contest> Contests { get; set; }
        public DbSet<ContestStudent>  ContestStudents { get; set; }
        public DbSet<Group> Groups { get; set; }
        public DbSet<GroupStudent> GroupStudents { get; set; }
        public DbSet<GroupContest> GroupContests { get; set; }
        public DbSet<ProblemStudent> ProblemStudents { get; set; }
        public DbSet<ProblemBankSubmission> ProblemBankSubmissions { get; set; }
        public DbSet<ProblemComment> ProblemComments { get; set; }

    }
}
