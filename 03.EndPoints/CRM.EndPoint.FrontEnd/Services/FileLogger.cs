namespace CRM.EndPoint.FrontEnd.Services;

using System;
using System.IO;
using System.Threading.Tasks;

public static class FileLogger
{
    private static readonly string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log.txt");

    public static async Task LogErrorAsync(Exception ex)
    {
        string message =
            $"----- ERROR ({DateTime.Now}) -----\n" +
            $"Message: {ex.Message}\n" +
            $"StackTrace: {ex.StackTrace}\n" +
            $"------------------------------\n";

        await File.AppendAllTextAsync(logPath, message);
    }
}
