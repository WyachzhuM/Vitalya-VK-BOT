using System.Text.Json;
using System.Text.Json.Serialization;
using VkNet.Model;

namespace vkbot_vitalya.Core;

public class ChatMessage {
    public ChatMessage(long? fromId, string? text, DateTime? date, long? conversationMessageId) {
        FromId = fromId ?? 0;
        Text = text ?? "";
        Date = date ?? DateTime.Now;
        ConversationMessageId = conversationMessageId ?? 0;
    }

    [JsonPropertyName("from_id")] public long? FromId { get; set; }

    [JsonPropertyName("text")] public string? Text { get; set; }

    [JsonPropertyName("date")] public DateTime? Date { get; set; }

    [JsonPropertyName("conversation_message_id")]
    public long? ConversationMessageId { get; set; }

    private static readonly ChatMessage chatMessage = new ChatMessage(0, "ahhh", DateTime.Now, 0);
    public static ChatMessage Default = chatMessage;
}

/// <summary>
/// Надо потом сделать загрузку сообщенйи в оперативу, но это тестить надо
/// </summary>
public class ChatMessages {
    private const int MAX_MESSAGES = 3000;

    public ChatMessages(long? peerID, List<ChatMessage> messages) {
        PeerID = peerID ?? 0;
        Messages = messages;
    }

    public ChatMessages() {
    }

    [JsonPropertyName("peer_id")] public long? PeerID { get; set; }

    [JsonPropertyName("messages")] public List<ChatMessage> Messages { get; set; }

    /// <summary>
    /// Provided that <a cref="ChatMessages"/> exists returns Bool
    /// </summary>
    public static implicit operator bool(ChatMessages? exists) => exists != null;

    #region Save Block

    public static string GetFileName(string peerId) {
        return $"Chat{peerId}.json";
    }

    public static async Task<ChatMessages> Deserialize(string fullpath) {
        if (!File.Exists(fullpath)) {
            L.W($"File {fullpath} does not exist. Creating new ChatMessages instance.");
            return new ChatMessages();
        }

        var content = await File.ReadAllTextAsync(fullpath);

        var result = JsonSerializer.Deserialize<ChatMessages>(content);

        if (result == null) {
            L.E($"Failed to deserialize ChatMessages ({fullpath})");
            return new ChatMessages();
        }

        return result;
    }

    private string Serialize() => JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });

    public async Task Update(string filePath, Message? message) {
        if (message == null) {
            L.D($"{nameof(ChatMessages)}: message is NULL");
            return;
        }

        var fullpath = Path.Combine(filePath, GetFileName(message.PeerId.ToString()));
        L.D($"{nameof(ChatMessages)}: Trying to read file from {fullpath}");

        var chatMessages = await Deserialize(fullpath);

        L.D($"{nameof(ChatMessages)}: Deserialized chatMessages successfully");

        var message1 = new ChatMessage(message.FromId, message.Text, message.Date, message.ConversationMessageId);

        if (chatMessages.Messages.Count >= MAX_MESSAGES) {
            chatMessages.Messages.RemoveAt(0); // Remove the oldest message
        }

        chatMessages.Messages.Add(message1);

        try {
            var json = JsonSerializer.Serialize(chatMessages, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(fullpath, json);
            L.D($"{nameof(ChatMessages)}: Updated and wrote to file successfully");
        } catch (Exception ex) {
            L.E($"{nameof(ChatMessages)}: Error serializing or writing to file: {ex.Message}");
        }
    }

    public async Task Save(string filePath, Message message) {
        var message1 = new ChatMessage(message.FromId, message.Text, message.Date, message.ConversationMessageId);

        var cm = new ChatMessages(message.PeerId, [message1]);

        await Save(filePath, cm);
    }

    private async Task Save(string filePath, ChatMessages chat) {
        var json = JsonSerializer.Serialize(chat, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(Path.Combine(filePath, GetFileName(chat.PeerID.ToString())), json);
    }

    #endregion

    public static async Task<List<ChatMessage>> GetMessagesFromUser(long? peerId, long? fromId) {
        var fullpath = Path.Combine(Program._savedMessagesFolder, GetFileName(peerId.ToString()));

        var chatmessages = await Deserialize(fullpath);

        if (chatmessages.Messages.Count != 0)
            return chatmessages.Messages.FindAll(x => x.FromId == fromId);
        var messages = new List<ChatMessage>() {
            ChatMessage.Default
        };

        return messages;
    }

    public static async Task<List<ChatMessage>> GetMessages(long? peerId) {
        var fullpath = Path.Combine(Program._savedMessagesFolder, GetFileName(peerId.ToString()));

        var chatmessages = await Deserialize(fullpath);

        if (chatmessages.Messages.Count != 0)
            return chatmessages.Messages;
        var messages = new List<ChatMessage>() {
            ChatMessage.Default
        };

        return messages;
    }

    public override string ToString() {
        return $"{nameof(ChatMessages)}: Chat {PeerID} has {Messages.Count} messages!";
    }
}