using System.Text.Json;
using System.Text.Json.Serialization;
using vkbot_vitalya.Config;
using vkbot_vitalya.Core;

namespace vkbot_vitalya.Services;

public class WeatherService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public async Task<WeatherResponse?> GetWeatherAsync(string cityName)
    {
        int maxRetries = 3;
        int delayMilliseconds = 2000; // Задержка между повторами

        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                string url = $"http://api.openweathermap.org/data/2.5/weather?q={cityName}&appid={_apiKey}&units=metric&lang=ru";
                L.M($"Requesting URL (Attempt {attempt + 1}): {url}"); // Logging the request URL

                HttpResponseMessage response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    L.D($"Received JSON: {json}");

                    var weatherResponse = JsonSerializer.Deserialize<WeatherResponse>(json);
                    return weatherResponse;
                }
                else
                {
                    L.M($"Response Status Code: {response.StatusCode}"); // Logging the status code
                    string errorContent = await response.Content.ReadAsStringAsync();
                    L.M($"Error Content: {errorContent}"); // Logging the error content
                }
            }
            catch (Exception e)
            {
                L.E(e);
            }

            L.M($"Retrying in {delayMilliseconds / 1000} seconds...");
            await Task.Delay(delayMilliseconds);
        }

        return null;
    }

    public WeatherService(Authentication auth)
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };

        _httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(10) // Set a timeout period
        };
        _apiKey = auth.WeatherApiKey;
    }
}

public class WeatherResponse
{
    public WeatherResponse(WeatherCoord? coord, WeatherMain? main, WeatherWind? wind, WeatherWeather[]? weather, WeatherSys? sys, string name, int? cod)
    {
        Coord = coord;
        Main = main;
        Wind = wind;
        Weather = weather;
        Sys = sys;
        Name = name;
        Cod = cod;
    }

    [JsonPropertyName("coord")]
    public WeatherCoord? Coord { get; set; }
    [JsonPropertyName("main")]
    public WeatherMain? Main { get; set; }
    [JsonPropertyName("wind")]
    public WeatherWind? Wind { get; set; }
    [JsonPropertyName("weather")]
    public WeatherWeather[]? Weather { get; set; }
    [JsonPropertyName("sys")]
    public WeatherSys? Sys { get; set; }
    [JsonPropertyName("name")]
    public string Name { get; set; }
    [JsonPropertyName("cod")]
    public int? Cod { get; set; }
}

public class WeatherCoord
{
    public WeatherCoord(float lon, float lat)
    {
        Lon = lon;
        Lat = lat;
    }

    [JsonPropertyName("lon")]
    public float Lon { get; set; }
    [JsonPropertyName("lat")]
    public float Lat { get; set; }
}

public class WeatherMain
{
    public WeatherMain(double temp, double feelsLike, double tempMin, double tempMax, int pressure, int humidity, int seaLevel, int grndLevel)
    {
        Temp = temp;
        FeelsLike = feelsLike;
        TempMin = tempMin;
        TempMax = tempMax;
        Pressure = pressure;
        Humidity = humidity;
        SeaLevel = seaLevel;
        GrndLevel = grndLevel;
    }

    [JsonPropertyName("temp")]
    public double Temp { get; set; }
    [JsonPropertyName("feels_like")]
    public double FeelsLike { get; set; }
    [JsonPropertyName("temp_min")]
    public double TempMin { get; set; }
    [JsonPropertyName("temp_max")]
    public double TempMax { get; set; }
    [JsonPropertyName("pressure")]
    public int Pressure { get; set; }
    [JsonPropertyName("humidity")]
    public int Humidity { get; set; }
    [JsonPropertyName("sea_level")]
    public int SeaLevel { get; set; }
    [JsonPropertyName("grnd_level")]
    public int GrndLevel { get; set; }
}

public class WeatherWind
{
    public WeatherWind(double speed, int deg, double gust)
    {
        Speed = speed;
        Deg = deg;
        Gust = gust;
    }

    [JsonPropertyName("speed")]
    public double Speed { get; set; }
    [JsonPropertyName("deg")]
    public int Deg { get; set; }
    [JsonPropertyName("gust")]
    public double Gust { get; set; }
}

public class WeatherWeather
{
    public WeatherWeather(int id, string main, string description, string icon)
    {
        Id = id;
        Main = main;
        Description = description;
        Icon = icon;
    }

    [JsonPropertyName("id")]
    public int Id { get; set; }
    [JsonPropertyName("main")]
    public string Main { get; set; }
    [JsonPropertyName("description")]
    public string Description { get; set; }
    [JsonPropertyName("icon")]
    public string Icon { get; set; }
}

public class WeatherSys
{
    public WeatherSys(int type, int id, string country, long sunrise, long sunset)
    {
        Type = type;
        Id = id;
        Country = country;
        Sunrise = sunrise;
        Sunset = sunset;
    }

    [JsonPropertyName("type")]
    public int Type { get; set; }
    [JsonPropertyName("id")]
    public int Id { get; set; }
    [JsonPropertyName("country")]
    public string Country { get; set; }
    [JsonPropertyName("sunrise")]
    public long Sunrise { get; set; }
    [JsonPropertyName("sunset")]
    public long Sunset { get; set; }
}