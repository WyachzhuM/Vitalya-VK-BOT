using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace vkbot_vitalya
{
    public class Config
    {
        public Config()
        {
        }

        public Config(string bot_name, double response_probability, Commands commands)
        {
            BotName = bot_name;
            ResponseProbability = response_probability;
            Commands = commands;
        }

        [JsonPropertyName("bot_name")]
        public string BotName { get; set; }
        [JsonPropertyName("response_probability")]
        public double ResponseProbability { get; set; }
        [JsonPropertyName("commands")]
        public Commands Commands { get; set; }

        public static Config? GetConfigFromJson(string path)
        {
            string json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<Config>(json);
        }
    }

    public class Commands
    {
        [JsonPropertyName("generate_sentences")]
        public string GenerateSentences { get; set; }
        [JsonPropertyName("echo")]
        public string Echo { get; set; }
        [JsonPropertyName("break")]
        public string Break { get; set; }
        [JsonPropertyName("liquidate")]
        public string Liquidate { get; set; }
        [JsonPropertyName("compress")]
        public string Compress { get; set; }
        [JsonPropertyName("add_text")]
        public string AddText { get; set; }
        [JsonPropertyName("meme")]
        public string Meme { get; set; }
    }
}
