using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Processing;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;
using vkbot_vitalya.Config;
using vkbot_vitalya.Core;

namespace vkbot_vitalya.Services;

public class Map
{
    private string _yandexApikey { get; set; }

    public Map() => _yandexApikey = Auth.Instance.YandexApiKey;

    public async Task<(Image?, (string lat, string lon))> Search(string location)
    {
        var coordinates = await GetCoordinatesAsync(location);

        if (coordinates == null)
        {
            L.I("Location not found.");
            return (null, (string.Empty, string.Empty));
        }

        var mapUrl = GenerateMapUrl(coordinates.Value.lat, coordinates.Value.lon, 12, 650, 450);
        var mapImage = await GetMapImageAsync(mapUrl);

        if (mapImage == null)
        {
            L.I("Failed to retrieve map image.");
            return (null, (string.Empty, string.Empty));
        }

        var markedImage = MarkLocationOnMap(mapImage, coordinates.Value.lat, coordinates.Value.lon);

        return (markedImage, ($"lat: {coordinates.Value.lat}", $"lon: {coordinates.Value.lon}"));
    }

    private static async Task<(double lat, double lon)?> GetCoordinatesAsync(string address)
    {
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("User-Agent", "MapExampleApp/1.0");

        try
        {
            var encodedAddress = HttpUtility.UrlEncode(address);
            var url = $"https://nominatim.openstreetmap.org/search?q={encodedAddress}&format=json&limit=1";
            L.I($"Request URL: {url}");

            var response = await httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                L.I($"HTTP Error: {(int)response.StatusCode} - {response.ReasonPhrase}");
                return null;
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            L.D($"Response: {responseContent}");

            var locationData = JsonSerializer.Deserialize<Location[]>(responseContent);

            if (locationData == null || locationData.Length == 0)
            {
                L.I("No location data found.");
                return null;
            }

            var latString = locationData[0].Lat.Replace(',', '.');
            var lonString = locationData[0].Lon.Replace(',', '.');

            if (double.TryParse(latString, NumberStyles.Float, CultureInfo.InvariantCulture, out var lat) &&
                double.TryParse(lonString, NumberStyles.Float, CultureInfo.InvariantCulture, out var lon))
            {
                L.I($"Coordinates: lat={lat}, lon={lon}");
                return (lat, lon);
            }
            else
            {
                L.I("Failed to convert coordinates to doubles.");
                return null;
            }
        }
        catch (HttpRequestException e)
        {
            L.I($"Request error: {e.Message}");
            return null;
        }
        catch (JsonException e)
        {
            L.I($"JSON parsing error: {e.Message}");
            return null;
        }
        catch (Exception e)
        {
            L.I($"Unexpected error: {e.Message}");
            return null;
        }
    }

    private string GenerateMapUrl(double lat, double lon, int zoom, int width, int height)
    {
        return $"https://static-maps.yandex.ru/1.x/?ll={lon.ToString(CultureInfo.InvariantCulture)},{lat.ToString(CultureInfo.InvariantCulture)}&z={zoom}&size={width},{height}&l=map&apikey={_yandexApikey}";
    }

    private static async Task<Image?> GetMapImageAsync(string url)
    {
        using var httpClient = new HttpClient();
        try
        {
            var response = await httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                L.I($"HTTP Error: {(int)response.StatusCode} - {response.ReasonPhrase}");
                return null;
            }

            var responseStream = await response.Content.ReadAsStreamAsync();
            return await Image.LoadAsync(responseStream);
        }
        catch (HttpRequestException e)
        {
            L.I($"Request error: {e.Message}");
            return null;
        }
        catch (Exception e)
        {
            L.I($"Unexpected error: {e.Message}");
            return null;
        }
    }

    private static Image MarkLocationOnMap(Image mapImage, double lat, double lon)
    {
        const int markerSize = 10;

        var markerX = mapImage.Width / 2;
        var markerY = mapImage.Height / 2;

        mapImage.Mutate(ctx =>
        {
            ctx.Fill(Color.Red, new EllipsePolygon(markerX, markerY, markerSize / 2));
        });

        return mapImage;
    }
}

public class Location
{
    public Location(string lat, string lon)
    {
        Lat = lat;
        Lon = lon;
    }

    [JsonPropertyName("lat")]
    public string Lat { get; set; }
    [JsonPropertyName("lon")]
    public string Lon { get; set; }
}
