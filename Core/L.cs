using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Core;

[assembly: XmlConfigurator(ConfigFile = "App.config")]

namespace vkbot_vitalya.Core {
    public static class L {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(Program));

        public static void D(object? message, Exception? e = null) {
            Logger.Debug(message, e);
        }

        public static void I(object? message, Exception? e = null) {
            Logger.Info(message, e);
        }

        public static void W(object? message, Exception? e = null) {
            Logger.Warn(message, e);
        }

        public static void E(object? message, Exception? e = null) {
            Logger.Error(message, e);
        }

        public static void F(object? message, Exception? e = null) {
            Logger.Fatal(message, e);
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
                return ConsoleColor.Gray;
            if (level == Level.Info)
                return ConsoleColor.White;
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