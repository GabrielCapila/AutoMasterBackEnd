using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InstagramAutomation.Api.Models;

[Table("comment_events")]
public class CommentEvent
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("instagram_account_id")]
    public int InstagramAccountId { get; set; }

    [Required]
    [StringLength(255)]
    [Column("comment_id")]
    public string CommentId { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    [Column("media_id")]
    public string MediaId { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    [Column("commenter_id")]
    public string CommenterId { get; set; } = string.Empty;

    [StringLength(255)]
    [Column("commenter_username")]
    public string? CommenterUsername { get; set; }

    [Required]
    [Column("comment_text")]
    public string CommentText { get; set; } = string.Empty;

    [Column("comment_timestamp")]
    public DateTime CommentTimestamp { get; set; }

    [Column("processed")]
    public bool Processed { get; set; } = false;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("processed_at")]
    public DateTime? ProcessedAt { get; set; }

    [StringLength(50)]
    [Column("media_type")]
    public string? MediaType { get; set; } // post, reel, story

    [StringLength(1000)]
    [Column("media_url")]
    public string? MediaUrl { get; set; }

    [Column("webhook_data")]
    public string? WebhookData { get; set; } // JSON raw data

    // Navigation properties
    [ForeignKey("InstagramAccountId")]
    public virtual InstagramAccount InstagramAccount { get; set; } = null!;

    public virtual ICollection<ActionExecution> ActionExecutions { get; set; } = new List<ActionExecution>();
}

