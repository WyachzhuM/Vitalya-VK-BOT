using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using log4net.Repository.Hierarchy;
using Microsoft.Extensions.Primitives;
using vkbot_vitalya.Config;
using vkbot_vitalya.Core;
using vkbot_vitalya.Services.Generators;
using vkbot_vitalya.Services.Generators.TextGeneration;
using VkNet.Model;

namespace vkbot_vitalya;

/// <summary>
/// Обработчик сообщений для одного бота на все чаты
/// </summary>
public partial class MessageHandler {
    private static readonly Random Rand = new Random();
    private readonly Vk _vk;
    private Saves _saves;

    [Obsolete] private Timer _messageTimer, _updateTimer;

    public bool isEnabled = true;

    public MessageHandler(Vk vk) {
        _vk = vk;
        ServiceEndpoint = new ServiceEndpoint();
        Processor = new ImageProcessor();
        _saves = Saves.Load();
        // _updateTimer = new Timer(Update, null, TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(10));
        // _messageTimer = new Timer(SendPeriodicMessages, null, TimeSpan.FromMinutes(60 * 2), TimeSpan.FromMinutes(60 * 2));
    }

    private ServiceEndpoint ServiceEndpoint { get; set; }
    private ImageProcessor Processor { get; set; }

    public async Task HandleMessage(Message message) {
        var sb = new StringBuilder();
        if (message.PeerId != message.FromId)
            sb.Append($"[{message.PeerId}] ");
        sb.Append($"<{message.FromId}> {message.Text}");
        if (message.ForwardedMessages != null)
            foreach (var forwarded in message.ForwardedMessages)
                sb.Append($"\n-> <{forwarded.FromId}> {forwarded.Text}");
        if (message.Attachments.Count > 0)
            sb.Append($" + {message.Attachments.Count} attachments");
        L.D(sb);

        if (message.Payload != null) {
            L.I("Got payload");
            HandlePayload(message);
            return;
        }

        MessageSaving(message);

        var text = Regex.Replace(message.Text, @"\s+", " ").Trim();
        var inDm = message.PeerId < 2000000000;

        string? nameUsed = null;
        string? commandUsed = null;
        string? aliasUsed = null;
        string? args = null;

        // После слова может быть пробел или конец строки
        foreach (var name in Conf.Instance.BotNames) {
            if (Regex.IsMatch(text.ToLower(), $"^{Regex.Escape(name)}(\\s|$)")) {
                nameUsed = name;
                text = text.Substring(name.Length).TrimStart();
                break;
            }
        }

        if (!inDm && nameUsed == null) {
            // Simple text
            if (Rand.NextSingle() < _saves.GetChat(message.PeerId!.Value).Properties.ResponseProbability) {
                var responseMessage = await MessageProcessor.KeepUpConversation(message);
                Answer(message, responseMessage);
            }

            return;
        }

        // После слова может быть пробел или конец строки
        foreach (var command in Conf.Instance.Commands) {
            foreach (var alias in command.Value) {
                if (Regex.IsMatch(text.ToLower(), $"^{Regex.Escape(alias)}(\\s|$)")) {
                    commandUsed = command.Key;
                    aliasUsed = alias;
                    args = text.Substring(alias.Length).TrimStart();
                    break;
                }
            }
        }

        if (commandUsed == null) {
            // Mention without a command
            var sentencesResponseMessage = await MessageProcessor.KeepUpConversation(message);
            Answer(message, sentencesResponseMessage);
            // HandleHelpCommand(message);
            return;
        }

        // Command detected
        Debug.Assert(args != null);
        Debug.Assert(aliasUsed != null);

        var sb2 = new StringBuilder($"Name: '{nameUsed}', Alias: '{aliasUsed}'");
        if (args is { Length: > 0 }) {
            sb2.Append($", Args: '{string.Join("' '", args)}'");
        }

        L.D(sb2);

        switch (commandUsed) {
            case "meme":
                HandleMemeCommand(message, args);
                return;
            case "weather":
                HandleWeatherCommand(message, args);
                return;
            case "hentai":
                HandleHCommand(message, args);
                return;
            case "anime":
                HandleAnimeCommand(message, args);
                return;
            case "where":
                HandleSearchCommand(message, args);
                return;
            case "help":
                HandleHelpCommand(message);
                return;
            case "settings":
                HandleSettingsCommand(message);
                return;
            case "py":
                HandlePythonCommand(message);
                return;
            case "generate_sentences":
                var sentencesResponseMessage = await MessageProcessor.KeepUpConversation();
                Answer(message, sentencesResponseMessage);
                return;
            case "echo":
                Answer(message, message.Text);
                return;
            case "chaos":
                HandleChaosCommand(message);
                return;
            case "who":
                HandleWhoCommand(message, aliasUsed, args);
                return;
            case "wiki":
                HandleWikiCommand(message, args);
                return;
            case "what":
                HandleWhatCommand(message, args);
                return;
            case "funeral":
                HandleFuneralCommand(message);
                return;
            case "why":
                Answer(message, "Потому что твоя мама жирная.");
                return;
            case "test":
                L.I("asd");
                var photo = await _vk.UploadImageFrom("https://cdn.donmai.us/original/ee/df/eedf2c5b58f49c83c06c359b45c592ec.jpg",
                    new HttpClient());
                _vk.Api.Messages.Send(new MessagesSendParams {
                    RandomId = Rand.Next(),
                    PeerId = message.PeerId!.Value,
                    ReplyTo = message.Id,
                    Message = "asd",
                    Attachments = photo
                });
                return;
            default:
                HandlePhotoCommand(message, commandUsed);
                return;
        }
    }

