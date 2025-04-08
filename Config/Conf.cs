using System.Text.Json;
using System.Text.Json.Serialization;
using vkbot_vitalya.Core;

namespace vkbot_vitalya.Config;

public static class Conf {
    public static readonly Configuration Instance;

    static Conf() {
        const string configPath = "config.json";
        if (!File.Exists(configPath)) {
            L.F($"{configPath} not found");
            throw new FileNotFoundException($"{configPath} not found");
        }

        Instance = Configuration.Load(configPath);
    }

    public class Configuration {
        public Configuration(List<string> botNames, Dictionary<string, List<string>> commands,
            Dictionary<string, JsonElement> additionalData) {
            BotNames = botNames;
            Commands = commands;
            AdditionalData = additionalData;
        }

        [JsonPropertyName("bot_names")] public List<string> BotNames { get; init; }

        [JsonPropertyName("commands")] public Dictionary<string, List<string>> Commands { get; init; }

        [JsonPropertyName("auto_update_chats")]
        public bool AutoUpdateChats { get; init; }
        
        public List<string> ForbiddenTags { get; set; }
        
        [JsonPropertyName("blacklist_enabled")]
        public bool UseForbiddenTags { get; init; }

        public Dictionary<string, JsonElement> AdditionalData { get; init; }

        public static Configuration Load(string path) {
            var json = File.ReadAllText(path);
            var jsonSerializerOptions = new JsonSerializerOptions {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
            try {
                var configuration = JsonSerializer.Deserialize<Configuration>(json, jsonSerializerOptions)
                                    ?? throw new Exception("Invalid config file");
                if (File.Exists("forbidden_tags.json"))
                    configuration.ForbiddenTags =
                        JsonSerializer.Deserialize<List<string>>(File.ReadAllText("forbidden_tags.json")) ?? [];
                else
                    configuration.ForbiddenTags = [];
                return configuration;
            } catch (Exception e) {
                throw new Exception("Failed to load config file", e);
            }
        }
    }
}