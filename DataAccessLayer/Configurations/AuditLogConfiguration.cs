#nullable enable
using DataAccessLayer.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccessLayer.Configurations
{
    public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
    {
        public void Configure(EntityTypeBuilder<AuditLog> builder)
        {
            builder.ToTable("AuditLogs");

            builder.HasKey(a => a.Id);

            builder.Property(a => a.OldValue)
                .HasPrecision(8, 3)
                .IsRequired(false);

            builder.Property(a => a.NewValue)
                .HasPrecision(8, 3)
                .IsRequired(false);

            builder.Property(a => a.ChangedBy)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(a => a.ChangedAt)
                .IsRequired();

            builder.HasIndex(a => a.StudentId);
            builder.HasIndex(a => a.ComponentId);
            builder.HasIndex(a => a.ChangedAt);

            builder.HasOne(a => a.Student)
                .WithMany(s => s.AuditLogs)
                .HasForeignKey(a => a.StudentId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            builder.HasOne(a => a.Component)
                .WithMany(gc => gc.AuditLogs)
                .HasForeignKey(a => a.ComponentId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();
        }
    }
}
