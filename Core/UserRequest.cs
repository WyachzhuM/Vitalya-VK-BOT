using vkbot_vitalya.Config;
using vkbot_vitalya.Core;
using VkNet.Model;

namespace vkbot_vitalya;

public class UserRequest
{
    public Action<Message>? onSimpleText;
    public Action<(string command, string args)>? onCommand;
    public Action<string>? onPayload;

    private Random random = new Random();

    public UserRequest(Message message, Conf config)
    {
        ID = random.Next(int.MaxValue);
        Config = config;

        Message = message;
        Text = Message.Text.ToLower().Trim();

        Payload = Message.Payload ?? null;

        foreach (string name in Config.BotNames)
        {
            if (Text.StartsWith(name))
            {
                BotNameUsed = name;
                break;
            }
        }

        if (BotNameUsed != null)
        {
            foreach (var cmd in config.Commands)
            {
                foreach (var value in cmd.Value)
                {
                    if (Text.Replace(BotNameUsed, string.Empty).Trim().StartsWith(value))
                    {
                        ActualCommand = value;
                        Command = cmd.Key;
                        break;
                    }
                }
            }

            if (ActualCommand != null)
            {
                int commandStartIndex = BotNameUsed.Length + ActualCommand.Length + 2;

                if (commandStartIndex <= Text.Length)
                {
                    Keywords = Text.Substring(commandStartIndex).Trim();
                }
                else
                {
                    Keywords = string.Empty; // или null
                }
            }
        }

        Console.WriteLine(this);
    }

    public void Init()
    {
        if (Payload != null)
        {
            Logger.M("onPayload?.Invoke");
            onPayload?.Invoke(Payload);
        }
        else if (BotNameUsed != null && ActualCommand != null)
        {
            Logger.M($"onCommand?.Invoke({Command}, {Keywords})");
            onCommand?.Invoke((Command, Keywords));
        }
        else
        {
            Logger.M("onSimpleText?.Invoke");
            onSimpleText?.Invoke(Message);
        }
    }

    public Message Message { get; private set; }

    public string Text { get; set; }

    /// <summary>
    /// From Config file with Dictionary.Value
    /// </summary>
    public string? Command { get; set; }

    /// <summary>
    /// From message
    /// </summary>
    public string? ActualCommand { get; set; }

    public string? Keywords { get; set; }

    public string? Payload { get; set; }

    public string? BotNameUsed { get; set; }

    private Int32 ID { get; set; }

    private Conf Config { get; set; }

    private bool GetBotName(string fulltext, out string BotNameUsed)
    {
        if (!Config.BotNames.Any(fulltext.StartsWith))
        {
            BotNameUsed = Config.BotNames.First(fulltext.StartsWith);
            return true;
        }
        BotNameUsed = string.Empty;

        return false;
    }

    public override string ToString()
    {
        return $"ID: '{ID}', Text: '{Text}', Command: '{Command}', ActualCommand: '{ActualCommand}', Keywords: '{Keywords}', BotNameUsed: '{BotNameUsed}',\nPayload:\n{Payload}\n";
    }
}
