using InsightStream.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InsightStream.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    /// <summary>
    /// Seeded admin: username "admin", password "ChangeMe123!". Change it immediately after first
    /// deploy — see deploy/deploy.md "Rotating the seeded admin password".
    /// </summary>
    public static readonly Guid SeedAdminId = Guid.Parse("00000000-0000-7000-8000-000000000001");

    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Username).IsRequired().HasMaxLength(64);

        builder.Property(u => u.PasswordHash).IsRequired();

        builder.HasIndex(u => u.Username).IsUnique();

        var now = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        builder.HasData(
            new
            {
                Id = SeedAdminId,
                Username = "admin",
                PasswordHash = "$2a$11$Lemo1jqJeszZ7t4DtrQe3.KDbMT67aBQa9adkEDaLH7im39QgUMuq",
                LastLogin = (DateTimeOffset?)null,
                CreatedAt = now,
                UpdatedAt = now,
            }
        );
    }
}
