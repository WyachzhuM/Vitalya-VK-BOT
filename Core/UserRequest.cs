﻿using System.Text;
using vkbot_vitalya.Config;
using vkbot_vitalya.Core;
using VkNet.Model;

namespace vkbot_vitalya;

public class Cmd
{
    public Cmd(string commandName, string args)
    {
        CommandName = commandName;
        Args = args;
    }

    public string CommandName { get; set; }
    public string Args { get; set; }
}

/// <summary>
/// Класс представления сообщения пользователя
/// </summary>
public class UserRequest
{
    public Action<Message>? onSimpleText;
    public Action<Cmd>? onCommand;
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
            L.M("onPayload?.Invoke");
            onPayload?.Invoke(Payload);
        }
        else if (BotNameUsed != null && ActualCommand != null)
        {
            L.M($"Invoke({Command}, {Keywords})");

            if(Command != null && Keywords != null)
                onCommand?.Invoke(new Cmd(Command, Keywords));
        }
        else
        {
            L.M("onSimpleText?.Invoke");
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

    public override string ToString() {
        var sb = new StringBuilder().Append($"ID: {ID}', Text: '{Text}'");
        if (Command != null) {
            sb.Append($", Command: '{Command}'");
        }

        if (ActualCommand != null) {
            sb.Append($", ActualCommand: '{ActualCommand}'");
        }

        if (Keywords is { Length: > 0 }) {
            sb.Append($", Keywords: '{Keywords}'");
        }

        sb.Append($", BotNameUsed: '{BotNameUsed}'");
        if (Payload is { Length: > 0 }) {
            sb.Append($"\nPayload:\n{Payload}");
        }

        return sb.ToString();
    }
}