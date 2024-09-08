using System.Threading;
using vkbot_vitalya.Config;
using vkbot_vitalya.Core;
using vkbot_vitalya.Services.Generators;
using VkNet;
using VkNet.Model;

namespace vkbot_vitalya;

public partial class MessageHandler
{
    private Random random = new Random();
    private Timer messageTimer;
    private VkApi vkApi;
    private Conf config;
    private ulong groupId;
    private Saves saves;

    public bool isEnabled = true;
    private const string SavesFilePath = "./saves.json";

    public MessageHandler(AuthBotFile auth)
    {
        Auth = auth;
        ServiceEndpoint = new ServiceEndpoint(Auth);
        Processor = new ImageProcessor();
        saves = Saves.Load(SavesFilePath);

        messageTimer = new Timer(SendPeriodicMessages, null, TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(10));
    }

    private ServiceEndpoint ServiceEndpoint { get; set; }
    private ImageProcessor Processor { get; set; }
    private AuthBotFile Auth { get; set; }

    public void HandleMessage(VkApi api, Message message, Conf config, ulong groupId)
    {
        this.vkApi = api;
        this.config = config;
        this.groupId = groupId;

        if (api == null || message == null || config == null)
        {
            Console.WriteLine("API, message, or config is null");
            File.AppendAllText("./log.txt", "API, message, or config is null\n");
            return;
        }

        Console.WriteLine("Handling message...");
        File.AppendAllText("./log.txt", "Handling message...\n");

        message.Out();

        saves.AddChat(message.PeerId.Value);
        saves.AddUserToChat(message.PeerId.Value, message.FromId.Value);
        saves.Save(SavesFilePath);

        var _command = ExtractCommand(message);
        if (config.BotNames.Any(_command.StartsWith))
        {
            var botNameUsed = config.BotNames.First(botName => _command.StartsWith(botName));
            _command = _command.Replace(botNameUsed, "").Trim();

            foreach (var cmd in config.Commands)
            {
                if (cmd.Value.Any(alias => _command.StartsWith(alias)))
                {
                    string actualCommand = cmd.Key;

                    if (actualCommand == "turnoffon")
                    {
                        string pass = _command.Substring(cmd.Value.First().Length).Trim();
                        if (pass == Auth.SystemPassKey) isEnabled = !isEnabled;
                        Console.WriteLine($"isEnabled : {isEnabled}");
                    }
                }
            }
        }

        if (!isEnabled)
        {
            //SendResponse(api, message.PeerId, "Я выключен");
            return;
        }

        // Extract command from message
        var command = ExtractCommand(message);

        if (command == string.Empty)
            return;

        bool isBotAddressed = config.BotNames.Any(botName => command.StartsWith(botName));

        if (isBotAddressed)
        {
            // Identify the actual command
            var botNameUsed = config.BotNames.First(botName => command.StartsWith(botName));
            command = command.Replace(botNameUsed, "").Trim();

            foreach (var cmd in config.Commands)
            {
                if (cmd.Value.Any(alias => command.StartsWith(alias)))
                {
                    string actualCommand = cmd.Key;
                    switch (actualCommand)
                    {
                        case "meme":
                            string keywords = command.Substring(cmd.Value.First().Length).Trim();
                            HandleMemeCommand(api, message, groupId, keywords);
                            return;
                        case "weather":
                            string cityName = command.Substring(cmd.Value.First().Length).Trim();
                            HandleWeatherCommand(api, message, cityName);
                            return;
                        case "anime":
                            HandleAnimeCommand(api, message, groupId);
                            return;
                        case "where":
                            string whereis = command.Substring(cmd.Value.First().Length).Trim();
                            HandleSearchCommand(api, message, groupId, whereis);
                            return;
                        case "help":
                            HandleHelpCommand(api, message, groupId);
                            return;
                        case "break":
                        case "liquidate":
                        case "compress":
                        case "add_text":
                            HandlePhotoCommand(api, message, groupId, command, actualCommand, config);
                            return;
                        case "generate_sentences":
                            var sentencesResponseMessage = MessageProcessor.GenerateMultipleSentences();
                            SendResponse(api, message.PeerId.Value, sentencesResponseMessage);
                            return;
                        case "echo":
                            var echoText = message.Text.Substring(cmd.Value.First().Length).Trim();
                            SendResponse(api, message.PeerId.Value, echoText);
                            return;
                        default:
                            var defaultMessage = MessageProcessor.GenerateRandomMessage();
                            SendResponse(api, message.PeerId.Value, defaultMessage);
                            return;
                    }
                }
            }
        }

        if (random.NextDouble() < config.ResponseProbability)
        {
            var responseMessage = MessageProcessor.GenerateRandomMessage();
            SendResponse(api, message.PeerId.Value, responseMessage);
        }
    }

    private string ExtractCommand(Message message)
    {
        string command = message.Text?.ToLower().Trim();

        if (command == null)
        {
            Console.WriteLine("Command is null");
            File.AppendAllText("./log.txt", "Command is null\n");
            return string.Empty;
        }

        Console.WriteLine($"Command received: {command}");
        File.AppendAllText("./log.txt", $"Command received: {command}\n");

        return command;
    }

    private void SendResponse(VkApi api, long? peerId, string message)
    {
        if (peerId != null)
        {
            api.Messages.Send(new MessagesSendParams
            {
                RandomId = random.Next(),
                PeerId = peerId,
                Message = message
            });
            Console.WriteLine($"Sent response: {message}");
            File.AppendAllText("./log.txt", $"Sent response: {message}\n");
        }
        else
        {
            Console.WriteLine($"peerId is NULL");
        }
    }

    private void SendResponse(VkApi api, long? peerId, string message, long? replyTo)
    {
        if (peerId != null)
        {
            api.Messages.Send(new MessagesSendParams
            {
                RandomId = random.Next(),
                PeerId = peerId,
                ReplyTo = replyTo,
                Message = message
            });
            Console.WriteLine($"Sent response: {message}");
            File.AppendAllText("./log.txt", $"Sent response: {message}\n");
        }
        else
        {
            Console.WriteLine($"peerId is NULL");
        }
    }

    private async void SendPeriodicMessages(object state)
    {
        try
        {
            if (vkApi != null && config != null && isEnabled)
            {
                var threads = await ServiceEndpoint.Wakaba.GetThreads();
                if (threads != null && threads.Count > 0)
                {
                    var randomThreadIndex = random.Next(threads.Count);
                    var selectedThread = threads[randomThreadIndex];
                    var randomComment = selectedThread.Comment;

                    foreach (var chat in saves.Chats)
                    {
                        if (!string.IsNullOrEmpty(randomComment))
                        {
                            SendResponse(vkApi, chat.PeerID, randomComment);
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
}