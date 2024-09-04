using System.Text.Json;
using System.Text.Json.Serialization;

namespace VkBot
{
    public class AuthBotFile
    {
        public AuthBotFile()
        {
        }

        public AuthBotFile(string access_token, ulong group_id)
        {
            AccessToken = access_token;
            GroupId = group_id;
        }

        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; }
        [JsonPropertyName("group_id")]
        public ulong GroupId { get; set; }

        public static AuthBotFile? GetAuthBotFileFromJson(string path)
        {
            string json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<AuthBotFile>(json);
        }

        public void SaveToJson(string fileName, string filePath)
        {
            var json = JsonSerializer.Serialize(this);

            File.WriteAllText($"{filePath}/{fileName}.json", json);

            Console.WriteLine("AuthBotFile saved in" + $"{filePath}/{fileName}.json");
        }
    }
}