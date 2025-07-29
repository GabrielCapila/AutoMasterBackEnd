using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InstagramAutomation.Api.Models;

[Table("automation_rules")]
public class AutomationRule
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("user_id")]
    public int UserId { get; set; }

    [Required]
    [Column("instagram_account_id")]
    public int InstagramAccountId { get; set; }

    [Required]
    [StringLength(255)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    [Column("description")]
    public string? Description { get; set; }

    [Required]
    [Column("trigger_keywords")]
    public string TriggerKeywords { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    [Column("match_type")]
    public string MatchType { get; set; } = "exact"; // exact, partial, regex, fuzzy

    [Column("case_sensitive")]
    public bool CaseSensitive { get; set; } = false;

    [Column("fuzzy_threshold")]
    public double? FuzzyThreshold { get; set; } = 0.8;

    [Column("public_response")]
    public string? PublicResponse { get; set; }

    [Column("private_message")]
    public string? PrivateMessage { get; set; }

    [Column("send_private_message")]
    public bool SendPrivateMessage { get; set; } = false;

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("priority")]
    public int Priority { get; set; } = 1;

    [Column("max_executions_per_hour")]
    public int? MaxExecutionsPerHour { get; set; }

    [Column("max_executions_per_day")]
    public int? MaxExecutionsPerDay { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Column("last_executed")]
    public DateTime? LastExecuted { get; set; }

    [Column("execution_count")]
    public int ExecutionCount { get; set; } = 0;

    // Navigation properties
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;

    [ForeignKey("InstagramAccountId")]
    public virtual InstagramAccount InstagramAccount { get; set; } = null!;

    public virtual ICollection<ActionExecution> ActionExecutions { get; set; } = new List<ActionExecution>();
}

