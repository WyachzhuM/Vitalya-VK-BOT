using System.Text.Json;
using System.Text.Json.Serialization;

namespace vkbot_vitalya.Config;

public class AuthBotFile
{
    public AuthBotFile(string accessToken,
                       ulong groupId,
                       string memeGenApiKey,
                       string weatherApiKey,
                       string danbooruLogin,
                       string danbooruApikey,
                       string proxyAdress,
                       string proxyLogin,
                       string proxyPassword,
                       string yandexApiKey)
    {
        AccessToken = accessToken;
        GroupId = groupId;
        MemeGenApiKey = memeGenApiKey;
        WeatherApiKey = weatherApiKey;
        DanbooruLogin = danbooruLogin;
        DanbooruApikey = danbooruApikey;
        ProxyAdress = proxyAdress;
        ProxyLogin = proxyLogin;
        ProxyPassword = proxyPassword;
        YandexApiKey = yandexApiKey;
    }

    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; }
    [JsonPropertyName("group_id")]
    public ulong GroupId { get; set; }

    [JsonPropertyName("memegen_apikey")]
    public string MemeGenApiKey { get; set; }
    [JsonPropertyName("weather_apikey")]
    public string WeatherApiKey { get; set; }

    [JsonPropertyName("danbooru_login")]
    public string DanbooruLogin { get; set; }
    [JsonPropertyName("danbooru_apikey")]
    public string DanbooruApikey { get; set; }

    [JsonPropertyName("proxy_adress")]
    public string ProxyAdress { get; set; }
    [JsonPropertyName("proxy_login")]
    public string ProxyLogin { get; set; }
    [JsonPropertyName("proxy_password")]
    public string ProxyPassword { get; set; }
    [JsonPropertyName("y_apikey")]
    public string YandexApiKey { get; set; }
    [JsonPropertyName("dev_key")]
    public string SystemPassKey { get; set; }

    public static AuthBotFile GetAuthBotFileFromJson(string path)
    {
        string json = File.ReadAllText(path);

        var result = JsonSerializer.Deserialize<AuthBotFile>(json);
        if (result != null)
            return result;
        else
        {
            Console.WriteLine($"AuthBotFile is NULL!");
            return DefaultNULL();
        }
    }

    public void SaveToJson(string fileName, string filePath)
    {
        var json = JsonSerializer.Serialize(this);

        File.WriteAllText($"{filePath}/{fileName}.json", json);

        Console.WriteLine("AuthBotFile saved in" + $"{filePath}/{fileName}.json");
    }

    public static AuthBotFile DefaultNULL()
    {
        AuthBotFile auth = new AuthBotFile(string.Empty, 0, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);

        return auth;
    }
}