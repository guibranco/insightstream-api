using InsightStream.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InsightStream.Infrastructure.Persistence.Configurations;

public class LinkConfiguration : IEntityTypeConfiguration<Link>
{
    public void Configure(EntityTypeBuilder<Link> builder)
    {
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Url).IsRequired().HasColumnType("text");

        builder.Property(l => l.UrlHash).IsRequired().HasColumnType("char(64)");

        builder.Property(l => l.Title).IsRequired();

        builder.Property(l => l.Status).IsRequired().HasConversion<string>().HasMaxLength(32);

        builder.Property(l => l.PriorityScore).IsRequired().HasColumnType("numeric(5,2)").HasDefaultValue(5.00m);

        builder.HasIndex(l => l.UrlHash).IsUnique();

        builder.HasIndex(l => l.Status);

        builder.HasIndex(l => l.PriorityScore).IsDescending(true);
    }
}
