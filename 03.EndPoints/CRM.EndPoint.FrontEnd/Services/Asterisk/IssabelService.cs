using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Renci.SshNet;

namespace CRM.EndPoint.FrontEnd.Services.Asterisk;

public class IssabelService(ILogger<IssabelService> logger) {
    private const string FtpHost = "192.168.1.150";
    private const string FtpUser = "root";
    private const string FtpPass = "123";

    private async Task<MemoryStream> GetAudioFromIsabel(string remoteFilePath)
    {
        logger.LogInformation("Downloading audio: {Path}", remoteFilePath);
        var memoryStream = new MemoryStream();
        try
        {
            using var client = new SftpClient(FtpHost, FtpUser, FtpPass);

            client.ConnectionInfo.Timeout = TimeSpan.FromSeconds(30);
            client.OperationTimeout = TimeSpan.FromSeconds(60);

            client.Connect();
            logger.LogInformation("SFTP connected");

            client.DownloadFile(remoteFilePath, memoryStream);
            logger.LogInformation("Audio downloaded with size: {Size}", memoryStream.Length);
            client.Disconnect();
            logger.LogInformation("SFTP disconnected");

            memoryStream.Position = 0;

            return memoryStream;
        }
        catch (Exception)
        {
            await memoryStream.DisposeAsync();
            throw;
        }
    }

    public async Task<MemoryStream> GetAudioWithRetry(string path)
    {
        try
        {
            //logger.LogInformation("Audio retry started");
            for (var i = 0; i < 5; i++)
            {
                logger.LogInformation("Audio retry started {Attempt}/5", i + 1);
                try
                {
                    var stream = await GetAudioFromIsabel(path);

                    if (stream != null && stream.Length > 0)
                    {
                        logger.LogInformation("Audio loaded");
                        return stream;
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Audio retry failed");
                    Console.WriteLine(ex);
                }

                await Task.Delay(2000);
            }

            logger.LogWarning("Audio retries exhausted");
            return null;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Audio load error");
            Console.WriteLine(e);
            return null;
        }
    }
}