    private void MessageSaving(Message message) {
        _saves.AddChat(message.PeerId!.Value);
        _saves.AddUserToChat(message.PeerId!.Value, message.FromId.Value);
        _saves.Save();
    }

    [Obsolete]
    private void Answer(long peerId, string text) {
        if (text is not { Length: > 0 }) {
            L.E("SendResponse: Message is empty");
            return;
        }

        try {
            _vk.Api.Messages.Send(new MessagesSendParams {
                RandomId = Rand.Next(),
                PeerId = peerId,
                Message = text
            });
            L.I($"Sent response: {text}");
        } catch (Exception e) {
            L.E("Failed to send response", e);
        }
    }

    private void Answer(Message message, string text, long? replyTo = null) {
        if (text is not { Length: > 0 }) {
            L.E("Answer: text is empty");
            return;
        }

        try {
            _vk.Api.Messages.Send(new MessagesSendParams {
                RandomId = Rand.Next(),
                PeerId = message.PeerId,
                Message = text,
                ReplyTo = replyTo
            });
            L.I($"Sent response: {text}");
        } catch (Exception e) {
            L.E("Failed to send response", e);
        }
    }

    // Метод для отправки периодических сообщений
    private async void SendPeriodicMessages(object state) {
        try {
            if (isEnabled) {
                var threads = await ServiceEndpoint.Wakaba.GetThreads();
                if (threads != null && threads.Count > 0) {
                    var randomThreadIndex = Rand.Next(threads.Count);
                    var selectedThread = threads[randomThreadIndex];
                    var randomComment = selectedThread.Comment;

                    if (!string.IsNullOrEmpty(randomComment)) {
                        // Удаление HTML-тегов
                        randomComment = StripHtml(randomComment);

                        // Обрезка сообщения до 999 символов
                        if (randomComment.Length > 999) {
                            randomComment = randomComment.Substring(0, 999);
                        }

                        foreach (var chat in _saves.Chats) {
                            // Проверка, включены ли периодические сообщения для этого чата
                            if (chat.Properties.IsMeme) {
                                Answer(chat.PeerId, randomComment);
                            }
                        }
                    }
                }
            }
        } catch (Exception ex) {
            L.I($"Error sending periodic message: {ex.Message}");
        }
    }

    private string StripHtml(string input) {
        return Regex.Replace(HttpUtility.HtmlDecode(input), "<.*?>", string.Empty);
    }
}