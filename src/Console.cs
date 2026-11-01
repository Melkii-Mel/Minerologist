using System;

public static class Out
{
    public static void Message(string message)
    {
        Console.WriteLine(message);
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
