using System.Text.Json;
using System.Text.Json.Serialization;
using vkbot_vitalya.Config;
using vkbot_vitalya.Core;

namespace vkbot_vitalya.Services;

[Obsolete]
public class MemeGen {
    public MemeGen() {
        if (Auth.Instance.MemeGenApiKey == null) {
            L.I("Put the meme apikey into auth.json file!");
            ApiKey = string.Empty;
        } else {
            ApiKey = Auth.Instance.MemeGenApiKey;
            Client.DefaultRequestHeaders.Add("x-api-key", ApiKey);
        }
    }

    private readonly HttpClient Client = new HttpClient();
    public string ApiKey { get; set; }

    /// <summary>
    /// Search memes from https://api.apileague.com/search-memes
    /// </summary>
    public async Task<MemeGenResponse?> SearchMemes(string keywords, int number, MemeType type) {
        try {
            var memetype = GetMemeTypeString(type);
            var url =
                $"https://api.apileague.com/search-memes?keywords={keywords}&number={number}&media-type={memetype}";

            L.I($"Request form: " + url);

            var response = await Client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var responseBody = await response.Content.ReadAsStringAsync();

            var memes = JsonSerializer.Deserialize<MemeGenResponse>(responseBody);

            if (memes != null) {
                L.I($"Finded {memes.Memes.Count} memes!");
                return memes;
            }

            return null;
        } catch (HttpRequestException e) {
            L.E($"Failed to find meme", e);
            return null;
        }
    }

    /// <summary>
    /// Get random meme from https://api.apileague.com/retrieve-random-meme
    /// </summary>
    public async Task<Meme?> RandomMeme(string keywords, MemeType type) {
        var memetype = string.Empty;
        if (type == MemeType.Image)
            memetype = "jpeg";
        else if (type == MemeType.Video)
            memetype = "mp4";
        else if (type == MemeType.Gif)
            memetype = "gif";
        else memetype = "jpeg";

        var url = $"https://api.apileague.com/retrieve-random-meme?keywords={keywords}&media-type={memetype}";

        try {
            var response = await Client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var responseBody = await response.Content.ReadAsStringAsync();

            var meme = JsonSerializer.Deserialize<Meme>(responseBody);

            if (meme != null) {
                return meme;
            }

            return null;
        } catch (HttpRequestException e) {
            L.E("", e);
            return null;
        }
    }

    public async void Test() {
        var url = "https://api.apileague.com/search-memes?keywords=rocket&number=3";
        var apiKey = "1ba7b7af9d514fb6aa0b4df448ae3c22";

        try {
            var response = await Client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var responseBody = await response.Content.ReadAsStringAsync();

            L.I(responseBody);
        } catch (HttpRequestException e) {
            L.E("", e);
        }
    }

    private string GetMemeTypeString(MemeType type) {
        return type switch {
            MemeType.Video => "mp4",
            MemeType.Gif => "gif",
            _ => "image",
        };
    }
}

public class MemeGenResponse {
    public MemeGenResponse() {
    }

    public MemeGenResponse(List<Meme> memes, int available) {
        Memes = memes;
        Available = available;
    }

    [JsonPropertyName("memes")] public List<Meme> Memes { get; set; }
    [JsonPropertyName("available")] public int Available { get; set; }
}

public class Meme {
    public Meme() {
    }

    public Meme(string description, string url, string type, int width, int height, int ratio) {
        Description = description;
        Url = url;
        Type = type;
        Width = width;
        Height = height;
        Ratio = ratio;
    }

    [JsonPropertyName("description")] public string Description { get; set; }
    [JsonPropertyName("url")] public string Url { get; set; }
    [JsonPropertyName("type")] public string Type { get; set; }
    [JsonPropertyName("width")] public int Width { get; set; }
    [JsonPropertyName("height")] public int Height { get; set; }
    [JsonPropertyName("ratio")] public double Ratio { get; set; }

    public MemeType GetMemeType() {
        if (Type == "video/mp4")
            return MemeType.Video;
        if (Type == "image/png")
            return MemeType.Image;
        if (Type == "image/jpeg")
            return MemeType.Image;
        return MemeType.Image;
    }
}

public enum MemeType {
    Image,
    Video,
    Gif
}