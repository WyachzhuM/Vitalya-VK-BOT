using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace vkbot_vitalya;

public class Config
{
    // Modified constructor to accept multiple bot names and commands
    public Config(List<string> botNames, double responseProbability, Dictionary<string, List<string>> commands)
    {
        BotNames = botNames;
        ResponseProbability = responseProbability;
        Commands = commands;
    }

    // Modified property to hold multiple bot names
    [JsonPropertyName("bot_names")]
    public List<string> BotNames { get; set; }

    [JsonPropertyName("response_probability")]
    public double ResponseProbability { get; set; }

    // Modified property to hold multiple forms of each command
    [JsonPropertyName("commands")]
    public Dictionary<string, List<string>> Commands { get; set; }

    public static Config? GetConfigFromJson(string path)
    {
        string json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<Config>(json);
    }
}
