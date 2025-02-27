using System.Text;
using System.Text.RegularExpressions;
using vkbot_vitalya.Config;
using vkbot_vitalya.Core;
using VkNet.Model;

namespace vkbot_vitalya;

public class Cmd {
    public Cmd(string commandName, string args) {
        CommandName = commandName;
        Args = args;
    }

    public string CommandName { get; set; }
    public string Args { get; set; }
}

public class UserRequest {
    public Action<Message>? onSimpleText;
    public Action<Cmd>? onCommand;
    public Action<string>? onPayload;

    public UserRequest(Message message) {
        Message = message;
        Text = Regex.Replace(Message.Text, @"\s+", " ").Trim();
        Payload = Message.Payload ?? null;

        foreach (var name in Conf.Instance.BotNames) {
            if (Text.StartsWith(name)) {
                BotNameUsed = name;
                break;
            }
        }

        if (BotNameUsed != null) {
            string request;
            try {
                request = Text.Substring(BotNameUsed.Length + 1);
            } catch (ArgumentOutOfRangeException) {
                goto end;
            }

            foreach (var command in Conf.Instance.Commands) {
                foreach (var alias in command.Value) {
                    if (request.StartsWith(alias)) {
                        Alias = alias;
                        Command = command.Key;
                        break;
                    }
                }
            }

            if (Command != null) {
                try {
                    Keywords = request.Substring(Alias!.Length + 1);
                    Args = Keywords.Split(' ');
                } catch (ArgumentOutOfRangeException) {
                    Keywords = string.Empty;
                    Args = [];
                }
            }
        }

        end:
        L.I(this);
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
    public string? Alias { get; set; }

    public string? Keywords { get; set; }
    public string[]? Args { get; set; }
    public string? Payload { get; set; }
    public string? BotNameUsed { get; set; }

    public override string ToString() {
        var sb = new StringBuilder().Append($"Text: '{Text}'");
        if (Command != null) {
            sb.Append($", Command: '{Command}'");
        }

        if (Alias != null) {
            sb.Append($", ActualCommand: '{Alias}'");
        }

        if (Keywords is { Length: > 0 }) {
            sb.Append($", Keywords: '{Keywords}'");
        }

        if (BotNameUsed != null) {
            sb.Append($", BotNameUsed: '{BotNameUsed}'");
        }

        if (Payload is { Length: > 0 }) {
            sb.Append($"\nPayload:\n{Payload}");
        }

        return sb.ToString();
    }
}