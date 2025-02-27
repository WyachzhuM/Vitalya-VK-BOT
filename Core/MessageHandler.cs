using System.Text.RegularExpressions;
using System.Web;
using vkbot_vitalya.Config;
using vkbot_vitalya.Core;
using vkbot_vitalya.Services.Generators;
using vkbot_vitalya.Services.Generators.TextGeneration;
using VkNet.Model;

namespace vkbot_vitalya;

public partial class MessageHandler {
    private static readonly Random Rand = new Random();
    private readonly Vk _vk;
    private Saves _saves;

    [Obsolete] private Timer _messageTimer, _updateTimer;

    public bool isEnabled = true;
    private const string SavesFilePath = "./saves.json";

    public MessageHandler(Vk vk) {
        _vk = vk;
        ServiceEndpoint = new ServiceEndpoint();
        Processor = new ImageProcessor();
        _saves = Saves.Load(SavesFilePath);
        // _updateTimer = new Timer(Update, null, TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(10));
        // _messageTimer = new Timer(SendPeriodicMessages, null, TimeSpan.FromMinutes(60 * 2), TimeSpan.FromMinutes(60 * 2));
    }

    private ServiceEndpoint ServiceEndpoint { get; set; }
    private ImageProcessor Processor { get; set; }

    public void HandleMessage(Message message) {
        message.Out();
        MessageSaving(message);

        var userRequest = new UserRequest(message);

        userRequest.onPayload = payload => HandlePayload(userRequest.Message);

        userRequest.onSimpleText = async message => {
            if (Rand.NextDouble() < Conf.Instance.ResponseProbability) {
                var responseMessage = await MessageProcessor.KeepUpConversation(message);
                Answer(message.PeerId!.Value, responseMessage);
            }
        };

        userRequest.onCommand = async cmd => {
            switch (cmd.CommandName) {
                case "meme":
                    HandleMemeCommand(message, cmd.Args);

                    return;
                case "weather":
                    HandleWeatherCommand(message, cmd.Args);

                    return;
                case "hentai":
                    HandleHCommand(message);

                    return;
                case "anime":
                    HandleAnimeCommand(message);

                    return;
                case "where":
                    HandleSearchCommand(message, cmd.Args);

                    return;
                case "help":
                    HandleHelpCommand(message);
                    return;
                case "settings":
                    HandleSettingsCommand(message);

                    return;
                case "py":
                    L.I("Command 'Python' recognized.");
                    HandlePythonCommand(message);
                    return;

                case "generate_sentences":
                    var sentencesResponseMessage = await MessageProcessor.KeepUpConversation();
                    Answer(message.PeerId!.Value, sentencesResponseMessage);
                    return;
                case "echo":
                    Answer(message.PeerId!.Value, userRequest.Text);
                    return;

                case "chaos":
                    L.I("Command 'Chaos' recognized.");
                    HandleChaosCommand(message);
                    return;

                case "who":
                    HandleWhoCommand(userRequest);
                    return;

                case "wiki":
                    HandleWikiCommand(message, cmd.Args);
                    return;
                default:
                    /* Больше некуда это вставлять */
                    await HandlePhotoCommand(message, cmd.CommandName);
                    return;
            }
        };

        if (userRequest.Payload != null) {
            L.I("Got payload");
            userRequest.onPayload?.Invoke(userRequest.Payload);
        } else if (userRequest.BotNameUsed != null && userRequest.Alias != null) {
            L.I($"Got command: '{userRequest.Command}' '{string.Join("' '", userRequest.Args)}'.");
            if (userRequest.Command != null && userRequest.Keywords != null)
                userRequest.onCommand?.Invoke(new Cmd(userRequest.Command, userRequest.Keywords));
        } else {
            userRequest.onSimpleText?.Invoke(userRequest.Message);
        }
    }

    private void MessageSaving(Message message) {
        _saves.AddChat(message.PeerId!.Value);
        _saves.AddUserToChat(message.PeerId!.Value, message.FromId.Value);
        _saves.Save(SavesFilePath);
    }

    private void Answer(long peerId, string text, long? replyTo = null) {
        if (text is not { Length: > 0 }) {
            L.E("SendResponse: Message is empty");
            return;
        }

        try {
            _vk.Api.Messages.Send(new MessagesSendParams {
                RandomId = Rand.Next(),
                PeerId = peerId,
                Message = text,
                ReplyTo = replyTo
            });
            L.I($"Sent response: {text}");
        } catch (Exception e) {
            L.E("Failed to send response", e);
        }
    }

    private void Answer(Message message, string text, long? replyTo = null) {
        Answer(message.PeerId!.Value, text, replyTo);
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