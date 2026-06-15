using System.Text;
using MetaChatbot;

// ---------------------------------------------------------------------------
// Builder
// ---------------------------------------------------------------------------

var builder = WebApplication.CreateBuilder(args);

// Bind "Meta" section → strongly-typed record
var metaConfig = builder.Configuration.GetSection("Meta").Get<MetaConfig>()
    ?? throw new InvalidOperationException("'Meta' configuration section is missing from appsettings.json.");

builder.Services.AddSingleton(metaConfig);

// Named HttpClient for all Graph API calls
builder.Services.AddHttpClient("Meta");

// Messaging service (singleton — HttpClient is obtained from factory, safe to share)
builder.Services.AddSingleton<MetaMessagingService>();

var app = builder.Build();

// ---------------------------------------------------------------------------
// GET /webhook  — Meta webhook verification handshake
// ---------------------------------------------------------------------------

app.MapGet("/webhook", (HttpRequest request, MetaConfig config) =>
{
    var mode        = request.Query["hub.mode"].ToString();
    var verifyToken = request.Query["hub.verify_token"].ToString();
    var challenge   = request.Query["hub.challenge"].ToString();

    if (mode == "subscribe" && verifyToken == config.VerifyToken)
    {
        app.Logger.LogInformation("Webhook verified successfully.");
        return Results.Content(challenge, "text/plain");
    }

    app.Logger.LogWarning("Webhook verification failed — mode={Mode}, token_match={Match}", mode, verifyToken == config.VerifyToken);
    return Results.StatusCode(403);
});

// ---------------------------------------------------------------------------
// POST /webhook  — Meta webhook event receiver
// ---------------------------------------------------------------------------

app.MapPost("/webhook", async (HttpRequest request, MetaConfig config) =>
{
    // EnableBuffering lets the framework buffer the body so we can read it as a
    // stream (for signature check) without preventing downstream middleware from
    // reading it again.
    request.EnableBuffering();

    byte[] bodyBytes;
    using (var ms = new MemoryStream())
    {
        await request.Body.CopyToAsync(ms);
        bodyBytes = ms.ToArray();
    }

    // Validate X-Hub-Signature-256 before touching the payload.
    var signature = request.Headers["X-Hub-Signature-256"].ToString();

    var signatureValid =
        (!string.IsNullOrEmpty(config.AppSecret)          && WebhookHandler.VerifySignature(config.AppSecret,           bodyBytes, signature)) ||
        (!string.IsNullOrEmpty(config.Instagram.AppSecret) && WebhookHandler.VerifySignature(config.Instagram.AppSecret, bodyBytes, signature));

    if (!signatureValid)
    {
        if (app.Logger.IsEnabled(LogLevel.Warning))
            app.Logger.LogWarning("Webhook signature validation failed — received: {Signature}", signature);
        return Results.Ok();
    }

    var bodyJson = Encoding.UTF8.GetString(bodyBytes);
    var payload  = WebhookHandler.ParseWebhookPayload(bodyJson);

    if (payload is null)
    {
        app.Logger.LogWarning("Failed to parse webhook payload: {Json}", bodyJson);
        return Results.Ok();
    }

    var platform = payload.Object switch
    {
        "page"      => "FACEBOOK",
        "instagram" => "INSTAGRAM",
        _           => payload.Object.ToUpperInvariant()
    };

    foreach (var entry in payload.Entry ?? [])
    {
        foreach (var evt in entry.Messaging ?? [])
        {
            var from = evt.Sender?.Id   ?? "(unknown)";
            var to   = evt.Recipient?.Id ?? "(unknown)";

            if (evt.Message is not null)
            {
                if (evt.Message.Text is not null)
                {
                    app.Logger.LogInformation(
                        "[{Platform}] From: {From} | To: {To} | Text: \"{Text}\"",
                        platform, from, to, evt.Message.Text);
                }

                if (evt.Message.Attachments is { Count: > 0 } attachments)
                {
                    foreach (var att in attachments)
                    {
                        app.Logger.LogInformation(
                            "[{Platform}] From: {From} | To: {To} | Attachment type: {Type}",
                            platform, from, to, att.Type);
                    }
                }
            }

            if (evt.Postback is not null)
            {
                app.Logger.LogInformation(
                    "[{Platform}] From: {From} | To: {To} | Postback: \"{PostbackPayload}\"",
                    platform, from, to, evt.Postback.Payload);
            }
        }
    }

    return Results.Ok();
});

// ---------------------------------------------------------------------------
// POST /api/send  — Internal API: send a reply to a specific user
// ---------------------------------------------------------------------------

app.MapPost("/api/send", async (SendRequest req, MetaMessagingService svc) =>
{
    if (string.IsNullOrWhiteSpace(req.Platform) ||
        string.IsNullOrWhiteSpace(req.RecipientId) ||
        string.IsNullOrWhiteSpace(req.Message))
    {
        return Results.BadRequest(new { error = "platform, recipientId, and message are all required." });
    }

    var (success, responseBody) = await svc.SendMessageAsync(req.Platform, req.RecipientId, req.Message);

    return success
        ? Results.Ok(new { success = true,  meta_response = responseBody })
        : Results.Json(new { success = false, error = responseBody }, statusCode: 502);
});

// ---------------------------------------------------------------------------
// GET /health  — Liveness probe
// ---------------------------------------------------------------------------

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

// ---------------------------------------------------------------------------
// GET /privacy  — Minimal privacy policy page (required for Meta Live mode)
// ---------------------------------------------------------------------------

app.MapGet("/privacy", () => Results.Content("""
    <html><body>
    <h1>Privacy Policy</h1>
    <p>This application receives and processes messages via the Meta Messaging API for development and testing purposes only.</p>
    <p>No personal data is stored. Messages are logged to the console for debugging only.</p>
    <p>Contact: piyaphon@greenmoons.co.th</p>
    </body></html>
    """, "text/html"));

// ---------------------------------------------------------------------------

app.Run();

// ---------------------------------------------------------------------------
// Request model for POST /api/send
// ---------------------------------------------------------------------------

/// <summary>Body accepted by the internal send endpoint.</summary>
record SendRequest(string Platform, string RecipientId, string Message);
