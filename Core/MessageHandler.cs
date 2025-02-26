using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Web;
using vkbot_vitalya.Config;
using vkbot_vitalya.Core;
using vkbot_vitalya.Services.Generators;
using vkbot_vitalya.Services.Generators.TextGeneration;
using VkNet;
using VkNet.Model;

namespace vkbot_vitalya;

public partial class MessageHandler
{
    private Random _random = new Random();
    private Timer _messageTimer, _updateTimer;
    private VkApi _vkApi;
    private Conf? _config = Conf.Instance;
    private ulong _groupId;
    private Saves _saves;
    private List<UserRequest> _userRequests;

    public bool isEnabled = true;
    private const string SavesFilePath = "./saves.json";

    public MessageHandler(Authentication auth, bool isDebug)
    {
        Auth = auth;
        ServiceEndpoint = new ServiceEndpoint(Auth);
        Processor = new ImageProcessor();
        _saves = Saves.Load(SavesFilePath);
        IsDebug = isDebug;
        _updateTimer = new Timer(Update, null, TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(10));
        _messageTimer = new Timer(SendPeriodicMessages, null, TimeSpan.FromMinutes(60 * 2), TimeSpan.FromMinutes(60 * 2));

        _userRequests = new List<UserRequest>();
    }

    private ServiceEndpoint ServiceEndpoint { get; set; }
    private ImageProcessor Processor { get; set; }
    private Authentication Auth { get; set; }
    private bool IsDebug { get; set; }

    /// <summary>
    /// Метод загрузки параметров сообщения
    /// </summary>
    public void HandleMessage(VkApi api, Message message, ulong groupId)
    {
        this._vkApi = api;
        this._groupId = groupId;

        if (api == null || message == null)
        {
            L.M("API, message, or config is null");
            return;
        }

        if (IsDebug)
        {
            L.M("New message...");
            message.Out();
        }

        MessageSaving(message);

        UserRequest userMessage = new UserRequest(message, _config);

        userMessage.onPayload = (string payload) =>
        {
            HandlePayload(api, userMessage.Message, groupId);
        };

        userMessage.onSimpleText = async (message) =>
        {
            if (_random.NextDouble() < _config.ResponseProbability)
            {
                var responseMessage = await MessageProcessor.KeepUpConversation(message);
                SendResponse(api, message.PeerId.Value, responseMessage);
            }
        };

        userMessage.onCommand = async (Cmd cmd) =>
        {
            switch (cmd.CommandName)
            {
                case "meme":
                    HandleMemeCommand(api, message, groupId, cmd.Args);

                    return;
                case "weather":
                    HandleWeatherCommand(api, message, cmd.Args);

                    return;
                case "hentai":
                    HandleHCommand(api, message, groupId);

                    return;
                case "anime":
                    HandleAnimeCommand(api, message, groupId);

                    return;
                case "where":
                    HandleSearchCommand(api, message, groupId, cmd.Args);

                    return;
                case "help":
                    HandleHelpCommand(api, message, groupId);
                    return;
                case "settings":
                    HandleSettingsCommand(api, message, groupId);

                    return;
                case "py":
                    L.M("Command 'Python' recognized.");
                    HandlePythonCommand(api, message, groupId);
                    return;

                case "generate_sentences":
                    var sentencesResponseMessage = await MessageProcessor.KeepUpConversation();
                    SendResponse(api, message.PeerId.Value, sentencesResponseMessage);
                    return;
                case "echo":
                    SendResponse(api, message.PeerId.Value, userMessage.Text);
                    return;

                case "chaos":
                    L.M("Command 'Chaos' recognized.");
                    HandleChaosCommand(api, message, groupId);
                    return;
                default:
                    /* Больше некуда это вставлять */
                    await HandlePhotoCommand(api, message, groupId, cmd.CommandName, _config);
                    return;
            }
        };

        userMessage.Init();
    }

    private void MessageSaving(Message message)
    {
        _saves.AddChat(message.PeerId.Value);
        _saves.AddUserToChat(message.PeerId.Value, message.FromId.Value);
        _saves.Save(SavesFilePath);
    }

    private void SendResponse(VkApi api, long? peerId, string message)
    {
        try
        {
            if (peerId != null)
            {
                api.Messages.Send(new MessagesSendParams
                {
                    RandomId = _random.Next(),
                    PeerId = peerId,
                    Message = message
                });
                L.M($"Sent response: {message}");
            }
            else
            {
                Console.WriteLine("peerId is NULL");
            }
        }
        catch (Exception ex)
        {
            L.E(ex);
        }
    }

    private void SendResponse(VkApi api, long? peerId, string message, long? replyTo)
    {
        try
        {
            if (peerId != null)
            {
                api.Messages.Send(new MessagesSendParams
                {
                    RandomId = _random.Next(),
                    PeerId = peerId,
                    ReplyTo = replyTo,
                    Message = message
                });
                L.M($"Sent response: {message}");
            }
            else
            {
                Console.WriteLine("peerId is NULL");
            }
        }
        catch (Exception ex)
        {
            L.E(ex);
        }
    }

    // Метод для отправки периодических сообщений
    private async void SendPeriodicMessages(object state)
    {
        try
        {
            if (_vkApi != null && _config != null && isEnabled)
            {
                var threads = await ServiceEndpoint.Wakaba.GetThreads();
                if (threads != null && threads.Count > 0)
                {
                    var randomThreadIndex = _random.Next(threads.Count);
                    var selectedThread = threads[randomThreadIndex];
                    var randomComment = selectedThread.Comment;

                    if (!string.IsNullOrEmpty(randomComment))
                    {
                        // Удаление HTML-тегов
                        randomComment = StripHtml(randomComment);

                        // Обрезка сообщения до 999 символов
                        if (randomComment.Length > 999)
                        {
                            randomComment = randomComment.Substring(0, 999);
                        }

                        foreach (var chat in _saves.Chats)
                        {
                            // Проверка, включены ли периодические сообщения для этого чата
                            if (chat.Propertyes.IsMeme)
                            {
                                SendResponse(_vkApi, chat.PeerID, randomComment);
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            L.M($"Error sending periodic message: {ex.Message}");
        }
    }

    private void Update(object state)
    {
        _userRequests.Clear();
    }

    private string StripHtml(string input)
    {
        return Regex.Replace(HttpUtility.HtmlDecode(input), "<.*?>", string.Empty);
    }
}