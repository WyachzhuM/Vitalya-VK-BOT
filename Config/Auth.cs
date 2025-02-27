using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using vkbot_vitalya.Core;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace vkbot_vitalya.Config;

public static class Auth {
    public static readonly Authentication Instance;

    static Auth() {
        const string authPath = "auth.json";
        if (!File.Exists(authPath)) {
            L.F($"{authPath} not found");
            throw new FileNotFoundException($"{authPath} not found");
        }

        Instance = Authentication.Load(authPath);
    }

    public class Authentication {
        public Authentication(
            string accessToken,
            ulong groupId,
            string memeGenApiKey,
            string weatherApiKey,
            string danbooruLogin,
            string danbooruApikey,
            string proxyAdress,
            string proxyLogin,
            string proxyPassword,
            string yandexApiKey
        ) {
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

        [JsonPropertyName("access_token")] public string AccessToken { get; init; }

        [JsonPropertyName("group_id")] public ulong GroupId { get; init; }

        [JsonPropertyName("memegen_apikey")] public string MemeGenApiKey { get; init; }

        [JsonPropertyName("weather_apikey")] public string WeatherApiKey { get; init; }

        [JsonPropertyName("danbooru_login")] public string DanbooruLogin { get; init; }

        [JsonPropertyName("danbooru_apikey")] public string DanbooruApikey { get; init; }

        [JsonPropertyName("proxy_adress")] public string ProxyAdress { get; init; }

        [JsonPropertyName("proxy_login")] public string ProxyLogin { get; init; }

        [JsonPropertyName("proxy_password")] public string ProxyPassword { get; init; }

        [JsonPropertyName("y_apikey")] public string YandexApiKey { get; init; }

        [JsonPropertyName("dev_key")] public string SystemPassKey { get; init; }

        public static Authentication Load(string path) {
            try {
                var json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<Authentication>(json)
                       ?? throw new Exception("Invalid auth file");
            } catch (Exception e) {
                throw new Exception("Failed to load auth file", e);
            }
        }
    }
}