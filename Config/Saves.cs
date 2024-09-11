using System.Text.Json;
using System.Text.Json.Serialization;

namespace vkbot_vitalya.Config;

public class Saves
{
    public Saves(IEnumerable<Chat> chats)
    {
        Chats = chats;
    }

    [JsonPropertyName("chats")]
    public IEnumerable<Chat> Chats { get; set; }

    public static Saves? Load(string filePath)
    {
        if (!File.Exists(filePath))
            return new Saves(new List<Chat>());

        var jsonString = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<Saves>(jsonString);
    }

    public void Save(string filePath)
    {
        var jsonString = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(filePath, jsonString);
    }

    public void AddChat(long peerId)
    {
        if (!Chats.Any(chat => chat.PeerID == peerId))
        {
            Chats.ToList().Add(new Chat(peerId, new ChatPropertyes(), new List<User>()));
        }
    }

    public void AddUserToChat(long peerId, long userId)
    {
        var chat = Chats.FirstOrDefault(chat => chat.PeerID == peerId);
        if (chat != null && !chat.Users.Any(user => user.UserID == userId))
        {
            chat.Users.Add(new User(userId));
        }
    }
}

public class Chat
{
    public Chat(long peerID, ChatPropertyes propertyes = null, List<User> users = null)
    {
        PeerID = peerID;
        Propertyes = propertyes ?? new ChatPropertyes();
        Users = users ?? new List<User>();
    }

    [JsonPropertyName("chat_peer_id")]
    public long PeerID { get; set; }

    [JsonPropertyName("property")]
    public ChatPropertyes Propertyes { get; set; }

    [JsonPropertyName("users")]
    public List<User> Users { get; set; }
}

public class ChatPropertyes
{
    [JsonPropertyName("anime")]
    public bool IsAnime { get; set; } = true;

    [JsonPropertyName("hentai")]
    public bool IsHentai { get; set; } = true;

    [JsonPropertyName("images")]
    public bool IsImageProccestion { get; set; } = true;

    [JsonPropertyName("meme")]
    public bool IsMeme { get; set; } = true;

    [JsonPropertyName("weather")]
    public bool IsWeather { get; set; } = true;

    [JsonPropertyName("location")]
    public bool IsLocation { get; set; } = true;

    public ChatPropertyes() { }
}

public record class User(long UserID)
{
    [JsonPropertyName("user_id")]
    public long UserID { get; set; } = UserID;
}