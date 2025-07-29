using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InstagramAutomation.Api.Models;

[Table("action_executions")]
public class ActionExecution
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("comment_event_id")]
    public int CommentEventId { get; set; }

    [Required]
    [Column("automation_rule_id")]
    public int AutomationRuleId { get; set; }

    [Required]
    [StringLength(50)]
    [Column("action_type")]
    public string ActionType { get; set; } = string.Empty; // public_reply, private_message

    [Required]
    [StringLength(50)]
    [Column("status")]
    public string Status { get; set; } = "pending"; // pending, success, failed, retrying

    [Column("response_text")]
    public string? ResponseText { get; set; }

    [StringLength(255)]
    [Column("response_id")]
    public string? ResponseId { get; set; } // ID da resposta no Instagram

    [Column("executed_at")]
    public DateTime? ExecutedAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("error_message")]
    public string? ErrorMessage { get; set; }

    [Column("retry_count")]
    public int RetryCount { get; set; } = 0;

    [Column("next_retry_at")]
    public DateTime? NextRetryAt { get; set; }

    [Column("api_response")]
    public string? ApiResponse { get; set; } // JSON response from Instagram API

    [Column("execution_time_ms")]
    public int? ExecutionTimeMs { get; set; }

    // Navigation properties
    [ForeignKey("CommentEventId")]
    public virtual CommentEvent CommentEvent { get; set; } = null!;

    [ForeignKey("AutomationRuleId")]
    public virtual AutomationRule AutomationRule { get; set; } = null!;
}

