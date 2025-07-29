using System.ComponentModel.DataAnnotations;

namespace InstagramAutomation.Api.DTOs;

public class InstagramAccountRequest
{
    [Required(ErrorMessage = "Access token é obrigatório")]
    public string AccessToken { get; set; } = string.Empty;

    public string? DisplayName { get; set; }
}

public class InstagramAccountResponse
{
    public int Id { get; set; }
    public string InstagramUserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastSync { get; set; }
    public int? FollowerCount { get; set; }
    public int? FollowingCount { get; set; }
    public int? MediaCount { get; set; }
    public bool TokenValid { get; set; }
    public DateTime? TokenExpiresAt { get; set; }
}

public class AutomationRuleRequest
{
    [Required(ErrorMessage = "Nome é obrigatório")]
    [StringLength(255, ErrorMessage = "Nome deve ter no máximo 255 caracteres")]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Descrição deve ter no máximo 1000 caracteres")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Palavras-chave são obrigatórias")]
    public string TriggerKeywords { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tipo de correspondência é obrigatório")]
    public string MatchType { get; set; } = "exact";

    public bool CaseSensitive { get; set; } = false;

    [Range(0.1, 1.0, ErrorMessage = "Threshold deve estar entre 0.1 e 1.0")]
    public double? FuzzyThreshold { get; set; } = 0.8;

    public string? PublicResponse { get; set; }

    public string? PrivateMessage { get; set; }

    public bool SendPrivateMessage { get; set; } = false;

    public bool IsActive { get; set; } = true;

    [Range(1, 10, ErrorMessage = "Prioridade deve estar entre 1 e 10")]
    public int Priority { get; set; } = 1;

    [Range(1, 1000, ErrorMessage = "Máximo de execuções por hora deve estar entre 1 e 1000")]
    public int? MaxExecutionsPerHour { get; set; }

    [Range(1, 10000, ErrorMessage = "Máximo de execuções por dia deve estar entre 1 e 10000")]
    public int? MaxExecutionsPerDay { get; set; }

    [Required(ErrorMessage = "ID da conta Instagram é obrigatório")]
    public int InstagramAccountId { get; set; }
}

public class AutomationRuleResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string TriggerKeywords { get; set; } = string.Empty;
    public string MatchType { get; set; } = string.Empty;
    public bool CaseSensitive { get; set; }
    public double? FuzzyThreshold { get; set; }
    public string? PublicResponse { get; set; }
    public string? PrivateMessage { get; set; }
    public bool SendPrivateMessage { get; set; }
    public bool IsActive { get; set; }
    public int Priority { get; set; }
    public int? MaxExecutionsPerHour { get; set; }
    public int? MaxExecutionsPerDay { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? LastExecuted { get; set; }
    public int ExecutionCount { get; set; }
    public InstagramAccountResponse InstagramAccount { get; set; } = null!;
}

public class CommentEventResponse
{
    public int Id { get; set; }
    public string CommentId { get; set; } = string.Empty;
    public string MediaId { get; set; } = string.Empty;
    public string CommenterId { get; set; } = string.Empty;
    public string? CommenterUsername { get; set; }
    public string CommentText { get; set; } = string.Empty;
    public DateTime CommentTimestamp { get; set; }
    public bool Processed { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? MediaType { get; set; }
    public string? MediaUrl { get; set; }
    public InstagramAccountResponse InstagramAccount { get; set; } = null!;
}

public class ActionExecutionResponse
{
    public int Id { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? ResponseText { get; set; }
    public string? ResponseId { get; set; }
    public DateTime? ExecutedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }
    public DateTime? NextRetryAt { get; set; }
    public int? ExecutionTimeMs { get; set; }
    public CommentEventResponse CommentEvent { get; set; } = null!;
    public AutomationRuleResponse AutomationRule { get; set; } = null!;
}

public class WebhookRequest
{
    public string Object { get; set; } = string.Empty;
    public List<WebhookEntry> Entry { get; set; } = new();
}

public class WebhookEntry
{
    public string Id { get; set; } = string.Empty;
    public long Time { get; set; }
    public List<WebhookChange> Changes { get; set; } = new();
}

public class WebhookChange
{
    public string Field { get; set; } = string.Empty;
    public WebhookValue Value { get; set; } = new();
}

public class WebhookValue
{
    public string Id { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public WebhookFrom From { get; set; } = new();
    public WebhookMedia Media { get; set; } = new();
}

public class WebhookFrom
{
    public string Id { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
}

public class WebhookMedia
{
    public string Id { get; set; } = string.Empty;
    public string MediaType { get; set; } = string.Empty;
}

public class AnalyticsResponse
{
    public int TotalComments { get; set; }
    public int ProcessedComments { get; set; }
    public int TotalRules { get; set; }
    public int ActiveRules { get; set; }
    public int TotalExecutions { get; set; }
    public int SuccessfulExecutions { get; set; }
    public int FailedExecutions { get; set; }
    public double SuccessRate { get; set; }
    public List<DailyStats> DailyStats { get; set; } = new();
    public List<RuleStats> TopRules { get; set; } = new();
}

public class DailyStats
{
    public DateTime Date { get; set; }
    public int Comments { get; set; }
    public int Executions { get; set; }
    public int Successes { get; set; }
    public int Failures { get; set; }
}

public class RuleStats
{
    public int RuleId { get; set; }
    public string RuleName { get; set; } = string.Empty;
    public int ExecutionCount { get; set; }
    public int SuccessCount { get; set; }
    public double SuccessRate { get; set; }
}

