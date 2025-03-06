using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using vkbot_vitalya.Config;
using vkbot_vitalya.Core;
using vkbot_vitalya.Core.Requesters;

namespace vkbot_vitalya.Services;

public class SafebooruApi {
    private const string MasterUrl = "https://safebooru.org/";
    private static readonly Random Rand = new();

    private readonly Dictionary<string, List<SafebooruPost>> cache = new Dictionary<string, List<SafebooruPost>>();

    private readonly List<string> forbiddenTags = [
        "futanari", "gay", "furry", "penis", "testicles", "huge penis", "erection", "inflation",
        "loli", "child on child", "yaoi", "2boys", "nazi", "trap", "succubus", "corpse",
        "coprophilic", "cunt", "multiple boys", "yaoi", "2boys", "multiple_boys", "male_penetrated",
        "bara", "male_focus", "muscular_male", "cum_on_male", "rotten", "coprophagia",
        "scat", "diarrhea", "poop", "squat toilet", "pee", "toilet use", "guro", "ero guro",
        "vomit", "fart", "tentacles", "peeing", "personality excrement", "defecating",
        "enema", "execution", "hazbin_hotel", "helluva_boss", "loona"
    ];

    public SafebooruApi() {
        Client = ProxyClient.GetProxyHttpClient(Auth.Instance.ProxyAdress,
            new NetworkCredential(Auth.Instance.ProxyLogin, Auth.Instance.ProxyPassword), "vk-bot-vitalya");
    }

    public HttpClient Client { get; set; }

    public async Task<SafebooruPost?> GetRandomPostAsync(string tags) {
        // Кэширование результатов
        if (cache.ContainsKey(tags) && cache[tags].Count > 0) {
            var cachedPosts = cache[tags];
            var randomIndex = Rand.Next(cachedPosts.Count);
            return cachedPosts[randomIndex];
        }

        var postCount = 5; //await GetPostCountAsync(tags);
        if (postCount == 0) {
            L.I("No posts found.");
            return null;
        }

        var incTags = tags.Replace(",", " ");
        var randomPage = Rand.Next(0, (postCount - 1) / 200 + 1);

        if (forbiddenTags.Contains(incTags) && !Program.IgnoreTagsBlacklist) {
            return null;
        }

        var url = MasterUrl + $"index.php?page=dapi&s=post&q=index&limit=200&json=1&pid={randomPage}&tags={incTags}";

        // Задержка перед выполнением запроса
        await Task.Delay(1000);
        HttpResponseMessage response;
        try {
            response = await Client.GetAsync(url);
        } catch (HttpRequestException e) {
            L.E("", e);
            return null;
        }

        L.I($"Response Status Code: {response.StatusCode}");

        if (response.IsSuccessStatusCode) {
            var responseBody = await response.Content.ReadAsStringAsync();

            // Improved check for search error
            if (!string.IsNullOrWhiteSpace(responseBody) && !responseBody.Contains("search error")) {
                try {
                    var posts = JsonSerializer.Deserialize<List<SafebooruPost>>(responseBody);

                    if (posts != null && posts.Count > 0) {
                        cache[tags] = posts; // Кэшируем результаты
                        L.I($"Posts found: {posts.Count}");

                        var randomIndex = Rand.Next(posts.Count);
                        return posts[randomIndex];
                    }

                    L.I("No posts found.");
                } catch (JsonException jsonEx) {
                    L.I($"JSON deserialization error: {jsonEx.Message}");
                }
            } else {
                L.I("Search error detected or response body is empty.");
            }
        } else {
            var errorContent = await response.Content.ReadAsStringAsync();
            L.I($"Error Content: {errorContent}");
        }

        return null;
    }

    public async Task<int> GetPostCountAsync(string tags) {
        var incTags = tags.Replace(",", " ");
        var url = MasterUrl + $"index.php?page=dapi&s=post&q=index&limit=1&json=1&tags={incTags}";

        // Задержка перед выполнением запроса
        await Task.Delay(1000);
        HttpResponseMessage response;
        try {
            response = await Client.GetAsync(url);
        } catch (HttpRequestException e) {
            L.E("", e);
            return 0;
        }

        if (response.IsSuccessStatusCode) {
            var responseBody = await response.Content.ReadAsStringAsync();
            var posts = JsonSerializer.Deserialize<List<SafebooruPost>>(responseBody);
            if (posts != null && posts.Count > 0) {
                return posts[0].ID;
            }
        }

        return 0;
    }
}

public class SafebooruPost {
    public SafebooruPost(string previewUrl, string sampleUrl, string fileUrl, int directoryNum, string hash, int width,
        int height, int iD, string image) {
        PreviewUrl = previewUrl;
        SampleUrl = sampleUrl;
        FileUrl = fileUrl;
        DirectoryNum = directoryNum;
        Hash = hash;
        Width = width;
        Height = height;
        ID = iD;
        Image = image;
    }

    [JsonPropertyName("preview_url")] public string PreviewUrl { get; set; }
    [JsonPropertyName("sample_url")] public string SampleUrl { get; set; }
    [JsonPropertyName("file_url")] public string FileUrl { get; set; }
    [JsonPropertyName("directory")] public int DirectoryNum { get; set; }
    [JsonPropertyName("hash")] public string Hash { get; set; }
    [JsonPropertyName("width")] public int Width { get; set; }
    [JsonPropertyName("height")] public int Height { get; set; }
    [JsonPropertyName("id")] public int ID { get; set; }
    [JsonPropertyName("image")] public string Image { get; set; }
}