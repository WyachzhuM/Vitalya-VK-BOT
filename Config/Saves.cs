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

    public void AddUserToChat(long peerId, User user) {
        var chat = Chats.FirstOrDefault(chat => chat.PeerId == peerId);
        if (chat != null && chat.Users.All(u => u.Id != user.Id)) {
            chat.Users.Add(user);
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

    [JsonPropertyName("peer_id")] public long PeerId { get; set; }

    [JsonPropertyName("properties")] public ChatProperties Properties { get; set; }

    [JsonPropertyName("users")] public List<User> Users { get; set; }
}

public class ChatProperties {
    [JsonPropertyName("anime")] public bool IsAnime { get; set; } = true;

    [JsonPropertyName("hentai")] public bool IsHentai { get; set; } = true;

    [JsonPropertyName("images")] public bool IsImageProccestion { get; set; } = true;

    [JsonPropertyName("meme")] public bool IsMeme { get; set; } = true;

    [JsonPropertyName("weather")] public bool IsWeather { get; set; } = true;

    [JsonPropertyName("location")] public bool IsLocation { get; set; } = true;

    [JsonPropertyName("response_probability")] public double ResponseProbability { get; set; } = 0.2;

    public ChatProperties() {
    }
}

public record User(
    long Id,
    string FirstName,
    string LastName,
    string FirstNameNom,
    string FirstNameGen,
    string FirstNameDat,
    string FirstNameAcc,
    string FirstNameIns,
    string FirstNameAbl,
    string LastNameNom,
    string LastNameGen,
    string LastNameDat,
    string LastNameAcc,
    string LastNameIns,
    string LastNameAbl) {
    [JsonPropertyName("id")] public long Id { get; set; } = Id;

    [JsonPropertyName("first_name")] public string FirstName { get; set; } = FirstName;

    [JsonPropertyName("last_name")] public string LastName { get; set; } = LastName;

    [JsonPropertyName("first_name_nom")] public string FirstNameNom { get; set; } = FirstNameNom;

    [JsonPropertyName("first_name_gen")] public string FirstNameGen { get; set; } = FirstNameGen;

    [JsonPropertyName("first_name_dat")] public string FirstNameDat { get; set; } = FirstNameDat;

    [JsonPropertyName("first_name_acc")] public string FirstNameAcc { get; set; } = FirstNameAcc;

    [JsonPropertyName("first_name_ins")] public string FirstNameIns { get; set; } = FirstNameIns;

    [JsonPropertyName("first_name_abl")] public string FirstNameAbl { get; set; } = FirstNameAbl;

    [JsonPropertyName("last_name_nom")] public string LastNameNom { get; set; } = LastNameNom;

    [JsonPropertyName("last_name_gen")] public string LastNameGen { get; set; } = LastNameGen;

    [JsonPropertyName("last_name_dat")] public string LastNameDat { get; set; } = LastNameDat;

    [JsonPropertyName("last_name_acc")] public string LastNameAcc { get; set; } = LastNameAcc;

    [JsonPropertyName("last_name_ins")] public string LastNameIns { get; set; } = LastNameIns;

    [JsonPropertyName("last_name_abl")] public string LastNameAbl { get; set; } = LastNameAbl;

    public override string ToString() {
        return FirstName + " " + LastName;
    }
}