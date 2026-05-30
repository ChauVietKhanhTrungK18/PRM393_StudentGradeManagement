using DataAccessLayer.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccessLayer.Configurations
{
    public class SessionConfiguration : IEntityTypeConfiguration<Session>
    {
        public void Configure(EntityTypeBuilder<Session> builder)
        {
            builder.ToTable("Sessions");

            builder.HasKey(s => s.Id);

            builder.Property(s => s.FGPath)
                .IsRequired()
                .HasMaxLength(1024);

            builder.Property(s => s.TeacherName)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(s => s.Semester)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(s => s.OpenedAt)
                .IsRequired();
        }
    }
}
