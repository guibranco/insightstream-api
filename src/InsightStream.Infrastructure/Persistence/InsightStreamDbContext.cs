using InsightStream.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace InsightStream.Infrastructure.Persistence;

public class InsightStreamDbContext(DbContextOptions<InsightStreamDbContext> options)
    : DbContext(options)
{
    public DbSet<Newsletter> Newsletters => Set<Newsletter>();

    public DbSet<Link> Links => Set<Link>();

    public DbSet<Author> Authors => Set<Author>();

    public DbSet<NewsletterLink> NewsletterLinks => Set<NewsletterLink>();

    public DbSet<LinkAuthor> LinkAuthors => Set<LinkAuthor>();

    public DbSet<User> Users => Set<User>();

    public DbSet<UserPreference> UserPreferences => Set<UserPreference>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InsightStreamDbContext).Assembly);
    }

    public override int SaveChanges()
    {
        ApplyTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void ApplyTimestamps()
    {
        var now = DateTimeOffset.UtcNow;

        foreach (var entry in ChangeTracker.Entries<IHasTimestamps>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }
    }
}
