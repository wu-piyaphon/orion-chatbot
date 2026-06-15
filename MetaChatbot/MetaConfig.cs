namespace MetaChatbot;

/// <summary>Root configuration bound from the "Meta" section of appsettings.json.</summary>
public record MetaConfig
{
    /// <summary>Token you set in the Meta App Dashboard → Webhooks → Verify Token.</summary>
    public string VerifyToken { get; init; } = string.Empty;

    /// <summary>App Secret from Meta App Dashboard → Settings → Basic. Used for HMAC-SHA256 signature validation.</summary>
    public string AppSecret { get; init; } = string.Empty;

    /// <summary>Graph API version, e.g. "v21.0".</summary>
    public string ApiVersion { get; init; } = "v21.0";

    public FacebookConfig Facebook { get; init; } = new();
    public InstagramConfig Instagram { get; init; } = new();
}

/// <summary>Facebook Page-specific settings.</summary>
public record FacebookConfig
{
    /// <summary>Numeric Facebook Page ID.</summary>
    public string PageId { get; init; } = string.Empty;

    /// <summary>Page Access Token with pages_messaging permission.</summary>
    public string PageAccessToken { get; init; } = string.Empty;
}

/// <summary>Instagram Professional Account-specific settings.</summary>
public record InstagramConfig
{
    /// <summary>Numeric Instagram-scoped Business Account ID (IGSID).</summary>
    public string IgId { get; init; } = string.Empty;

    /// <summary>Access Token with instagram_manage_messages permission.</summary>
    public string AccessToken { get; init; } = string.Empty;
}
