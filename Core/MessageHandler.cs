using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using vkbot_vitalya.Config;
using vkbot_vitalya.Core;
using vkbot_vitalya.Services.Generators;
using vkbot_vitalya.Services.Generators.TextGeneration;
using VkNet.Model;

namespace vkbot_vitalya;

/// <summary>
///     Обработчик сообщений для одного бота на все чаты
/// </summary>
public partial class MessageHandler {
    private static readonly Random Rand = new();
    private static readonly bool PeriodicMessagesEnabled = true;
    private static readonly Dictionary<string, CommandHandler> CommandHandlers = [];
    private readonly Bot _bot;
    [Obsolete] private Timer _messageTimer, _updateTimer;


    public MessageHandler(Bot bot) {
        _bot = bot;
        ServiceEndpoint = new ServiceEndpoint();
        // _updateTimer = new Timer(Update, null, TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(10));
        // _messageTimer = new Timer(SendPeriodicMessages, null, TimeSpan.FromMinutes(60 * 2), TimeSpan.FromMinutes(60 * 2));

        AddCommand("meme", HandleMemeCommand);
        AddCommand("weather", HandleWeatherCommand);
        AddCommand("hentai", HandleHCommand);
        AddCommand("anime", HandleAnimeCommand);
        AddCommand("where", HandleSearchCommand);
        AddCommand("help", HandleHelpCommand);
        AddCommand("settings", HandleSettingsCommand);
        // AddCommand("py", HandlePythonCommand);
        AddCommand("echo", (message, alias, args) => AnswerAsync(message, message.Text));
        AddCommand("chaos", HandleChaosCommand);
        AddCommand("who", HandleWhoCommand);
        AddCommand("wiki", HandleWikiCommand);
        AddCommand("what", HandleWhatCommand);
        AddCommand("funeral", HandleFuneralCommand);
        AddCommand("why", (message, alias, args) => AnswerAsync(message, "Потому что твоя мама жирная"));
        AddCommand("test", (message, alias, args) => Task.CompletedTask);
        AddCommand("update_chat", HandleUpdateChat);
        AddCommand("break", (message, alias, args) => HandleImageCommand(message, ImageProcessor.BreakImage));
        AddCommand("liquidate", (message, alias, args) => HandleImageCommand(message, ImageProcessor.LiquidateImage));
        AddCommand("compress", (message, alias, args) => HandleImageCommand(message, ImageProcessor.CompressImage));
        AddCommand("add_text",
            (message, alias, args) => HandleImageCommand(message, ImageProcessor.AddTextImageCommand));
        AddCommand("generate_sentences", async (message, alias, args) => {
            var text = await MessageProcessor.KeepUpConversation();
            Answer(message, text);
        });
    }

    private ServiceEndpoint ServiceEndpoint { get; }

    private static void AddCommand(string command, CommandHandler handler) {
        Debug.Assert(!CommandHandlers.ContainsKey(command));
        Debug.Assert(!CommandHandlers.ContainsValue(handler));
        CommandHandlers[command] = handler;
    }

