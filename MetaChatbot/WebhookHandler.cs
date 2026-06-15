using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MetaChatbot.Models;

namespace MetaChatbot;

/// <summary>Stateless helpers for webhook signature verification and payload parsing.</summary>
public static class WebhookHandler
{
    /// <summary>
    /// Validates the X-Hub-Signature-256 header against HMAC-SHA256 of the raw request body.
    /// Uses constant-time comparison to prevent timing attacks.
    /// </summary>
    /// <param name="appSecret">Meta App Secret from configuration.</param>
    /// <param name="bodyBytes">Raw request body bytes (must be read before any model binding).</param>
    /// <param name="signatureHeader">Value of the X-Hub-Signature-256 header, e.g. "sha256=abcdef...".</param>
    /// <returns>True if the signature is valid; false otherwise.</returns>
    public static bool VerifySignature(string appSecret, byte[] bodyBytes, string signatureHeader)
    {
        if (string.IsNullOrWhiteSpace(signatureHeader) || !signatureHeader.StartsWith("sha256=", StringComparison.OrdinalIgnoreCase))
            return false;

        var expectedHex = signatureHeader["sha256=".Length..].ToLowerInvariant();

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(appSecret));
        var computedHex = Convert.ToHexString(hmac.ComputeHash(bodyBytes)).ToLowerInvariant();

        // Constant-time comparison prevents timing-based secret extraction.
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computedHex),
            Encoding.UTF8.GetBytes(expectedHex)
        );
    }

    /// <summary>Computes the expected HMAC-SHA256 signature for a given body. Used for debugging.</summary>
    public static string ComputeSignature(string appSecret, byte[] bodyBytes)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(appSecret));
        return "sha256=" + Convert.ToHexString(hmac.ComputeHash(bodyBytes)).ToLowerInvariant();
    }

    /// <summary>
    /// Deserializes the raw Meta webhook JSON into a <see cref="WebhookPayload"/>.
    /// Returns null if the JSON is malformed or missing required fields.
    /// </summary>
    /// <param name="json">UTF-8 decoded request body.</param>
    public static WebhookPayload? ParseWebhookPayload(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<WebhookPayload>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
}
