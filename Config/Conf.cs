using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace vkbot_vitalya.Config;

public class Conf
{
    public Conf(List<string> botNames, double responseProbability, Dictionary<string, List<string>> commands, Dictionary<string, JsonElement> additionalData)
    {
        BotNames = botNames;
        ResponseProbability = responseProbability;
        Commands = commands;
        AdditionalData = additionalData;
    }

    public static Conf? Instance { get; private set; }

    [JsonPropertyName("bot_names")]
    public List<string> BotNames { get; set; }

    [JsonPropertyName("response_probability")]
    public double ResponseProbability { get; set; }

    [JsonPropertyName("commands")]
    public Dictionary<string, List<string>> Commands { get; set; }

    public Dictionary<string, JsonElement> AdditionalData { get; set; }

    public static Conf? GetConfigFromJson(string path)
    {
        string json = File.ReadAllText(path);
        JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    
        var result = JsonSerializer.Deserialize<Conf>(json, jsonSerializerOptions);

        Instance = result;

        return result;
    }
}