using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using vkbot_vitalya.Config;
using vkbot_vitalya.Core.Requesters;
using VkNet.Model;

namespace vkbot_vitalya.Services;

/// <summary>
/// это было сложно, но я это сделал
/// </summary>
public class DanbooruApi
{
    private static readonly TimeSpan SecondsBetweenInvokes = TimeSpan.FromSeconds(30);

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

    public DanbooruApi(Authentication auth)
    {
        ApiKey = auth.DanbooruApikey;
        Login = auth.DanbooruLogin;

        Client = ProxyClient.GetProxyHttpClient(auth.ProxyAdress, new NetworkCredential(auth.ProxyLogin, auth.ProxyPassword), "vk-bot-vitalya");

        var byteArray = Encoding.ASCII.GetBytes($"{Login}:{ApiKey}");
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
    }

    public HttpClient Client { get; }

    private string ApiKey { get; set; }
    private string Login { get; set; }

    public async Task<Post?> RandomImageAsync(Action onForbTag, string tags = "", string excludeTags = "yaoi,2boys,multiple_boys,male_penetrated,bara, male_focus, muscular_male, cum_on_male, facial_hair", int count = 1)
    {
        try
        {
            Random rand = new Random();
            int maxAttempts = 5;

            var excludeTagsArray = excludeTags.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                int page = rand.Next(1, 1000);

                // Если теги не указаны, используем отрицательные теги из excludeTags
                var tagList = string.IsNullOrWhiteSpace(tags)
                    ? excludeTagsArray.Skip(attempt % (excludeTagsArray.Length / 2) * 2).Take(2).Select(tag => "-" + Uri.EscapeDataString(tag))
                    : tags.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Take(2).Select(Uri.EscapeDataString);

                string joinedTags = string.Join("+", tagList);

                string url = $"https://danbooru.donmai.us/posts.json?tags={joinedTags}&limit={count}&page={page}";

                Console.WriteLine($"Requesting URL: {url}");
                File.AppendAllText("./log.txt", $"Requesting URL: {url}\n");

                HttpResponseMessage response = await Client.GetAsync(url);

                Console.WriteLine($"Response Status Code: {response.StatusCode}");
                File.AppendAllText("./log.txt", $"Response Status Code: {response.StatusCode}\n");

                if (response.IsSuccessStatusCode)
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    var posts = JsonSerializer.Deserialize<List<Post>>(responseBody);

                    if (posts != null && posts.Count > 0)
                    {
                        Console.WriteLine($"Posts found: {posts.Count}");
                        File.AppendAllText("./log.txt", $"Posts found: {posts.Count}\n");

                        int randomIndex = rand.Next(posts.Count);
                        var post = posts[randomIndex];

                        bool isEnough = false;

                        forbiddenTags.ForEach(forbTag =>
                        {
                            if (post.TagString.Contains(forbTag))
                            {
                                isEnough = true;
                                onForbTag?.Invoke();
                            }
                        });

                        if (isEnough)
                            break;

                        // Добавляем проверку типа файла
                        if (post.FileUrl.EndsWith(".jpg") || post.FileUrl.EndsWith(".jpeg") || post.FileUrl.EndsWith(".png") || post.FileUrl.EndsWith(".gif"))
                        {
                            return post;
                        }
                        else
                        {
                            Console.WriteLine($"Unsupported file type: {post.FileUrl}");
                            File.AppendAllText("./log.txt", $"Unsupported file type: {post.FileUrl}\n");
                        }
                    }
                    else
                    {
                        Console.WriteLine("No posts found.");
                        File.AppendAllText("./log.txt", "No posts found.\n");
                    }
                }
                else
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Error Content: {errorContent}");
                    File.AppendAllText("./log.txt", $"Error Content: {errorContent}\n");
                }
            }
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine($"HttpRequestException: {e.Message}");
            if (e.InnerException != null)
            {
                Console.WriteLine($"Inner Exception: {e.InnerException.Message}");
            }
            File.AppendAllText("./log.txt", $"HttpRequestException: {e.Message}\n");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Exception: {e.Message}");
            if (e.InnerException != null)
            {
                Console.WriteLine($"Inner Exception: {e.InnerException.Message}");
            }
            File.AppendAllText("./log.txt", $"Exception: {e.Message}\n");
        }

        return null;
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
    public string FileUrl { get; set; }
    [JsonPropertyName("preview_file_url")]
    public string PreviewFileUrl { get; set; }
    [JsonPropertyName("tag_string")]
    public string TagString { get; set; }

    public override string ToString() => $"Id: {Id}, FileUrl: {FileUrl}, Tags: {TagString}";
}