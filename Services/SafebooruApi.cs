using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using VkNet.Utils;
using vkbot_vitalya.Config;
using vkbot_vitalya.Core;
using VkNet.Model;
using vkbot_vitalya.Core.Requesters;

namespace vkbot_vitalya.Services;

public class SafebooruApi
{
    private readonly string BASE_URL = "https://safebooru.org/";

    private readonly Dictionary<string, List<SafebooruPost>> cache = new Dictionary<string, List<SafebooruPost>>();

    private readonly List<string> forbiddenTags = new List<string>()
    {
        "futanari", "gay", "furry", "penis", "testicles", "huge penis", "erection", "inflation",
        "loli", "child on child", "yaoi", "2boys", "nazi", "trap", "succubus", "corpse",
        "coprophilic", "cunt", "multiple boys", "yaoi", "2boys", "multiple_boys", "male_penetrated",
        "bara", "male_focus", "muscular_male", "cum_on_male", "rotten", "coprophagia",
        "scat", "diarrhea", "poop", "squat toilet", "pee", "toilet use", "guro", "ero guro",
        "vomit", "fart", "tentacles", "peeing", "personality excrement", "defecating",
        "enema", "execution", "hazbin_hotel", "helluva_boss", "loona"
    };

    public SafebooruApi(Authentication auth)
    {
        Client = ProxyClient.GetProxyHttpClient(auth.ProxyAdress, new NetworkCredential(auth.ProxyLogin, auth.ProxyPassword), "vk-bot-vitalya");
    }

    private HttpClient Client { get; set; }

    public async Task<SafebooruPost> GetRandomPostAsync(string tags)
    {
        // Кэширование результатов
        if (cache.ContainsKey(tags) && cache[tags].Count > 0)
        {
            var cachedPosts = cache[tags];
            Random random = new Random();
            int randomIndex = random.Next(cachedPosts.Count);
            return cachedPosts[randomIndex];
        }

        int postCount = 5;//await GetPostCountAsync(tags);
        if (postCount == 0)
        {
            L.M("No posts found.");
            return null;
        }

        Random rnd = new Random();
        string incTags = tags.Replace(",", " ");
        int randomPage = rnd.Next(0, (postCount - 1) / 200 + 1);

        if (forbiddenTags.Contains(incTags))
        {
            return null;
        }

        string url = BASE_URL + $"index.php?page=dapi&s=post&q=index&limit=200&json=1&pid={randomPage}&tags={incTags}";

        // Задержка перед выполнением запроса
        await Task.Delay(1000);

        HttpResponseMessage response = await Client.GetAsync(url);
        L.M($"Response Status Code: {response.StatusCode}");

        if (response.IsSuccessStatusCode)
        {
            string responseBody = await response.Content.ReadAsStringAsync();

            if (!string.IsNullOrWhiteSpace(responseBody) && !responseBody.Contains("search error"))  // Improved check for search error
            {
                try
                {
                    var posts = JsonSerializer.Deserialize<List<SafebooruPost>>(responseBody);

                    if (posts != null && posts.Count > 0)
                    {
                        cache[tags] = posts; // Кэшируем результаты
                        L.M($"Posts found: {posts.Count}");

                        int randomIndex = rnd.Next(posts.Count);
                        return posts[randomIndex];
                    }
                    else
                    {
                        L.M("No posts found.");
                    }
                }
                catch (JsonException jsonEx)
                {
                    L.M($"JSON deserialization error: {jsonEx.Message}");
                }
            }
            else
            {
                L.M("Search error detected or response body is empty.");
            }
        }
        else
        {
            string errorContent = await response.Content.ReadAsStringAsync();
            L.M($"Error Content: {errorContent}");
        }

        return null;
    }

    public async Task<int> GetPostCountAsync(string tags)
    {
        string incTags = tags.Replace(",", " ");
        string url = BASE_URL + $"index.php?page=dapi&s=post&q=index&limit=1&json=1&tags={incTags}";

        // Задержка перед выполнением запроса
        await Task.Delay(1000);

        HttpResponseMessage response = await Client.GetAsync(url);
        if (response.IsSuccessStatusCode)
        {
            string responseBody = await response.Content.ReadAsStringAsync();
            var posts = JsonSerializer.Deserialize<List<SafebooruPost>>(responseBody);
            if (posts != null && posts.Count > 0)
            {
                return posts[0].ID;
            }
        }
        return 0;
    }
}

public class SafebooruPost
{
    public SafebooruPost(string previewUrl, string sampleUrl, string fileUrl, int directoryNum, string hash, int width, int height, int iD, string image)
    {
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

    [JsonPropertyName("preview_url")]
    public string PreviewUrl { get; set; }
    [JsonPropertyName("sample_url")]
    public string SampleUrl { get; set; }
    [JsonPropertyName("file_url")]
    public string FileUrl { get; set; }
    [JsonPropertyName("directory")]
    public int DirectoryNum { get; set; }
    [JsonPropertyName("hash")]
    public string Hash { get; set; }
    [JsonPropertyName("width")]
    public int Width { get; set; }
    [JsonPropertyName("height")]
    public int Height { get; set; }
    [JsonPropertyName("id")]
    public int ID { get; set; }
    [JsonPropertyName("image")]
    public string Image { get; set; }
}