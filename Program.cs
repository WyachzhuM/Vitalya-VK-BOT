using vkbot_vitalya.Config;
using VkNet;
using VkNet.Exception;
using VkNet.Model;

namespace vkbot_vitalya;

public static class Program
{
    private static VkApi api = new VkApi();
    private static Conf config;
    private static Authentication auth;
    private static MessageHandler messageHandler;
    public static readonly string MessagesFilePath = "./messages.txt";

    private static ManualResetEvent _shutdownEvent = new ManualResetEvent(false);

    public static void Main(string[] args)
    {
        Console.ForegroundColor = ConsoleColor.White;

        var logFilePath = "./log.txt";
        File.AppendAllText(logFilePath, $"Bot started at {DateTime.Now}\n");

        var authPath = "./auth.json";
        var configPath = "./config.json";

        if (!File.Exists(authPath) || !File.Exists(configPath))
        {
            File.AppendAllText(logFilePath, $"Auth file or config file is missing\n");
            return;
        }

        auth = Authentication.GetAuthBotFileFromJson(authPath);
        config = Conf.GetConfigFromJson(configPath);

        if (auth == null || config == null)
        {
            File.AppendAllText(logFilePath, "Auth file or config file is NULL\n");
            return;
        }

        messageHandler = new MessageHandler(auth, true);

        try
        {
            api.Authorize(new ApiAuthParams { AccessToken = auth.AccessToken });
            File.AppendAllText(logFilePath, "Authorization successful\n");
        }
        catch (Exception ex)
        {
            File.AppendAllText(logFilePath, $"Authorization failed: {ex.Message}\n");
            Console.WriteLine($"Authorization failed: {ex.Message}");
            return;
        }

        File.AppendAllText(logFilePath, "Bot is running...\n");
        Console.WriteLine("Bot is running...");

        SetupSignalHandling();

        try
        {
            var longPollServer = api.Groups.GetLongPollServer(auth.GroupId);
            StartLongPoll(longPollServer, auth.GroupId);
        }
        catch (Exception ex)
        {
            File.AppendAllText(logFilePath, $"Error in long poll: {ex.Message}\n");
            Console.WriteLine($"Error in long poll: {ex.Message}");
            return;
        }

        // Keep the application running
        File.AppendAllText(logFilePath, "Entering infinite loop, waiting for termination signal\n");
        _shutdownEvent.WaitOne();
        File.AppendAllText(logFilePath, $"Bot stopped at {DateTime.Now}\n");
    }

    private static async void StartLongPoll(LongPollServerResponse longPollServer, ulong groupId)
    {
        var lastUpdateTs = longPollServer.Ts;

        while (true)
        {
            try
            {
                var poll = await api.Groups.GetBotsLongPollHistoryAsync(
                    new BotsLongPollHistoryParams
                    {
                        Server = longPollServer.Server,
                        Ts = lastUpdateTs,
                        Key = longPollServer.Key,
                        Wait = 25
                    });

                if (poll?.Updates == null) continue;

                foreach (var update in poll.Updates)
                {
                    if (update.Instance is MessageNew messageNew)
                    {
                        var message = messageNew.Message;
                        Console.WriteLine($"New message from {message.FromId}: {message.Text}");
                        File.AppendAllText("./log.txt", $"New message from {message.FromId}: {message.Text}\n");

                        bool isBotAddressed = config.BotNames.Any(message.Text.StartsWith);
                        // Save message to file
                        if (!isBotAddressed)
                            SaveMessageToFile(message.Text);

                        // Call message handler
                        try
                        {
                            messageHandler.HandleMessage(api, message, config, groupId);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error in HandleMessage: {ex.Message}");
                            File.AppendAllText("./log.txt", $"Error in HandleMessage: {ex.Message}\n");
                            File.AppendAllText("./log.txt", $"Stack Trace: {ex.StackTrace}\n");
                        }
                    }
                }

                // Update Ts for the next request
                lastUpdateTs = poll.Ts;
            }
            catch (LongPollKeyExpiredException)
            {
                // Refresh longPollServer for correct work
                longPollServer = api.Groups.GetLongPollServer(groupId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                File.AppendAllText("./log.txt", $"Error: {ex.Message}\n");
            }
        }
    }

    private static void SetupSignalHandling()
    {
        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true;
            _shutdownEvent.Set();
        };

        AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
        {
            _shutdownEvent.Set();
        };
    }

    private static void SaveMessageToFile(string message)
    {
        using (StreamWriter sw = new StreamWriter(MessagesFilePath, true))
        {
            sw.WriteLine(message);
        }
    }
}