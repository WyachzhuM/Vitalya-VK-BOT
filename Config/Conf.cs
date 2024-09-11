using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace vkbot_vitalya.Config;

public record Conf(List<string> BotNames, double ResponseProbability, Dictionary<string, List<string>> Commands, Dictionary<string, JsonElement> AdditionalData)
{
    [JsonPropertyName("bot_names")]
    public List<string> BotNames { get; set; } = BotNames;

    [JsonPropertyName("response_probability")]
    public double ResponseProbability { get; set; } = ResponseProbability;

    [JsonPropertyName("commands")]
    public Dictionary<string, List<string>> Commands { get; set; } = Commands;

    public static Conf? GetConfigFromJson(string path)
    {
        string json = File.ReadAllText(path);
        JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        var result = JsonSerializer.Deserialize<Conf>(json, jsonSerializerOptions);

        return result;
    }
}

//public class Conf
//{
//    public Conf()
//    {
//    }
//
//    // Modified constructor to accept multiple bot names and commands
//    public Conf(List<string> botNames, double responseProbability, Dictionary<string, List<string>> commands)
//    {
//        BotNames = botNames;
//        ResponseProbability = responseProbability;
//        Commands = commands;
//    }
//
//    // Modified property to hold multiple bot names
//    [JsonPropertyName("bot_names")]
//    public List<string> BotNames { get; set; }
//
//    [JsonPropertyName("response_probability")]
//    public double ResponseProbability { get; set; }
//
//    // Modified property to hold multiple forms of each command
//    [JsonPropertyName("commands")]
//    public Dictionary<string, List<string>> Commands { get; set; }
//
//    [JsonExtensionData]
//    public Dictionary<string, JsonElement> AdditionalData { get; set; }
//
//    public static Conf GetConfigFromJson(string path)
//    {
//        string json = File.ReadAllText(path);
//        var options = new JsonSerializerOptions()
//        {
//            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
//        };
//        var result = JsonSerializer.Deserialize<Conf>(json, options);
//        if (result == null)
//        {
//            return new Conf();
//        }
//        return result;
//    }
//}

