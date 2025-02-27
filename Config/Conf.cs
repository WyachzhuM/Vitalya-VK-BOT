﻿using System.Text.Json;
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
        public Configuration(List<string> botNames, double responseProbability, Dictionary<string, List<string>> commands,
            Dictionary<string, JsonElement> additionalData) {
            BotNames = botNames;
            ResponseProbability = responseProbability;
            Commands = commands;
            AdditionalData = additionalData;
        }

        [JsonPropertyName("bot_names")]
        public List<string> BotNames { get; init; }

        [JsonPropertyName("response_probability")]
        public double ResponseProbability { get; init; }

        [JsonPropertyName("commands")]
        public Dictionary<string, List<string>> Commands { get; init; }

        public Dictionary<string, JsonElement> AdditionalData { get; init; }

        public static Configuration Load(string path) {
            var json = File.ReadAllText(path);
            var jsonSerializerOptions = new JsonSerializerOptions {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
            try {
                return JsonSerializer.Deserialize<Configuration>(json, jsonSerializerOptions)
                       ?? throw new Exception("Invalid config file");
            } catch (Exception e) {
                throw new Exception("Failed to load config file", e);
            }
        }
    }
}

