#nullable enable
using DataAccessLayer.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccessLayer.Configurations
{
    public class SubjectClassConfiguration : IEntityTypeConfiguration<SubjectClass>
    {
        public void Configure(EntityTypeBuilder<SubjectClass> builder)
        {
            builder.ToTable("SubjectClasses");

            builder.HasKey(sc => sc.Id);

            builder.Property(sc => sc.SubjectCode)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(sc => sc.ClassName)
                .IsRequired()
                .HasMaxLength(100);

            // Unique constraint: Subject + Class
            builder.HasIndex(sc => new { sc.SubjectCode, sc.ClassName })
                .IsUnique();

            // Relationships
            builder.HasMany(sc => sc.Students)
                .WithOne(s => s.SubjectClass)
                .HasForeignKey(s => s.SubjectClassId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(sc => sc.GradingComponents)
                .WithOne(gc => gc.SubjectClass)
                .HasForeignKey(gc => gc.SubjectClassId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
