using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using CRM.EndPoint.FrontEnd.Models.ViewModel.Ai;
using Microsoft.Extensions.Logging;

namespace CRM.EndPoint.FrontEnd.Services.AI;

public class ViraAi(HttpClient httpClient, ILogger<ViraAi> logger) {
    private const string domainHotWords =
        "وزین پلاست,روبیکا,بله,ایتا,واتساپ,تلگرام," +
        "نایلون,نایلکس,سلفون,پلی‌اتیلن,پلاستیک,شیرینگ,گلخانه‌ای,کشاورزی,مالچ,یووی," +
        "پاکت,کرافت,سوپاپ‌دار,زیپ‌دار,فویل,آلومینیوم,پنجره‌دار,بغل‌کاست,ته‌کاست,ایستاده,دورونما,شفاف,مات,لفاف," +
        "استرچ,پالت,حبابدار,رکابی,خیاری,فریزر,زباله,لیبل,غلاف," +
        "او پی پی,سی پی پی,پی او اف,متالایز,صدفی,لمینت,گرانول," +
        "سیلک,کلیشه,سیلندر,چاپ,رنگی,تصویری," +
        "تناژ,پارت,میکرون,ضخامت,عرض,طول,سایز,ابعاد,کیلو,کیلوگرم,پیش‌فاکتور,فاکتور,تسویه,باربری,اسنپ,پیک,نمونه";

    public async Task<string> ConvertAudioToText(Stream audioStream, string audioFileName, int durationSeconds)
    {
        logger.LogInformation("Transcription started: {FileName}", audioFileName);
        using var form = new MultipartFormDataContent();

        // API Parameters
        form.Add(new StringContent("telephony"), "model"); // https://api.ivira.ai/partai/avanegar?type=document
        form.Add(new StringContent("false"), "srt");
        form.Add(new StringContent("true"), "inverseNormalizer");
        form.Add(new StringContent("false"), "timestamp");
        form.Add(new StringContent("false"), "spokenPunctuation");
        form.Add(new StringContent("true"), "punctuation");
        form.Add(new StringContent("2"), "numSpeakers");
        form.Add(new StringContent("true"), "diarize");
        form.Add(new StringContent(domainHotWords), "hotwords[]");

        var fileContent = new StreamContent(audioStream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("audio/mpeg");
        form.Add(fileContent, "audio", audioFileName);

        const int threshold = 100;

        try
        {
            if (durationSeconds <= threshold)
            {
                logger.LogInformation("Routing to SHORT API. Duration: {Duration}s - File: {File}", durationSeconds, audioFileName);

                return await QueryShortAudioApi(form, audioFileName);
            } else
            {
                logger.LogInformation("Routing to LONG API (Async). Duration: {Duration}s - File: {File}", durationSeconds, audioFileName);

                return await HandleLongAudioApi(form, audioFileName);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Transcription error: {File}", audioFileName);

            return null;
        }
    }

    private async Task<string> QueryShortAudioApi(MultipartFormDataContent form, string fileName)
    {
        var response = await httpClient.PostAsync("avanegar/v2/avanegar/request", form);

        if (response.IsSuccessStatusCode)
        {
            var resJson = await response.Content.ReadFromJsonAsync<AiAvanegarModel>();
            logger.LogInformation("Short Transcription completed: {File}", fileName);
            return resJson?.Data?.Data?.AiResponse?.Result?.Text;
        }

        var errorContent = await response.Content.ReadAsStringAsync();
        logger.LogError("Short Transcription Error ({Code}) for {File}: {Error}", response.StatusCode, fileName, errorContent);
        return null;
    }


    private async Task<string> HandleLongAudioApi(MultipartFormDataContent form, string fileName)
    {
        var response = await httpClient.PostAsync("avanegar/v2/avanegar-large/request", form);
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            logger.LogError("Long Transcription Init Error ({Code}) for {File}: {Error}", response.StatusCode, fileName, errorContent);
            return null;
        }

        var postResult = await response.Content.ReadFromJsonAsync<AiAvanegarModel>();
        var requestId = postResult?.Data?.Data?.Id;

        if (string.IsNullOrEmpty(requestId))
        {
            logger.LogError("Long Transcription failed to generate RequestId for: {File}", fileName);
            return null;
        }

        logger.LogInformation("Long transcription tracking initialized. ID: {Id} for {File}", requestId, fileName);
        const int maxAttempts = 40; // حداکثر ۴۰ بار چک میکنه (حدود ۶ دقیقه)
        for (var i = 0; i < maxAttempts; i++)
        {
            await Task.Delay(TimeSpan.FromSeconds(10)); // هر ۱۰ ثانیه یکبار چک میکنه

            var trackResponse = await httpClient.GetAsync($"avanegar/v2/avanegar-large/track/{requestId}");
            if (!trackResponse.IsSuccessStatusCode)
            {
                logger.LogWarning("Tracking connection warning ({Code}) for ID: {Id}. Retrying...", trackResponse.StatusCode, requestId);
                continue;
            }

            var trackResult = await trackResponse.Content.ReadFromJsonAsync<AiAvanegarModel>();
            var status = trackResult?.Data?.Status?.ToLowerInvariant();
            var errorContent = await trackResponse.Content.ReadAsStringAsync();
            var ew = await trackResponse.Content.ReadFromJsonAsync<DataAvanegar>();
            if (status == "success")
            {
                logger.LogInformation("Long Transcription completed successfully for: {File} after {Attempts} attempts", fileName, i + 1);
                return trackResult.Data.Data?.AiResponse?.Result?.Text;
            }
            if (status == "failed" || status == "fail")
            {
                logger.LogError("Long Transcription internal processing failed on server for: {File}. Message: {Msg}", fileName, trackResult.Data?.Message);
                return null;
            }

            logger.LogDebug("File {Id} is still processing... (Attempt {Num})", requestId, i + 1);
        }

        logger.LogError("Long API tracking timed out (6 min) for ID: {Id} ({File})", requestId, fileName);
        return null;
    }
}