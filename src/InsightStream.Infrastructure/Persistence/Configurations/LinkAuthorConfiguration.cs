using InsightStream.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InsightStream.Infrastructure.Persistence.Configurations;

public class LinkAuthorConfiguration : IEntityTypeConfiguration<LinkAuthor>
{
    public void Configure(EntityTypeBuilder<LinkAuthor> builder)
    {
        builder.HasKey(la => new { la.LinkId, la.AuthorId });

        builder
            .HasOne(la => la.Link)
            .WithMany(l => l.LinkAuthors)
            .HasForeignKey(la => la.LinkId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(la => la.Author)
            .WithMany(a => a.LinkAuthors)
            .HasForeignKey(la => la.AuthorId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
