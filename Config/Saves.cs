using System.Text.Json;
using System.Text.Json.Serialization;

namespace vkbot_vitalya.Config;

public class Saves
{
    public Saves(List<Chat> chats)
    {
        Chats = chats;
    }

    [JsonPropertyName("chats")]
    public List<Chat> Chats { get; set; }

    public static Saves Load(string filePath)
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
            Chats.Add(new Chat(peerId, new List<User>()));
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
    public Chat(long peerID, List<User> users)
    {
        PeerID = peerID;
        Users = users;
    }

    [JsonPropertyName("chat_peer_id")]
    public long PeerID { get; set; }

    [JsonPropertyName("users")]
    public List<User> Users { get; set; }
}

public class User
{
    public User(long userID)
    {
        UserID = userID;
    }

    [JsonPropertyName("user_id")]
    public long UserID { get; set; }
}
