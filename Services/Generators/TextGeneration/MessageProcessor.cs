using vkbot_vitalya.Core;
using VkNet.Model;

namespace vkbot_vitalya.Services.Generators.TextGeneration;

public static class MessageProcessor {
    private const int MAX_MESSAGE_LENGTH = 50;
    private static Random _random = new Random();

    // To services 
    public static async Task<string> KeepUpConversation() {
        var chatFiles = Directory.GetFiles(Program._savedMessagesFolder, "Chat*.json");

        if (chatFiles.Length == 0) {
            return "No chats available.";
        }

        var randomChatFilePath = chatFiles[_random.Next(chatFiles.Length)];
        var chatMessages = await ChatMessages.Deserialize(randomChatFilePath);

        if (chatMessages.Messages.Count == 0) {
            return "Selected chat has no messages.";
        }

        var newMessage = GenerateNewMessage(chatMessages.Messages);
        return newMessage;
    }

    // To random chat 
    public static async Task<string> KeepUpConversation(Message message) {
        // var messages = await ChatMessages.GetMessages(message.PeerId);

        var derivedMessages = await ChatMessages.GetMessagesFromUser(message.PeerId, message.FromId);
        var newMessage = GenerateNewMessage(derivedMessages);
        return newMessage;
    }

    private static string GenerateNewMessage(List<ChatMessage> messages) {
        var result = string.Empty;

        if (messages.Count == 0) {
            return result;
        }

        var random = new Random();

        var words = messages[random.Next(messages.Count)].Text!.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0) {
            return result;
        }

        var currentWord = words[random.Next(words.Length)];

        var m = random.Next(MAX_MESSAGE_LENGTH);
        for (var i = 0; i < m; i++) {
            result += currentWord + " ";

            var nextWord = WordAssociations.GetNextWord(currentWord);
            if (string.IsNullOrEmpty(nextWord)) {
                break;
            }

            currentWord = nextWord;
        }

        return result.Trim();
    }
}