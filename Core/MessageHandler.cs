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
    private Conf _config;
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

    public void HandleMessage(VkApi api, Message message, Conf config, ulong groupId)
    {
        this._vkApi = api;
        this._config = config;
        this._groupId = groupId;

        if (api == null || message == null || config == null)
        {
            Logger.M("API, message, or config is null");
            return;
        }

        if (IsDebug)
        {
            Logger.M("New message...");
            message.Out();
        }

        _saves.AddChat(message.PeerId.Value);
        _saves.AddUserToChat(message.PeerId.Value, message.FromId.Value);
        _saves.Save(SavesFilePath);

        UserRequest request = new UserRequest(message, config);

        request.onPayload = (string payload) =>
        {
            HandlePayload(api, request.Message, groupId);
        };

        request.onSimpleText = async (message) =>
        {
            if (_random.NextDouble() < config.ResponseProbability)
            {
                var responseMessage = await MessageProcessor.KeepUpConversation(message);
                SendResponse(api, message.PeerId.Value, responseMessage);
            }
        };

        request.onCommand = async ((string command, string args) commargs) =>
        {
            Console.WriteLine($"{commargs.command}, args:{commargs.args}");
            switch (commargs.command)
            {
                case "meme":
                    if (commargs.args != null)
                    {
                        HandleMemeCommand(api, message, groupId, commargs.args);
                    }
                    else
                    {
                        // say about it
                    }
                    return;
                case "weather":
                    if (commargs.args != null)
                    {
                        HandleWeatherCommand(api, message, commargs.args);
                    }
                    else
                    {
                        // say about it
                    }
                    return;
                case "hentai":
                    HandleHCommand(api, message, groupId);
                    return;
                case "anime":
                    Logger.M("HandleAnimeCommand(api, message, groupId);");
                    HandleAnimeCommand(api, message, groupId);

                    return;
                case "where":
                    if (commargs.args != null)
                    {
                        HandleSearchCommand(api, message, groupId, commargs.args);
                    }
                    else
                    {
                        // say about it
                    }
                    return;
                case "help":
                    HandleHelpCommand(api, message, groupId);
                    return;
                case "settings":
                    HandleSettingsCommand(api, message, groupId);
                    return;
                case "break":
                case "liquidate":
                case "compress":
                case "add_text":
                    await HandlePhotoCommand(api, message, groupId, request.Text, commargs.command, config);
                    return;
                case "generate_sentences":
                    var sentencesResponseMessage = await MessageProcessor.KeepUpConversation();
                    SendResponse(api, message.PeerId.Value, sentencesResponseMessage);
                    return;
                case "echo":
                    SendResponse(api, message.PeerId.Value, request.Text);
                    return;
                default:
                    var defaultMessage = await MessageProcessor.KeepUpConversation();
                    SendResponse(api, message.PeerId.Value, defaultMessage);
                    return;
            }
        };

        request.Init();
    }

    private void SendResponse(VkApi api, long? peerId, string message)
    {
        if (peerId != null)
        {
            api.Messages.Send(new MessagesSendParams
            {
                RandomId = _random.Next(),
                PeerId = peerId,
                Message = message
            });
            Console.WriteLine($"Sent response: {message}");
            File.AppendAllText("./log.txt", $"Sent response: {message}\n");
        }
        else
        {
            Console.WriteLine("peerId is NULL");
        }
    }

    private void SendResponse(VkApi api, long? peerId, string message, long? replyTo)
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
            Console.WriteLine($"Sent response: {message}");
            File.AppendAllText("./log.txt", $"Sent response: {message}\n");
        }
        else
        {
            Console.WriteLine("peerId is NULL");
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
            Console.WriteLine($"Error sending periodic message: {ex.Message}");
            File.AppendAllText("./log.txt", $"Error sending periodic message: {ex.Message}\n");
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