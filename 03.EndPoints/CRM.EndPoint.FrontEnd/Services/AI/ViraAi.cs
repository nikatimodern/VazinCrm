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

            // محصولات پایه و مواد اولیه
            "سلفون,شیرینک,شیرینگ,نایلکس,نایلون,پلی‌اتیلن,پلاستیک,گرانول,مستربچ,کربنات," +

            // انواع جنس و متریال تخصصی
            "استرج,استرچ,حبابدار,او پی پی,سی پی پی,پی او اف,متالایز,صدفی,شیری,شفاف,مات," +

            // فرمت‌های تولید و دوخت
            "کاست,بغل‌کاست,ته‌کاست,رول,شیت,طاقه,پرفراژ,لب‌چسب,لب‌چسبدار,زیپ‌کیپ,پاکت,سفره,زباله,فریزر," +

            // اصطلاحات چاپ
            "سیلک,فلکسو,هلیو,لمینت,کلیشه,سیلندر,چاپ," +

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