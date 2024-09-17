using System.Text.Json;
using System.Text.Json.Serialization;
using VkNet.Model;

namespace vkbot_vitalya.Core;

public class ChatMessage
{
    public ChatMessage(long? fromId, string? text, DateTime? date, long? conversationMessageId)
    {
        FromId = fromId ?? 0;
        Text = text ?? "";
        Date = date ?? DateTime.Now;
        ConversationMessageId = conversationMessageId ?? 0;
    }

    [JsonPropertyName("from_id")]
    public long? FromId { get; set; }

    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("date")]
    public DateTime? Date { get; set; }

    [JsonPropertyName("conversation_message_id")]
    public long? ConversationMessageId { get; set; }

    private static readonly ChatMessage chatMessage = new ChatMessage(0, "ahhh", DateTime.Now, 0);
    public static ChatMessage Default = chatMessage;
}

/// <summary>
/// Надо потом сделать загрузку сообщенйи в оперативу, но это тестить надо
/// </summary>
public class ChatMessages
{
    private const int MAX_MESSAGES = 3000;

    public ChatMessages(long? peerID, List<ChatMessage> messages)
    {
        PeerID = peerID ?? 0;
        Messages = messages;
    }

    public ChatMessages()
    {
    }

    [JsonPropertyName("peer_id")]
    public long? PeerID { get; set; }

    [JsonPropertyName("messages")]
    public List<ChatMessage> Messages { get; set; }

    /// <summary>
    /// Provided that <a cref="ChatMessages"/> exists returns Bool
    /// </summary>
    public static implicit operator bool(ChatMessages? exists) => exists != null;

    #region Save Block
    public static string GetFileName(string? peerId)
    {
        if (peerId != null)
            return $"Chat{peerId}.json";
        else return $"Chat_default.json";
    }

    public static async Task<ChatMessages> Deserialize(string fullpath)
    {
        if (!File.Exists(fullpath))
        {
            Logger.M($"{nameof(ChatMessages)}: File {fullpath} does not exist. Creating new ChatMessages instance.");
            return new ChatMessages();
        }

        string content = await File.ReadAllTextAsync(fullpath);

        var result = JsonSerializer.Deserialize<ChatMessages>(content);

        if (result != null)
            return result;

        Logger.M($"{nameof(ChatMessages)}: JsonSerializer.Deserialize<ChatMessages>(content) == NULL");

        return new ChatMessages();
    }

    private string Serialize() => JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });

    public async Task Update(string filePath, Message? message)
    {
        if (message == null)
        {
            Logger.M($"{nameof(ChatMessages)}: message is NULL");
            return;
        }

        string fullpath = Path.Combine(filePath, GetFileName(message.PeerId.ToString()));
        Logger.M($"{nameof(ChatMessages)}: Trying to read file from {fullpath}");

        var chatMessages = await Deserialize(fullpath);

        if (chatMessages == null)
        {
            Logger.M($"{nameof(ChatMessages)}: Retrieved chatMessages is NULL");
            return;
        }

        Logger.M($"{nameof(ChatMessages)}: Deserialized chatMessages successfully");

        ChatMessage message1 = new ChatMessage(message.FromId, message.Text, message.Date, message.ConversationMessageId);

        if (chatMessages.Messages.Count >= MAX_MESSAGES)
        {
            chatMessages.Messages.RemoveAt(0); // Remove the oldest message
        }

        chatMessages.Messages.Add(message1);

        try
        {
            string json = JsonSerializer.Serialize(chatMessages, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(fullpath, json);
            Logger.M($"{nameof(ChatMessages)}: Updated and wrote to file successfully");
        }
        catch (Exception ex)
        {
            Logger.M($"{nameof(ChatMessages)}: Error serializing or writing to file: {ex.Message}");
        }
    }

    public async Task Save(string filePath, Message? message)
    {
        if (message?.PeerId == null)
        {
            Logger.M($"{nameof(ChatMessages)}: message?.PeerId is NULL");
            return;
        }

        ChatMessage message1 = new ChatMessage(message.FromId, message.Text, message.Date, message.ConversationMessageId);

        ChatMessages cm = new ChatMessages(message.PeerId, new List<ChatMessage>() { message1 });

        await Save(filePath, cm);
    }

    private async Task Save(string filePath, ChatMessages chat)
    {
        string json = JsonSerializer.Serialize(chat, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(Path.Combine(filePath, GetFileName(chat.PeerID.ToString())), json);
    }
    #endregion

    public static async Task<List<ChatMessage>> GetMessagesFromUser(long? peerId, long? fromId)
    {
        string fullpath = Path.Combine(Program._savedMessagesFolder, GetFileName(peerId.ToString()));

        ChatMessages chatmessages = await Deserialize(fullpath);

        if (chatmessages.Messages.Count != 0)
            return chatmessages.Messages.FindAll(x => x.FromId == fromId);
        else
        {
            var messages = new List<ChatMessage>()
            {
                ChatMessage.Default
            };

            return messages;
        }
    }

    public static async Task<List<ChatMessage>> GetMessages(long? peerId)
    {
        string fullpath = Path.Combine(Program._savedMessagesFolder, GetFileName(peerId.ToString()));

        ChatMessages chatmessages = await Deserialize(fullpath);

        if (chatmessages.Messages.Count != 0)
            return chatmessages.Messages;
        else
        {
            var messages = new List<ChatMessage>()
            {
                ChatMessage.Default
            };

            return messages;
        }
    }

    public override string ToString()
    {
        return $"{nameof(ChatMessages)}: Chat {PeerID} has {Messages.Count} messages!";
    }
}