using InsightStream.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InsightStream.Infrastructure.Persistence.Configurations;

public class NewsletterConfiguration : IEntityTypeConfiguration<Newsletter>
{
    public void Configure(EntityTypeBuilder<Newsletter> builder)
    {
        builder.HasKey(n => n.Id);

        builder.Property(n => n.Title).IsRequired();

        builder.Property(n => n.DestinationEmail).IsRequired();

        builder.Property(n => n.RawContent).IsRequired().HasColumnType("text");

        builder.Property(n => n.EmailHash).IsRequired().HasColumnType("char(64)");

        builder.HasIndex(n => n.EmailHash).IsUnique();
    }
}
