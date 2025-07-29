using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InstagramAutomation.Api.Models;

[Table("instagram_accounts")]
public class InstagramAccount
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("user_id")]
    public int UserId { get; set; }

    [Required]
    [StringLength(255)]
    [Column("instagram_user_id")]
    public string InstagramUserId { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    [Column("username")]
    public string Username { get; set; } = string.Empty;

    [StringLength(500)]
    [Column("display_name")]
    public string? DisplayName { get; set; }

    [StringLength(1000)]
    [Column("profile_picture_url")]
    public string? ProfilePictureUrl { get; set; }

    [Column("access_token")]
    public string? AccessToken { get; set; }

    [Column("token_expires_at")]
    public DateTime? TokenExpiresAt { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Column("last_sync")]
    public DateTime? LastSync { get; set; }

    [Column("follower_count")]
    public int? FollowerCount { get; set; }

    [Column("following_count")]
    public int? FollowingCount { get; set; }

    [Column("media_count")]
    public int? MediaCount { get; set; }

    // Navigation properties
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;
    
    public virtual ICollection<AutomationRule> AutomationRules { get; set; } = new List<AutomationRule>();
    public virtual ICollection<CommentEvent> CommentEvents { get; set; } = new List<CommentEvent>();
}

