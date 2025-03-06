using vkbot_vitalya.Services.Generators.TextGeneration;
using VkNet.Model;

namespace vkbot_vitalya.Core.Saver;

public class MessageSaver {
    public static string _savedMessagesFolder;
    private static WordAssociations _wordAssociations = new WordAssociations();

    public MessageSaver(string savedMessagesFolder) {
        _savedMessagesFolder = savedMessagesFolder;
    }

    public async Task SaveMessage(Message message) {
        if (!Directory.Exists(_savedMessagesFolder))
            Directory.CreateDirectory(_savedMessagesFolder);

        var fullPath = Path.Combine(_savedMessagesFolder, ChatMessages.GetFileName(message.PeerId.ToString()!));
        if (File.Exists(fullPath)) {
            var saved = await ChatMessages.Deserialize(fullpath: fullPath);

            if (saved) {
                await saved.Update(_savedMessagesFolder, message);
            } else {
                L.I($"{nameof(Program)}: saved == NULL");
                return;
            }
        } else {
            var message1 = new ChatMessage(message.FromId, message.Text, message.Date, message.ConversationMessageId);
            var messages = new List<ChatMessage> {
                message1
            };

            var save = new ChatMessages(message.PeerId, messages);

            await save.Save(_savedMessagesFolder, message);
        }

        UpdateWordAssociations(message.Text);
    }

    private static void UpdateWordAssociations(string text) {
        string[] words = text.Split(" ", StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < words.Length - 1; i++) {
            WordAssociations.AddAssociation(words[i], words[i + 1]);
        }

        WordAssociations.Save(WordAssociations.AssocFilePath);
    }
}