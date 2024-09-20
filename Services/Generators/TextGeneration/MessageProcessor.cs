using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using vkbot_vitalya.Core;
using VkNet.Model;

namespace vkbot_vitalya.Services.Generators.TextGeneration;

public static class MessageProcessor
{
    private const int MAX_MESSAGE_LENGTH = 3;
    private static Random _random = new Random();

    // To services 
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

    // To random chat 
    public static async Task<string> KeepUpConversation(Message message)
    {
        if (message.PeerId != null)
        {
            var messages = await ChatMessages.GetMessages(message.PeerId);

            List<ChatMessage> derivedMessages;

            derivedMessages = await ChatMessages.GetMessagesFromUser(message.PeerId, message.FromId);

            string newMessage = GenerateNewMessage(derivedMessages);

            return newMessage;
        }

        return "Unable to generate message, PeerId is null.";
    }

    private static string GenerateNewMessage(List<ChatMessage> messages)
    {
        string result = string.Empty;

        if (messages.Count == 0)
        {
            return result;
        }

        Random random = new Random();

        string[] words = messages[random.Next(messages.Count)].Text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
        {
            return result;
        }

        string currentWord = words[random.Next(words.Length)];

        for (int i = 0; i < MAX_MESSAGE_LENGTH; i++)
        {
            result += currentWord + " ";

            string nextWord = WordAssociations.GetNextWord(currentWord);
            if (string.IsNullOrEmpty(nextWord))
            {
                break;
            }

            currentWord = nextWord;
        }

        return result.Trim();
    }
}