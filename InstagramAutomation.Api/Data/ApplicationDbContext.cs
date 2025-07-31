using Microsoft.EntityFrameworkCore;
using InstagramAutomation.Api.Models;

namespace InstagramAutomation.Api.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<InstagramAccount> InstagramAccounts { get; set; }
    public DbSet<AutomationRule> AutomationRules { get; set; }
    public DbSet<CommentEvent> CommentEvents { get; set; }
    public DbSet<ActionExecution> ActionExecutions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // InstagramAccount configuration
        modelBuilder.Entity<InstagramAccount>(entity =>
        {
            entity.HasIndex(e => e.InstagramUserId).IsUnique();
            entity.HasIndex(e => e.Username);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.User)
                .WithMany(p => p.InstagramAccounts)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // AutomationRule configuration
        modelBuilder.Entity<AutomationRule>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.Name });
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.User)
                .WithMany(p => p.AutomationRules)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.InstagramAccount)
                .WithMany(p => p.AutomationRules)
                .HasForeignKey(d => d.InstagramAccountId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // CommentEvent configuration
        modelBuilder.Entity<CommentEvent>(entity =>
        {
            entity.HasIndex(e => e.CommentId).IsUnique();
            entity.HasIndex(e => new { e.InstagramAccountId, e.CommentTimestamp });
            entity.HasIndex(e => e.Processed);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.InstagramAccount)
                .WithMany(p => p.CommentEvents)
                .HasForeignKey(d => d.InstagramAccountId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ActionExecution configuration
        modelBuilder.Entity<ActionExecution>(entity =>
        {
            entity.HasIndex(e => new { e.CommentEventId, e.ActionType });
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.NextRetryAt);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.CommentEvent)
                .WithMany(p => p.ActionExecutions)
                .HasForeignKey(d => d.CommentEventId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.AutomationRule)
                .WithMany(p => p.ActionExecutions)
                .HasForeignKey(d => d.AutomationRuleId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is User || e.Entity is InstagramAccount || e.Entity is AutomationRule)
            .Where(e => e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.Entity is User user)
                user.UpdatedAt = DateTime.UtcNow;
            else if (entry.Entity is InstagramAccount account)
                account.UpdatedAt = DateTime.UtcNow;
            else if (entry.Entity is AutomationRule rule)
                rule.UpdatedAt = DateTime.UtcNow;
        }
    }
}

