using System.Text;
using log4net;
using log4net.Config;
using vkbot_vitalya.Config;
using vkbot_vitalya.Core;
using vkbot_vitalya.Core.Saver;
using VkNet;
using VkNet.Exception;
using VkNet.Model;


namespace vkbot_vitalya;


public static class Program
{
    private static VkApi? _api;
    private static Conf? _config;
    private static Authentication? _authentication;
    private static MessageHandler? _handler;
    private static ExceptDict? _exceptDict;
    private static MessageSaver? _messageSaver;

    public static string _savedMessagesFolder = Path.Combine(Environment.CurrentDirectory, "SavedMessages");
    private static string _authPath = "./auth.json";
    private static string _configPath = "./config.json";

    private static ManualResetEvent _shutdownEvent = new ManualResetEvent(false);

    public static void Main(string[] args) {
        Console.OutputEncoding = Encoding.Unicode;
        Console.ForegroundColor = ConsoleColor.White;

        L.M($"Bot started at {DateTime.Now}");
        if (!File.Exists(_authPath) || !File.Exists(_configPath))
        {
            L.M($"Auth file or config file is missing");
            return;
        }

        _authentication = Authentication.GetAuthBotFileFromJson(_authPath);
        _config = Conf.GetConfigFromJson(_configPath);
        _exceptDict = new ExceptDict(_config);
        _messageSaver = new MessageSaver(_savedMessagesFolder);

        if (_authentication == null || _config == null)
        {
            L.M("Auth file or config file is NULL");
            return;
        }

        _handler = new MessageHandler(_authentication, true);
        _api = new VkApi();

        try
        {
            _api.Authorize(new ApiAuthParams { AccessToken = _authentication.AccessToken });
            L.M("Authorization successful");
        }
        catch (Exception ex)
        {
            L.M($"Authorization failed: {ex.Message}");
            return;
        }
        
        SetupSignalHandling();

        try
        {
            var longPollServer = _api.Groups.GetLongPollServer(_authentication.GroupId);
            StartLongPoll(longPollServer, _authentication.GroupId);
        }
        catch (Exception ex)
        {
            L.M($"Error in long poll: {ex.Message}");
            return;
        }

        _shutdownEvent.WaitOne();
        L.M($"Bot stopped at {DateTime.Now}");
    }

    private static async void StartLongPoll(LongPollServerResponse longPollServer, ulong groupId)
    {
        var lastUpdateTs = longPollServer.Ts;

        if (_api == null || _handler == null || _config == null || _exceptDict == null)
            return;

        while (true)
        {
            try
            {
                var poll = await _api.Groups.GetBotsLongPollHistoryAsync(
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

                        bool dontSave = _exceptDict.GetExceptions().Any(message.Text.StartsWith);

                        // Save message to file
                        if (!dontSave && _messageSaver != null)
                            await _messageSaver.SaveMessage(message);

                        // Call message handler
                        try
                        {
                            _handler.HandleMessage(_api, message, groupId);
                        }
                        catch (Exception ex)
                        {
                            L.E($"Error in HandleMessage: {ex.Message}");
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
                longPollServer = _api.Groups.GetLongPollServer(groupId);
            }
            catch (Exception ex)
            {
                L.M($"Error: {ex.Message}");
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
}
