using System.Text.Json;
using InstagramAutomation.Api.Models;

namespace InstagramAutomation.Api.Services;

public interface IInstagramApiService
{
    Task<InstagramUserInfo?> GetUserInfoAsync(string accessToken);
    Task<bool> ValidateAccessTokenAsync(string accessToken);
    Task<bool> PostCommentReplyAsync(string accessToken, string commentId, string message);
    Task<bool> SendPrivateMessageAsync(string accessToken, string userId, string message);
    Task<List<InstagramMedia>> GetUserMediaAsync(string accessToken, int limit = 10);
}

public class InstagramApiService : IInstagramApiService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<InstagramApiService> _logger;
    private readonly string _baseUrl;
    private readonly string _apiVersion;

    public InstagramApiService(HttpClient httpClient, IConfiguration configuration, ILogger<InstagramApiService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _baseUrl = _configuration["Instagram:BaseUrl"] ?? "https://graph.facebook.com";
        _apiVersion = _configuration["Instagram:ApiVersion"] ?? "v21.0";
    }

    public async Task<InstagramUserInfo?> GetUserInfoAsync(string accessToken)
    {
        try
        {
            var url = $"{_baseUrl}/{_apiVersion}/me?fields=id,username,account_type,media_count,followers_count,follows_count&access_token={accessToken}";
            
            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Falha ao obter informações do usuário Instagram: {StatusCode}", response.StatusCode);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var userInfo = JsonSerializer.Deserialize<InstagramUserInfo>(content, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });

            return userInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter informações do usuário Instagram");
            return null;
        }
    }

    public async Task<bool> ValidateAccessTokenAsync(string accessToken)
    {
        try
        {
            var url = $"{_baseUrl}/{_apiVersion}/me?access_token={accessToken}";
            
            var response = await _httpClient.GetAsync(url);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao validar access token");
            return false;
        }
    }

    public async Task<bool> PostCommentReplyAsync(string accessToken, string commentId, string message)
    {
        try
        {
            var url = $"{_baseUrl}/{_apiVersion}/{commentId}/replies";
            
            var formData = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("message", message),
                new KeyValuePair<string, string>("access_token", accessToken)
            });

            var response = await _httpClient.PostAsync(url, formData);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Falha ao responder comentário: {StatusCode} - {Content}", response.StatusCode, errorContent);
                return false;
            }

            _logger.LogInformation("Resposta ao comentário enviada com sucesso: {CommentId}", commentId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao responder comentário: {CommentId}", commentId);
            return false;
        }
    }

    public async Task<bool> SendPrivateMessageAsync(string accessToken, string userId, string message)
    {
        try
        {
            // Nota: Esta funcionalidade requer permissões especiais do Instagram
            // e pode não estar disponível para todas as aplicações
            var url = $"{_baseUrl}/{_apiVersion}/me/messages";
            
            var payload = new
            {
                recipient = new { id = userId },
                message = new { text = message }
            };

            var jsonContent = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{url}?access_token={accessToken}", content);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Falha ao enviar mensagem privada: {StatusCode} - {Content}", response.StatusCode, errorContent);
                return false;
            }

            _logger.LogInformation("Mensagem privada enviada com sucesso para: {UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao enviar mensagem privada para: {UserId}", userId);
            return false;
        }
    }

    public async Task<List<InstagramMedia>> GetUserMediaAsync(string accessToken, int limit = 10)
    {
        try
        {
            var url = $"{_baseUrl}/{_apiVersion}/me/media?fields=id,media_type,media_url,permalink,timestamp,caption&limit={limit}&access_token={accessToken}";
            
            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Falha ao obter mídia do usuário: {StatusCode}", response.StatusCode);
                return new List<InstagramMedia>();
            }

            var content = await response.Content.ReadAsStringAsync();
            var mediaResponse = JsonSerializer.Deserialize<InstagramMediaResponse>(content, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });

            return mediaResponse?.Data ?? new List<InstagramMedia>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter mídia do usuário");
            return new List<InstagramMedia>();
        }
    }
}

public class InstagramUserInfo
{
    public string Id { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string AccountType { get; set; } = string.Empty;
    public int MediaCount { get; set; }
    public int FollowersCount { get; set; }
    public int FollowsCount { get; set; }
}

public class InstagramMedia
{
    public string Id { get; set; } = string.Empty;
    public string MediaType { get; set; } = string.Empty;
    public string? MediaUrl { get; set; }
    public string? Permalink { get; set; }
    public DateTime Timestamp { get; set; }
    public string? Caption { get; set; }
}

public class InstagramMediaResponse
{
    public List<InstagramMedia> Data { get; set; } = new();
    public InstagramPaging? Paging { get; set; }
}

public class InstagramPaging
{
    public string? Next { get; set; }
    public string? Previous { get; set; }
}

