using System.Text.Json.Serialization;

namespace MetaChatbot.Models;

/// <summary>Top-level envelope sent by Meta for every webhook event.</summary>
public record WebhookPayload(
    [property: JsonPropertyName("object")] string Object,
    [property: JsonPropertyName("entry")]  List<WebhookEntry> Entry
);

/// <summary>One entry in the payload — corresponds to a page/IG account.</summary>
public record WebhookEntry(
    [property: JsonPropertyName("id")]        string Id,
    [property: JsonPropertyName("time")]      long   Time,
    [property: JsonPropertyName("messaging")] List<MessagingEvent>? Messaging
);

/// <summary>A single message/postback event inside an entry.</summary>
public record MessagingEvent(
    [property: JsonPropertyName("sender")]    Participant      Sender,
    [property: JsonPropertyName("recipient")] Participant      Recipient,
    [property: JsonPropertyName("timestamp")] long             Timestamp,
    [property: JsonPropertyName("message")]   MessagePayload?  Message,
    [property: JsonPropertyName("postback")]  PostbackPayload? Postback
);

/// <summary>Sender or recipient with their platform-scoped ID.</summary>
public record Participant(
    [property: JsonPropertyName("id")] string Id
);

/// <summary>The message object (text and/or attachments).</summary>
public record MessagePayload(
    [property: JsonPropertyName("mid")]         string           Mid,
    [property: JsonPropertyName("text")]        string?          Text,
    [property: JsonPropertyName("attachments")] List<Attachment>? Attachments
);

/// <summary>An attachment entry (image, audio, video, file, etc.).</summary>
public record Attachment(
    [property: JsonPropertyName("type")]    string            Type,
    [property: JsonPropertyName("payload")] AttachmentDetail? Payload
);

/// <summary>Attachment detail — URL is present for most media types.</summary>
public record AttachmentDetail(
    [property: JsonPropertyName("url")] string? Url
);

/// <summary>Postback from a button/persistent-menu tap.</summary>
public record PostbackPayload(
    [property: JsonPropertyName("title")]   string? Title,
    [property: JsonPropertyName("payload")] string? Payload
);
