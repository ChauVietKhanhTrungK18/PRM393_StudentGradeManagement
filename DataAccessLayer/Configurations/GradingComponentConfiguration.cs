#nullable enable
using DataAccessLayer.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccessLayer.Configurations
{
    public class GradingComponentConfiguration : IEntityTypeConfiguration<GradingComponent>
    {
        public void Configure(EntityTypeBuilder<GradingComponent> builder)
        {
            builder.ToTable("GradingComponents");

            builder.HasKey(gc => gc.Id);

            builder.Property(gc => gc.Name)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(gc => gc.MaxMark)
                .HasPrecision(8, 3) // allow fractional marks, precision as needed
                .IsRequired();

            builder.Property(gc => gc.Weight)
                .HasPrecision(5, 2)
                .IsRequired();

            builder.Property(gc => gc.IsCondition)
                .IsRequired();

            builder.HasIndex(gc => new { gc.SubjectClassId, gc.Name })
                .IsUnique();

            builder.HasMany(gc => gc.Marks)
                .WithOne(m => m.Component)
                .HasForeignKey(m => m.ComponentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(gc => gc.AuditLogs)
                .WithOne(a => a.Component)
                .HasForeignKey(a => a.ComponentId)
                .OnDelete(DeleteBehavior.Restrict); 
        }
    }
}
