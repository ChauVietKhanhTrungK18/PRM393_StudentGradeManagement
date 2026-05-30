#nullable enable
using DataAccessLayer.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccessLayer.Configurations
{
    public class StudentConfiguration : IEntityTypeConfiguration<Student>
    {
        public void Configure(EntityTypeBuilder<Student> builder)
        {
            builder.ToTable("Students");

            builder.HasKey(s => s.Id);

            builder.Property(s => s.RollNumber)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(s => s.FullName)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(s => s.Comment)
                .HasMaxLength(1000);

            builder.HasIndex(s => new { s.SubjectClassId, s.RollNumber })
                .IsUnique();

            // Relationships
            builder.HasMany(s => s.Marks)
                .WithOne(m => m.Student)
                .HasForeignKey(m => m.StudentId)
                .OnDelete(DeleteBehavior.Cascade); 

            builder.HasMany(s => s.AuditLogs)
                .WithOne(a => a.Student)
                .HasForeignKey(a => a.StudentId)
                .OnDelete(DeleteBehavior.Cascade); 
        }
    }
}
