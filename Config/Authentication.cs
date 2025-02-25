using System.Text.Json;
using System.Text.Json.Serialization;
using vkbot_vitalya.Core;

namespace vkbot_vitalya.Config;

public class Authentication
{
    public Authentication(string accessToken, ulong groupId, string memeGenApiKey, string weatherApiKey,
                       string danbooruLogin, string danbooruApikey, string proxyAdress,
                       string proxyLogin, string proxyPassword, string yandexApiKey)
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

    public static Authentication GetAuthBotFileFromJson(string path)
    {
        try
        {
            string json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<Authentication>(json) ?? Default();
        }
        catch (Exception ex)
        {
            L.E("Error reading AuthBotFile", ex);
            return Default();
        }
    }

    public void SaveToJson(string fileName, string filePath)
    {
        try
        {
            var json = JsonSerializer.Serialize(this);
            var fullPath = Path.Combine(filePath, $"{fileName}.json");
            File.WriteAllText(fullPath, json);
            L.M($"AuthBotFile saved in {fullPath}");
        }
        catch (Exception ex)
        {
            L.M($"Error saving AuthBotFile: {ex.Message}");
        }
    }

    public static Authentication Default()
    {
        return new Authentication(string.Empty, 0, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
    }
}