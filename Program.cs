using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;
using VkNet;
using VkNet.Enums.Filters;
using VkNet.Exception;
using VkNet.Model;

namespace vkbot_vitalya;

class Program
{
    private static VkApi api;
    private static Config config;
    private static AuthBotFile auth;
    public static readonly string MessagesFilePath = "./messages.txt";
    private static Random random = new Random();
    private static ManualResetEvent _shutdownEvent = new ManualResetEvent(false);
    private static MemeGen memeGen;
    private static WeatherService weatherService;
    private static DanbooruApi danbooruApi;

    public static void Main(string[] args)
    {
        var logFilePath = "./log.txt";
        File.AppendAllText(logFilePath, $"Bot started at {DateTime.Now}\n");

        var authPath = "./auth.json";
        var configPath = "./config.json";

        if (!File.Exists(authPath) || !File.Exists(configPath))
        {
            File.AppendAllText(logFilePath, $"Auth file or config file is missing\n");
            return;
        }

        auth = AuthBotFile.GetAuthBotFileFromJson(authPath);
        config = Config.GetConfigFromJson(configPath);
        danbooruApi = new DanbooruApi(auth);

        if (auth == null || config == null)
        {
            File.AppendAllText(logFilePath, "Auth file or config file is NULL\n");
            return;
        }

        memeGen = new MemeGen(auth);
        weatherService = new WeatherService(auth.WeatherApiKey);

        api = new VkApi();
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

        MessageHandler.Initialize(memeGen, weatherService, danbooruApi); // Initialize handler with the necessary instances

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

                        // Save message to file
                        if(!message.Text.Contains("@"))
                            SaveMessageToFile(message.Text);

                        // Call message handler
                        try
                        {
                            MessageHandler.HandleMessage(api, message, config, groupId);
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
        Console.CancelKeyPress += (sender, e) => {
            e.Cancel = true;
            _shutdownEvent.Set();
        };

        AppDomain.CurrentDomain.ProcessExit += (sender, e) => {
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