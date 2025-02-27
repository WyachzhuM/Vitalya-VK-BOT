using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using vkbot_vitalya.Config;
using vkbot_vitalya.Core;
using vkbot_vitalya.Core.Requesters;

namespace vkbot_vitalya.Services;

/// <summary>
/// это было сложно, но я это сделал
/// </summary>
public class DanbooruApi
{
    private static readonly TimeSpan SECONDS_BETWEEN_INVOKES = TimeSpan.FromSeconds(30);
    private static readonly Random RAND = new Random();

    private readonly List<string> forbiddenTags = new List<string>()
    {
        "futanari", "gay", "furry", "penis", "testicles", "huge penis", "erection", "inflation",
        "loli", "child on child", "yaoi", "2boys", "nazi", "trap", "succubus", "corpse",
        "coprophilic", "cunt", "multiple boys", "yaoi", "2boys", "multiple_boys", "male_penetrated",
        "bara", "male_focus", "muscular_male", "cum_on_male", "rotten", "coprophagia",
        "scat", "diarrhea", "poop", "squat toilet", "pee", "toilet use", "guro", "ero guro",
        "vomit", "fart", "tentacles", "peeing", "personality excrement", "defecating",
        "enema", "execution", "hazbin_hotel"
    };

    public DanbooruApi()
    {
        ApiKey = Auth.Instance.DanbooruApikey;
        Login = Auth.Instance.DanbooruLogin;

        Client = ProxyClient.GetProxyHttpClient(Auth.Instance.ProxyAdress, new NetworkCredential(Auth.Instance.ProxyLogin, Auth.Instance.ProxyPassword), "vk-bot-vitalya");

        var byteArray = Encoding.ASCII.GetBytes($"{Login}:{ApiKey}");
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
    }

    public HttpClient Client { get; }

    private string ApiKey { get; }
    private string Login { get; }

    public async Task<string?> RandomImageAsync(Action onForbTag, string tags = "",
        string excludeTags =
            "yaoi,2boys,multiple_boys,male_penetrated,bara, male_focus, muscular_male, cum_on_male, facial_hair",
        int count = 1) {
        try {
            const int maxAttempts = 5;

            var excludeTagsArray = excludeTags.Split(',', StringSplitOptions.RemoveEmptyEntries);

            for (var i = 0; i < maxAttempts; i++) {
                var page = RAND.Next(1, 1000);

                var tagList = string.IsNullOrWhiteSpace(tags) && !Program.IgnoreTagsBlacklist
                    ? excludeTagsArray.Skip(i % (excludeTagsArray.Length / 2) * 2).Take(2).Select(tag => "-" + Uri.EscapeDataString(tag))
                    : tags.Split(',', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(Uri.EscapeDataString);

                var joinedTags = string.Join("+", tagList);

                var url = $"https://danbooru.donmai.us/posts.json?limit={count}&page={page}";
                if (joinedTags.Length > 0) {
                    url += $"&tags={joinedTags}";
                }

                L.I($"Requesting URL: {url}");

                using var response = await Client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsStringAsync();
                var posts = JsonSerializer.Deserialize<List<Post>>(responseBody);

                if (posts is not { Count: > 0 }) {
                    L.I("No posts found.");
                    continue;
                }

                L.I($"Posts found: {posts.Count}");

                var post = posts[RAND.Next(posts.Count)];

                var isEnough = false;

                forbiddenTags.ForEach(forbTag => {
                    if (post.TagString.Contains(forbTag)) {
                        isEnough = true;
                        onForbTag.Invoke();
                    }
                });

                if (isEnough && !Program.IgnoreTagsBlacklist) {
                    continue;
                }

                // null for tags: loli, 
                if (post.FileUrl == null) {
                    L.E($"null url for tags: {joinedTags}");
                    continue;
                }
                
                // Добавляем проверку типа файла
                if (post.FileUrl.EndsWith(".jpg") || post.FileUrl.EndsWith(".jpeg") ||
                    post.FileUrl.EndsWith(".png") || post.FileUrl.EndsWith(".gif")) {
                    return post.FileUrl;
                }

                L.I($"Unsupported file type: {post.FileUrl}");
            }
            return null;
        } catch (Exception e) {
            L.E("Failed to find image", e);
            return null;
        }
    }
}

public class Post
{
    public Post(int id, string fileUrl, string previewFileUrl, string tagString)
    {
        Id = id;
        FileUrl = fileUrl;
        PreviewFileUrl = previewFileUrl;
        TagString = tagString;
    }

    [JsonPropertyName("id")]
    public int Id { get; set; }
    [JsonPropertyName("file_url")]
    public string? FileUrl { get; set; }
    [JsonPropertyName("preview_file_url")]
    public string PreviewFileUrl { get; set; }
    [JsonPropertyName("tag_string")]
    public string TagString { get; set; }

    public override string ToString() => $"Id: {Id}, FileUrl: {FileUrl}, Tags: {TagString}";
}