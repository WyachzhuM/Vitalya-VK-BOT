using System.Text.Json;
using System.Text.Json.Serialization;
using VkBot;

namespace vkbot_vitalya;

public class MemeGen
{
    public MemeGen(AuthBotFile auth)
    {
        if (auth.MemeGenApiKey == null)
        {
            Console.WriteLine("Put the meme apikey into auth.json file!");
            ApiKey = string.Empty;
        }
        else
        {
            ApiKey = auth.MemeGenApiKey;
            Client.DefaultRequestHeaders.Add("x-api-key", ApiKey);
        }
    }

    private readonly HttpClient Client = new HttpClient();
    public string ApiKey { get; set; }

    /// <summary>
    /// Search memes from https://api.apileague.com/search-memes
    /// </summary>
    public async Task<MemeGenResponse?> SearchMemes(string keywords, int number, MemeType type)
    {
        try
        {
            string memetype = GetMemeTypeString(type);
            string url = $"https://api.apileague.com/search-memes?keywords={keywords}&number={number}&media-type={memetype}";

            Console.WriteLine($"Request form: " + url);

            HttpResponseMessage response = await Client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();

            var memes = JsonSerializer.Deserialize<MemeGenResponse>(responseBody);

            if (memes != null)
            {
                Console.WriteLine($"Finded {memes.Memes.Count} memes!");
                return memes;
            }
            else
            {
                return null;
            }
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine("\nException Caught!");
            Console.WriteLine("Message :{0} ", e.Message);
            return null;
        }
    }

    /// <summary>
    /// Get random meme from https://api.apileague.com/retrieve-random-meme
    /// </summary>
    public async Task<Meme?> RandomMeme(string keywords, MemeType type)
    {
        string memetype = string.Empty;
        if (type == MemeType.Image)
            memetype = "jpeg";
        else if (type == MemeType.Video)
            memetype = "mp4";
        else if (type == MemeType.Gif)
            memetype = "gif";
        else memetype = "jpeg";

        string url = $"https://api.apileague.com/retrieve-random-meme?keywords={keywords}&media-type={memetype}";

        try
        {
            HttpResponseMessage response = await Client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();

            var meme = JsonSerializer.Deserialize<Meme>(responseBody);

            if (meme != null)
            {
                return meme;
            }
            else
            {
                return null;
            }
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine("\nException Caught!");
            Console.WriteLine("Message :{0} ", e.Message);

            return null;
        }
    }

    public async void Test()
    {
        string url = "https://api.apileague.com/search-memes?keywords=rocket&number=3";
        string apiKey = "1ba7b7af9d514fb6aa0b4df448ae3c22";

        try
        {
            HttpResponseMessage response = await Client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();

            Console.WriteLine(responseBody);
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine("\nException Caught!");
            Console.WriteLine("Message :{0} ", e.Message);
        }
    }

    private string GetMemeTypeString(MemeType type)
    {
        return type switch
        {
            MemeType.Video => "mp4",
            MemeType.Gif => "gif",
            _ => "image",
        };
    }
}

public class MemeGenResponse
{
    public MemeGenResponse()
    {
    }

    public MemeGenResponse(List<Meme> memes, int available)
    {
        Memes = memes;
        Available = available;
    }

    [JsonPropertyName("memes")]
    public List<Meme> Memes { get; set; }
    [JsonPropertyName("available")]
    public int Available { get; set; }
}

public class Meme
{
    public Meme()
    {
    }

    public Meme(string description, string url, string type, int width, int height, int ratio)
    {
        Description = description;
        Url = url;
        Type = type;
        Width = width;
        Height = height;
        Ratio = ratio;
    }

    [JsonPropertyName("description")]
    public string Description { get; set; }
    [JsonPropertyName("url")]
    public string Url { get; set; }
    [JsonPropertyName("type")]
    public string Type { get; set; }
    [JsonPropertyName("width")]
    public int Width { get; set; }
    [JsonPropertyName("height")]
    public int Height { get; set; }
    [JsonPropertyName("ratio")]
    public double Ratio { get; set; }

    public MemeType GetMemeType()
    {
        if (Type == "video/mp4")
            return MemeType.Video;
        if (Type == "image/png")
            return MemeType.Image;
        if (Type == "image/jpeg")
            return MemeType.Image;
        else
            return MemeType.Image;
    }
}

public enum MemeType
{
    Image,
    Video,
    Gif
}