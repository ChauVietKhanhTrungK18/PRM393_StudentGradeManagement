using System.Reflection;
using DataAccessLayer.Entities;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.DbContexts
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Session> Sessions => Set<Session>();
        public DbSet<SubjectClass> SubjectClasses => Set<SubjectClass>();
        public DbSet<Student> Students => Set<Student>();
        public DbSet<GradingComponent> GradingComponents => Set<GradingComponent>();
        public DbSet<Mark> Marks => Set<Mark>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
        public DbSet<Snapshot> Snapshots => Set<Snapshot>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

            base.OnModelCreating(modelBuilder);
        }
    }
}
