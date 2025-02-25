using System.Reflection;
using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Repository.Hierarchy;
[assembly: XmlConfigurator(ConfigFile = "App.config")]

namespace vkbot_vitalya.Core {
    public static class L {
        private static readonly ILog Logger = LogManager.GetLogger("Program");

        private static void Log(string message, ConsoleColor color) {
            Console.ForegroundColor = color;
            // Logger.Info($"[{DateTime.Now:T}] {message}");
            Console.ForegroundColor = ConsoleColor.White;
            File.AppendAllText("./log.txt", $"[{DateTime.Now}] {message}\n");
        }

        public static void M(object? message) {
            Logger.Info(message);
        }

        public static void W(object? message) {
            Logger.Warn(message);
        }

        public static void E(object? message) {
            Logger.Error(message);
        }

        public static void E(Exception ex) {
            Logger.Error("", ex);
        }

        public static void E(string message, Exception ex) {
            Logger.Error(message, ex);
        }

        public static void F(string message, Exception ex) {
            Logger.Fatal(message, ex);
        }

        public static void D(string message) {
            Logger.Debug(message);
        }
    }
}

namespace LoggerNs {
    public class ColoredConsoleAppender : ConsoleAppender {
        protected override void Append(LoggingEvent loggingEvent) {
            Console.ForegroundColor = GetColor(loggingEvent.Level);
            base.Append(loggingEvent);
            Console.ResetColor();
        }

        private ConsoleColor GetColor(Level? level) {
            if (level == Level.Debug)
                return ConsoleColor.Cyan;
            if (level == Level.Info)
                return ConsoleColor.Gray;
            if (level == Level.Warn)
                return ConsoleColor.Yellow;
            if (level == Level.Error)
                return ConsoleColor.Red;
            if (level == Level.Fatal)
                return ConsoleColor.DarkRed;
            return ConsoleColor.Gray;
        }
    }
}