    public async Task HandleMessage(Message message) {
        var chatCache = _bot.Saves.Chats.FirstOrDefault(c => c.PeerId == message.PeerId);
        if (chatCache == null) {
            _bot.Saves.AddChat(message.PeerId);
            await _bot.UpdateChat(message.PeerId);
            chatCache = _bot.Saves.Chats.FirstOrDefault(c => c.PeerId == message.PeerId)!;
        }

        var user = chatCache.Users.FirstOrDefault(u => u.Id == message.FromId);
        if (user == null) {
            await _bot.UpdateChat(message.PeerId);
            user = chatCache.Users.FirstOrDefault(u => u.Id == message.FromId)!;
        }

        var sender = user.ToString();
        var sb = new StringBuilder();
        if (message.PeerId != message.FromId)
            sb.Append($"[{message.PeerId}] ");
        sb.Append($"<{sender}> {message.Text}");
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

        var text = Regex.Replace(message.Text, @"\s+", " ").Trim();
        var requireBotName = message.PeerId > 2000000000
                             && message.ReplyMessage?.FromId != -(long)Auth.Instance.GroupId;

        string? botName = null;
        string? command = null;
        string? alias = null;
        string? args = null;

        botName = Conf.Instance.BotNames
            .FirstOrDefault(name => Regex.IsMatch(text.ToLower(), $"^{Regex.Escape(name)}(\\s|$)"));
        if (botName != null) {
            text = text[botName.Length..].TrimStart();
        }


        if (requireBotName && botName == null) {
            // Simple text
            if (Rand.NextSingle() < _bot.Saves.GetChat(message.PeerId).Properties.ResponseProbability) {
                var responseMessage = await MessageProcessor.KeepUpConversation(message);
                Answer(message, responseMessage);
            }

            return;
        }

        foreach (var knownCommand in Conf.Instance.Commands) {
            foreach (var knownAlias in knownCommand.Value) {
                if (Regex.IsMatch(text.ToLower(), $"^{Regex.Escape(knownAlias)}(\\s|$)")) {
                    command = knownCommand.Key;
                    alias = knownAlias;
                    args = text[knownAlias.Length..].TrimStart();
                    break;
                }
            }
        }

        if (command == null) {
            // Mention without a command
            var answertext = await MessageProcessor.KeepUpConversation(message);
            Answer(message, answertext);
            return;
        }

        // Command detected
        Debug.Assert(args != null);
        Debug.Assert(alias != null);

        var sb2 = new StringBuilder($"Name: '{botName}', Alias: '{alias}'");
        if (args is { Length: > 0 }) {
            sb2.Append($", Args: '{string.Join("' '", args)}'");
        }

        L.D(sb2);

        if (CommandHandlers.TryGetValue(command, out var value)) {
            try {
                await value.Invoke(message, alias, args);
            } catch (Exception e) {
                L.E($"Failed to handle '{command} {args}'", e);
            }
        } else {
            L.E($"No command handler for '{command}'");
        }
    }


    [Obsolete]
    private void SendMessage(long peerId, string text) {
        if (text is not { Length: > 0 }) {
            L.E("SendResponse: Message is empty");
            return;
        }

        try {
            _bot.Api.Messages.Send(new MessagesSendParams {
                RandomId = Rand.Next(),
                PeerId = peerId,
                Message = text
            });
            L.I($"Sent response: {text}");
        } catch (Exception e) {
            L.E("Failed to send response", e);
        }
    }

    private async Task AnswerAsync(Message message, string text, long? replyTo = null) {
        if (text is not { Length: > 0 }) {
            L.E("Answer: text is empty");
            return;
        }

        try {
            await _bot.Api.Messages.SendAsync(new MessagesSendParams {
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

    private void Answer(Message message, string text, long? replyTo = null) {
        if (text is not { Length: > 0 }) {
            L.E("Answer: text is empty");
            return;
        }

        try {
            _bot.Api.Messages.Send(new MessagesSendParams {
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
            if (!PeriodicMessagesEnabled) return;
            var threads = await ServiceEndpoint.Wakaba.GetThreads();
            if (threads is not { Count: > 0 }) return;

            var randomThreadIndex = Rand.Next(threads.Count);
            var selectedThread = threads[randomThreadIndex];
            var randomComment = selectedThread.Comment;

            if (string.IsNullOrEmpty(randomComment)) return;

            // Удаление HTML-тегов
            randomComment = StripHtml(randomComment);

            // Обрезка сообщения до 999 символов
            if (randomComment.Length > 999) randomComment = randomComment.Substring(0, 999);

            foreach (var chat in _bot.Saves.Chats) {
                // Проверка, включены ли периодические сообщения для этого чата
                if (chat.Properties.IsMeme)
                    SendMessage(chat.PeerId, randomComment);
            }
        } catch (Exception e) {
            L.I($"Error sending periodic message: {e.Message}");
        }
    }

    private static string StripHtml(string input) {
        return Regex.Replace(HttpUtility.HtmlDecode(input), "<.*?>", string.Empty);
    }

    private delegate Task CommandHandler(Message message, string alias, string args);
}