using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using CRM.EndPoint.FrontEnd.Models.ViewModel.Ai;
using Microsoft.Extensions.Logging;

namespace CRM.EndPoint.FrontEnd.Services.AI;

public class ViraAi {
    private readonly HttpClient _httpClient;
    private readonly ILogger<ViraAi> logger;

    public ViraAi(HttpClient httpClient, ILogger<ViraAi> logger)
    {
        _httpClient = httpClient;
        this.logger = logger;
    }


    public async Task<AiResponseDetails> ConvertAudioToText(Stream audioStream, string audioFileName)
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
        const string domainHotWords =
            // نام شرکت و پیام‌رسان‌های رایج برای ارسال فاکتور
            "وزین پلاست,روبیکا,بله,ایتا,واتساپ,تلگرام," +

            // محصولات پایه و کشاورزی/گلخانه‌ای (تخصص وزین پلاست)
            "نایلون,نایلکس,سلفون,پلی‌اتیلن,پلاستیک,شیرینگ,گلخانه‌ای,کشاورزی,مالچ,یووی," +

            // پاکت‌ها و لفاف‌های بسته‌بندی (محصولات ویژه سایت)
            "پاکت,کرافت,سوپاپ‌دار,زیپ‌دار,فویل,آلومینیوم,پنجره‌دار,بغل‌کاست,ته‌کاست,ایستاده,دورونما,شفاف,مات,لفاف," +

            // انواع فیلم و پلاستیک‌های صنعتی و فروشگاهی
            "استرچ,پالت,حبابدار,رکابی,خیاری,فریزر,زباله,لیبل,غلاف," +

            // جنس و متریال تخصصی
            "او پی پی,سی پی پی,پی او اف,متالایز,صدفی,لمینت,گرانول," +

            // اصطلاحات چاپ (فلکسو و هلیو)
            "سیلک,کلیشه,سیلندر,چاپ,رنگی,تصویری," +

            // واحدها، ابعاد و اصطلاحات فروش
            "تناژ,پارت,میکرون,ضخامت,عرض,طول,سایز,ابعاد,کیلو,کیلوگرم,پیش‌فاکتور,فاکتور,تسویه,باربری,اسنپ,پیک,نمونه";

        form.Add(new StringContent(domainHotWords), "hotwords[]");
        // فایل صوتی
        //var filePath = Path.Combine("wwwroot", "a.mp3");
        //var fileBytess = await File.ReadAllBytesAsync(filePath);

        var fileContent = new StreamContent(audioStream);

        fileContent.Headers.ContentType = new MediaTypeHeaderValue("audio/mpeg");

        form.Add(fileContent, "audio", audioFileName);

        try
        {
            var response = await _httpClient.PostAsync("avanegar/v2/avanegar/request", form);

            if (response.IsSuccessStatusCode)
            {
                var resJson = await response.Content.ReadFromJsonAsync<AiAvanegarModel>();
                logger.LogInformation("Transcription completed");

                return resJson?.Data?.Data?.AiResponse;
            } else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                logger.LogError("Transcription API Error ({Code}): {Error}", response.StatusCode, errorContent);
                return null;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Transcription error");

            return null;
        }
    }
}