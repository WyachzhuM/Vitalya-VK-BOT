using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using vkbot_vitalya.Config;
using vkbot_vitalya.Core;
using vkbot_vitalya.Core.Requesters;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace vkbot_vitalya.Services;

/// <summary>
/// это было сложно, но я это сделал
/// </summary>
public class DanbooruApi {
    private const string MasterUrl = "https://danbooru.donmai.us/";

    private static readonly Random Rand = new Random();
    private static readonly Dictionary<string, int> TagsCounters = [];
    public readonly Dictionary<string, TagCache> TagsCache;

    public DanbooruApi() {
        if (File.Exists("tags_cache.json")) {
            var text = File.ReadAllText("tags_cache.json");
            TagsCache = JsonConvert.DeserializeObject<Dictionary<string, TagCache>>(text) ?? [];
        } else {
            TagsCache = [];
        }
        ApiKey = Auth.Instance.DanbooruApikey;
        Login = Auth.Instance.DanbooruLogin;

        Client = ProxyClient.GetProxyHttpClient(Auth.Instance.ProxyAdress,
            new NetworkCredential(Auth.Instance.ProxyLogin, Auth.Instance.ProxyPassword), "vk-bot-vitalya");

        var byteArray = Encoding.ASCII.GetBytes($"{Login}:{ApiKey}");
        Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
    }

    public HttpClient Client { get; }

    private string ApiKey { get; }
    private string Login { get; }

    private static readonly string[] AllowedFormats = ["jpg", "jpeg", "png", "gif", "webp"];

    public async Task<(string?, string?)> RandomImageAsync(string tagsString = "") {
        string[] alwaysExclude = ["loli", "shota"];
        var tags = tagsString.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(Uri.EscapeDataString).ToList();

        // Исключаем чилд по
        if (tags.Intersect(alwaysExclude).Any()) 
            return (null, "Ничего нет с такими тегами");
        
        // Исключаем запрещенные теги
        if (Conf.Instance.UseForbiddenTags && tags.Intersect(Conf.Instance.ForbiddenTags).Any())
            return (null, "Я эту хуйню искать не буду");
        
        // Узнаем количество постов с каждым тегом
        Dictionary<string, TagCache> cache = [];
        foreach (var tag in tags) {
            if (TagsCache.TryGetValue(tag, out var value)) {
                cache[tag] = value;
                continue;
            }
            var url1 = $"{MasterUrl}tags.json?search[name_or_alias_matches]={tag}&search[hide_empty]=true";
            using var response1 = await Client.GetAsync(url1);
            response1.EnsureSuccessStatusCode();
            var responseBody1 = await response1.Content.ReadAsStringAsync();
            var jArray = JArray.Parse(responseBody1);
            if (jArray.Count < 1)
                return (null, $"Ничего нет с тегом {tag}");

            var count = (int)jArray[0]["post_count"];
            var order = Enumerable.Range(0, count).ToArray();
            Rand.Shuffle(order);
            var tagCache = new TagCache(count, order);
            TagsCache.Add(tag, tagCache);
            L.I($"{tag}: {count}");
            cache[tag] = tagCache;
        }

        var tagsByRarity = cache.OrderBy(p => p.Value).ToList();
        var tagsHash = string.Join(' ', tagsByRarity.Select(p => p.Key));

        for (var attempt = 0; attempt < 5; attempt++) {
            string url;
            switch (tagsByRarity.Count) {
                case 0:
                    // Нет тегов, выбираю любую страницу
                    url = $"{MasterUrl}posts.json?" +
                          $"page={Rand.Next(1000)}" +
                          $"&limit=1" +
                          $"&tags=-loli+-shota";
                    break;
                case 1:
                    // Один тег, ограничиваюсь количеством постов с ним
                    var tagCache = tagsByRarity[0].Value;
                    url = $"{MasterUrl}posts.json?" +
                          $"page={Math.Min(tagCache.order[tagCache.i++], 1000)}" +
                          $"&limit=1" +
                          $"&tags={tagsByRarity[0].Key}+-loli";
                    if (tagCache.i > tagCache.count)
                        tagCache.i = 0;
                    break;
                default: 
                    // Много тегов, не могу узнать количество постов, начинаю с первого
                    TagsCounters.TryAdd(tagsHash, 0);
                    TagsCounters[tagsHash] += 1;
                    url = $"{MasterUrl}posts.json?" +
                          $"page={TagsCounters[tagsHash]}" +
                          $"&limit=1" +
                          $"&tags={tagsByRarity[0].Key}+{tagsByRarity[1].Key}";
                    break;
            } 

            L.I($"Requesting URL: {url}");

            using var response = await Client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            var posts = JsonSerializer.Deserialize<List<Post>>(responseBody);

            if (posts is not { Count: > 0 }) {
                if (tagsByRarity.Count < 2)
                    L.E("Тегов меньше 2, но посты не найдены");
                // Посты с несколькими тегами закончились, сбрасываю счетчик
                if (TagsCounters.ContainsKey(tagsHash))
                    TagsCounters[tagsHash] = 0;
                L.I("No posts found. Retrying");
                continue;
            }

            L.I($"Posts found: {posts.Count}");

            var post = posts[Rand.Next(posts.Count)];

            L.I($"Tags: {post.TagString}");


            if (Conf.Instance.UseForbiddenTags && Conf.Instance.ForbiddenTags.Any(tag => post.TagString.Contains(tag))) {
                // Попался запрещенный тег
                L.I("Got forbidden tag. Retrying");
                continue;
            }

            if (post.FileUrl == null) {
                // Без понятия
                if (post.TagString.Split(' ').Intersect(alwaysExclude).Any())
                    L.I("Got loli or shota in tags. Retrying");
                else
                    L.E("post.FileUrl is null");

                continue;
            }

            if (AllowedFormats.Any(s => post.FileUrl.EndsWith(s))) {
                return (post.FileUrl, null);
            }

            L.I($"Got {post.FileUrl.Split('.')[^1]} format. Retrying");
        }
        
        return (null, "Извините, не удалось найти изображение аниме");
    }
    
    public class TagCache(int count, int[] order) {
        public readonly int count = count;
        public readonly int[] order = order;
        public int i = 0;
    }
}

public class Post {
    public Post(int id, string fileUrl, string previewFileUrl, string tagString) {
        Id = id;
        FileUrl = fileUrl;
        PreviewFileUrl = previewFileUrl;
        TagString = tagString;
    }

    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("file_url")] public string? FileUrl { get; set; }
    [JsonPropertyName("preview_file_url")] public string PreviewFileUrl { get; set; }
    [JsonPropertyName("tag_string")] public string TagString { get; set; }

    public override string ToString() => $"Id: {Id}, FileUrl: {FileUrl}, Tags: {TagString}";
}