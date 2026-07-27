using InsightStream.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InsightStream.Infrastructure.Persistence.Configurations;

public class UserPreferenceConfiguration : IEntityTypeConfiguration<UserPreference>
{
    public void Configure(EntityTypeBuilder<UserPreference> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.PreferenceType).IsRequired().HasConversion<string>().HasMaxLength(16);

        builder.Property(p => p.PreferenceValue).IsRequired();

        builder.Property(p => p.Weight).IsRequired().HasColumnType("numeric(3,2)");

        builder
            .HasOne(p => p.User)
            .WithMany(u => u.UserPreferences)
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => new { p.UserId, p.PreferenceType, p.PreferenceValue }).IsUnique();

        builder.ToTable(t =>
            t.HasCheckConstraint("ck_user_preferences_weight_range", "weight >= -1.00 AND weight <= 1.00")
        );
    }
}
