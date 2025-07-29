using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InstagramAutomation.Api.Models;

[Table("users")]
public class User
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [EmailAddress]
    [StringLength(255)]
    [Column("email")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    [Column("password_hash")]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    [Column("full_name")]
    public string FullName { get; set; } = string.Empty;

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Column("last_login")]
    public DateTime? LastLogin { get; set; }

    [StringLength(50)]
    [Column("subscription_tier")]
    public string SubscriptionTier { get; set; } = "free";

    [Column("subscription_expires_at")]
    public DateTime? SubscriptionExpiresAt { get; set; }

    // Navigation properties
    public virtual ICollection<InstagramAccount> InstagramAccounts { get; set; } = new List<InstagramAccount>();
    public virtual ICollection<AutomationRule> AutomationRules { get; set; } = new List<AutomationRule>();
}

