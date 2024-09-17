using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using vkbot_vitalya.Core;
using VkNet.Model;

namespace vkbot_vitalya.Services.Generators;

public static class MessageProcessor
{
    private const int MAX_MESSAGE_LENGTH = 5;
    private static Random _random = new Random();

    public static async Task<string> KeepUpConversation(Message message)
    {
        if (message.PeerId != null)
        {
            var messages = await ChatMessages.GetMessages(message.PeerId);

            bool useFromUser = _random.Next(0, 2) == 1;

            List<ChatMessage> derivedMessages;
            if (useFromUser)
            {
                derivedMessages = await ChatMessages.GetMessagesFromUser(message.PeerId, message.FromId);
            }
            else
            {
                derivedMessages = messages;
            }

            string newMessage = GenerateNewMessage(derivedMessages);

            return newMessage;
        }

        return "Unable to generate message, PeerId is null.";
    }

    public static async Task<string> KeepUpConversation()
    {
        var chatFiles = Directory.GetFiles(Program._savedMessagesFolder, "Chat*.json");

        if (chatFiles.Length == 0)
        {
            return "No chats available.";
        }

        var randomChatFilePath = chatFiles[_random.Next(chatFiles.Length)];
        var chatMessages = await ChatMessages.Deserialize(randomChatFilePath);

        if (chatMessages.Messages.Count == 0)
        {
            return "Selected chat has no messages.";
        }

        string newMessage = GenerateNewMessage(chatMessages.Messages);
        return newMessage;
    }

    private static string GenerateNewMessage(List<ChatMessage> messages)
    {
        bool generateMultiple = _random.Next(0, 2) == 1;

        if (generateMultiple)
        {
            var sentences = new List<string>();
            var words = messages.SelectMany(m => m.Text.Split(' ')).ToList();

            for (var i = 0; i < MAX_MESSAGE_LENGTH; i++)
            {
                var randomWords = words.OrderBy(x => _random.Next()).Take(5).ToArray();
                sentences.Add(string.Join(" ", randomWords));
            }

            return string.Join(". ", sentences) + ".";
        }
        else
        {
            var lines = messages.Select(m => m.Text).ToList();

            if (lines.Count == 0)
                return "I have nothing to say.";

            var method = _random.Next(2);

            if (method == 0)
            {
                var randomMessages = lines.OrderBy(x => _random.Next()).Take(2).ToArray();
                return string.Join(" ", randomMessages);
            }
            else
            {
                var words = lines.SelectMany(line => line.Split(' ')).ToList();
                var randomWords = words.OrderBy(x => _random.Next()).Take(5).ToArray();
                return string.Join(" ", randomWords);
            }
        }
    }
}