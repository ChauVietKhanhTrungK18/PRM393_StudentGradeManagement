#nullable enable
using DataAccessLayer.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccessLayer.Configurations
{
    public class MarkConfiguration : IEntityTypeConfiguration<Mark>
    {
        public void Configure(EntityTypeBuilder<Mark> builder)
        {
            builder.ToTable("Marks");

            builder.HasKey(m => m.Id);

            builder.Property(m => m.Value)
                .HasPrecision(8, 3)
                .IsRequired();

            builder.Property(m => m.Comment)
                .HasMaxLength(1000);

            builder.HasIndex(m => new { m.StudentId, m.ComponentId })
                .IsUnique();

            builder.HasIndex(m => m.StudentId);
            builder.HasIndex(m => m.ComponentId);

            builder.HasOne(m => m.Student)
                .WithMany(s => s.Marks)
                .HasForeignKey(m => m.StudentId)
                .OnDelete(DeleteBehavior.Cascade) 
                .IsRequired();

            builder.HasOne(m => m.Component)
                .WithMany(gc => gc.Marks)
                .HasForeignKey(m => m.ComponentId)
                .OnDelete(DeleteBehavior.Restrict) 
                .IsRequired();
        }
    }
}
