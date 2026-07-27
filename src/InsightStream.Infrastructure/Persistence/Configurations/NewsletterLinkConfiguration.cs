using InsightStream.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InsightStream.Infrastructure.Persistence.Configurations;

public class NewsletterLinkConfiguration : IEntityTypeConfiguration<NewsletterLink>
{
    public void Configure(EntityTypeBuilder<NewsletterLink> builder)
    {
        builder.HasKey(nl => new { nl.NewsletterId, nl.LinkId });

        builder
            .HasOne(nl => nl.Newsletter)
            .WithMany(n => n.NewsletterLinks)
            .HasForeignKey(nl => nl.NewsletterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(nl => nl.Link)
            .WithMany(l => l.NewsletterLinks)
            .HasForeignKey(nl => nl.LinkId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
