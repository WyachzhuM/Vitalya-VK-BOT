using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vkbot_vitalya.Core;

public static class Logger
{
    public static void M(string message)
    {
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine(DateTime.Now + Environment.NewLine);
        Console.WriteLine(message);
        Console.ForegroundColor = ConsoleColor.White;
        File.AppendAllText("./log.txt", $"{DateTime.Now}\n{message}\n");
    }
}
