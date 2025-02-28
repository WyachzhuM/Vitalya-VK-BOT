using System.Text.Json;
using System.Text.Json.Serialization;
using vkbot_vitalya.Core;

namespace vkbot_vitalya.Config;

public class Saves {
    private const string SavesFilePath = "./saves.json";
    public Saves(List<Chat> chats) {
        Chats = chats;
    }

    [JsonPropertyName("chats")]
    public List<Chat> Chats { get; set; }

    public static Saves Load() {
        if (!File.Exists(SavesFilePath))
            return new Saves(new List<Chat>());

        var jsonString = File.ReadAllText(SavesFilePath);
        var saves = JsonSerializer.Deserialize<Saves>(jsonString);
        return saves ?? new Saves(new List<Chat>());
    }

    public void Save() {
        var jsonString = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(SavesFilePath, jsonString);
    }

    public void AddChat(long peerId) {
        if (Chats.All(chat => chat.PeerId != peerId)) {
            Chats.Add(new Chat(peerId, new ChatProperties(), new List<User>()));
        }
    }

    public void AddUserToChat(long peerId, long userId) {
        var chat = Chats.FirstOrDefault(chat => chat.PeerId == peerId);
        if (chat != null && chat.Users.All(user => user.Id != userId)) {
            chat.Users.Add(new User(userId));
        }
    }

    public Chat GetChat(long peerId) {
        foreach (var chat in Chats) {
            if (chat.PeerId == peerId) {
                return chat;
            }
        }

        throw new Exception("Attempted to get chat before saving it");
    }
}

public class Chat {
    public Chat(long peerId, ChatProperties? properties = null, List<User>? users = null) {
        PeerId = peerId;
        Properties = properties ?? new ChatProperties();
        Users = users ?? [];
    }

    [JsonPropertyName("peer_id")]
    public long PeerId { get; set; }

    [JsonPropertyName("properties")]
    public ChatProperties Properties { get; set; }

    [JsonPropertyName("users")]
    public List<User> Users { get; set; }
}

public class ChatProperties {
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
    
    [JsonPropertyName("response_probability")]
    public double ResponseProbability { get; set; } = 0.2;

    public ChatProperties() { }
}

public record User(long Id) {
    [JsonPropertyName("id")]
    public long Id { get; set; } = Id;
}