using System;

public static class Out
{
    public static void Message(params object[] message)
    {
        Console.WriteLine(string.Join(" ", message));
    }

    public static void Error(string message)
    {
        var originalColor = Console.ForegroundColor;

        try
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine(message);
        }
        finally
        {
            Console.ForegroundColor = originalColor;
        }
    }
}
