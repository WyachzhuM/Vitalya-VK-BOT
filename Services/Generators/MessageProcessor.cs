using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vkbot_vitalya.Services.Generators;

public static class MessageProcessor
{
    private static Random random = new Random();

    public static string GenerateMultipleSentences()
    {
        var lines = File.ReadAllLines(Program.MessagesFilePath);
        if (lines.Length == 0) return "I have nothing to say.";

        var words = lines.SelectMany(line => line.Split(' ')).ToList();
        var sentences = new List<string>();

        for (var i = 0; i < 5; i++)
        {
            var randomWords = words.OrderBy(x => random.Next()).Take(5).ToArray();
            sentences.Add(string.Join(" ", randomWords));
        }

        return string.Join(". ", sentences) + ".";
    }

    public static string GenerateRandomMessage()
    {
        var lines = File.ReadAllLines(Program.MessagesFilePath);
        if (lines.Length == 0) return "I have nothing to say.";

        var method = random.Next(2);

        if (method == 0)
        {
            var randomMessages = lines.OrderBy(x => random.Next()).Take(2).ToArray();
            return string.Join(" ", randomMessages);
        }
        else
        {
            var words = lines.SelectMany(line => line.Split(' ')).ToList();
            var randomWords = words.OrderBy(x => random.Next()).Take(5).ToArray();
            return string.Join(" ", randomWords);
        }
    }
}
