namespace vkbot_vitalya.Core;

public static class L {
    private static void Log(string message, ConsoleColor color) {
        Console.ForegroundColor = color;
        Console.WriteLine($"[{DateTime.Now:T}] {message}");
        Console.ForegroundColor = ConsoleColor.White;
        File.AppendAllText("./log.txt", $"[{DateTime.Now}] {message}\n");
    }

    public static void M(string message) {
        Log(message, ConsoleColor.Gray);
    }

    public static void W(string message) {
        Log(message, ConsoleColor.Yellow);
    }

    public static void E(string message) {
        Log(message, ConsoleColor.Red);
    }

    public static void E(Exception ex) {
        E(ex.ToString());
    }
}