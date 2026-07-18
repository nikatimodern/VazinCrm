using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Renci.SshNet;

namespace CRM.EndPoint.FrontEnd.Services.Asterisk;

public class IssabelService(ILogger<IssabelService> logger) {
    private const string FtpHost = "192.168.1.150";
    private const string FtpUser = "root";
    private const string FtpPass = "123";

    private async Task<MemoryStream> GetAudioFromIsabel(string remoteFilePath, CancellationToken ct)
    {
        logger.LogInformation("Downloading audio: {Path}", remoteFilePath);
        var memoryStream = new MemoryStream();
        try
        {
            using var client = new SftpClient(FtpHost, FtpUser, FtpPass);

            client.ConnectionInfo.Timeout = TimeSpan.FromSeconds(30);
            client.OperationTimeout = TimeSpan.FromSeconds(60);

            await client.ConnectAsync(ct);
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

    public async Task<MemoryStream> GetAudioWithRetry(string path, CancellationToken ct = default)
    {
        try
        {
            for (var i = 0; i < 5; i++)
            {
                logger.LogInformation("Audio retry started {Attempt}/5", i + 1);
                try
                {
                    var stream = await GetAudioFromIsabel(path, ct);

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

                await Task.Delay(2000, ct);
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

    public async Task<MemoryStream> ConvertToFlac(MemoryStream wav)
    {
        wav.Position = 0;

        var input = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".wav");
        var output = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".flac");

        try
        {
            await File.WriteAllBytesAsync(input, wav.ToArray());
            var ffmpegPath = Path.Combine(AppContext.BaseDirectory, "Tools", "ffmpeg", "ffmpeg.exe");
            var psi = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = $"-y -i \"{input}\" -c:a flac -compression_level 8 \"{output}\"",
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi)!;

            var stderr = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
                throw new Exception($"ffmpeg failed:{Environment.NewLine}{stderr}");

            // برای تست فایل را ذخیره کن
            var saveFolder = Path.Combine(AppContext.BaseDirectory, "ConvertedAudio");
            Directory.CreateDirectory(saveFolder);

            var savedFile = Path.Combine(
                saveFolder,
                $"audio_{DateTime.Now:yyyyMMdd_HHmmss}.flac");

            File.Copy(output, savedFile, true);

            Console.WriteLine($"Saved FLAC : {savedFile}");

            var bytes = await File.ReadAllBytesAsync(output);

            Console.WriteLine($"WAV : {wav.Length / 1024d / 1024d:F2} MB");
            Console.WriteLine($"FLAC: {bytes.Length / 1024d / 1024d:F2} MB");
            return new MemoryStream(bytes);
        }
        finally
        {
            if (File.Exists(input))
                File.Delete(input);

            // فایل temp پاک شود
            if (File.Exists(output))
                File.Delete(output);
        }
    }
}