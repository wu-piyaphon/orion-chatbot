using System.Text;
using System.Text.Json;

namespace MetaChatbot;

/// <summary>Sends messages to users via the Meta Graph API (Facebook + Instagram).</summary>
public sealed class MetaMessagingService
{
    private readonly HttpClient _http;
    private readonly MetaConfig _config;
    private readonly ILogger<MetaMessagingService> _logger;

    public MetaMessagingService(
        IHttpClientFactory httpClientFactory,
        MetaConfig config,
        ILogger<MetaMessagingService> logger)
    {
        _http   = httpClientFactory.CreateClient("Meta");
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Sends a plain-text message to a Facebook or Instagram user via the Graph API.
    /// </summary>
    /// <param name="platform">"facebook" or "instagram" (case-insensitive).</param>
    /// <param name="recipientId">PSID (Facebook) or IGSID (Instagram) of the target user.</param>
    /// <param name="message">Text to deliver.</param>
    /// <returns>Tuple of (success flag, raw Meta response body).</returns>
    public async Task<(bool Success, string ResponseBody)> SendMessageAsync(
        string platform,
        string recipientId,
        string message)
    {
        string baseUrl;
        string accessToken;

        if (platform.Equals("facebook", StringComparison.OrdinalIgnoreCase))
        {
            baseUrl     = $"https://graph.facebook.com/{_config.ApiVersion}/{_config.Facebook.PageId}/messages";
            accessToken = _config.Facebook.PageAccessToken;
        }
        else if (platform.Equals("instagram", StringComparison.OrdinalIgnoreCase))
        {
            baseUrl     = $"https://graph.instagram.com/{_config.ApiVersion}/{_config.Instagram.IgId}/messages";
            accessToken = _config.Instagram.AccessToken;
        }
        else
        {
            _logger.LogWarning("Unknown platform requested: {Platform}", platform);
            return (false, $"Unknown platform: {platform}");
        }

        // Meta requires access_token as a query parameter for send-message calls.
        var requestUri = $"{baseUrl}?access_token={Uri.EscapeDataString(accessToken)}";

        var body = new
        {
            recipient      = new { id = recipientId },
            messaging_type = "RESPONSE",
            message        = new { text = message }
        };

        var json    = JsonSerializer.Serialize(body);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var response     = await _http.PostAsync(requestUri, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
                _logger.LogInformation("[{Platform}] Sent to {RecipientId}: \"{Message}\"", platform, recipientId, message);
            else
                _logger.LogWarning("[{Platform}] Send failed for {RecipientId} — HTTP {Status}: {Body}", platform, recipientId, (int)response.StatusCode, responseBody);

            return (response.IsSuccessStatusCode, responseBody);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{Platform}] HTTP error while sending to {RecipientId}", platform, recipientId);
            return (false, ex.Message);
        }
    }
}
