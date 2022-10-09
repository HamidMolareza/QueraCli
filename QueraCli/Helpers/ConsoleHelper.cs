namespace QueraCli.Helpers;

public static class ConsoleHelper {
    public static void Write(string text, ConsoleColor color) {
        var prevColor = Console.ForegroundColor;

        Console.ForegroundColor = color;
        Console.Write(text);
        Console.ForegroundColor = prevColor;
    }

    public static void WriteLine(string text, ConsoleColor color) => Write($"{text}\n", color);
}