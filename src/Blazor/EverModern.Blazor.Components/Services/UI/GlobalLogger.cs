namespace EverModern.Blazor.Components.Services.UI;

static class GlobalLogger
{
    static void Log(string message) => Console.WriteLine($"{DateTime.Now} --> {message}");

    public static string Info(string message)
    {
        Log($"INFO: {message}");
        return message;
    }

    public static string Debug(string message)
    {
#if DEBUG
        Log($"DEBUG: {message}");
#endif
        return message;
    }

    public static string Error(string message)
    {
        Log($"ERROR: {message}");
        return message;
    }
}
