using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text.Json;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CRM.EndPoint.FrontEnd.Models.Sms;
using Microsoft.Extensions.Options;

namespace CRM.EndPoint.FrontEnd.Services.Sms;

public sealed class SmsIrService : ISmsIrService {
    private readonly HttpClient _httpClient;
    private readonly SmsIrOptions _options;

    private static readonly JsonSerializerOptions JsonOptions = new() {
        PropertyNameCaseInsensitive = true
    };

    public SmsIrService(HttpClient httpClient, IOptions<SmsIrOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<SmsIrSendResponse> SendAsync(string mobile, string message, long? sendDateTime = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mobile);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        var result = await SendBulkAsync([mobile], message, sendDateTime, cancellationToken);

        return new SmsIrSendResponse {
            MessageId = result.MessageIds.FirstOrDefault(),
            Cost = result.Cost,
            PackId = result.PackId
        };
    }

    public async Task<SmsIrSendBulkResponse> SendBulkAsync(IEnumerable<string> mobiles, string message, long? sendDateTime = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        var mobileList = mobiles
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .ToList();

        if (mobileList.Count == 0)
            throw new ArgumentException("حداقل یک شماره موبایل باید وارد شود.");

        var request = new SmsIrSendBulkRequest {
            LineNumber = _options.LineNumber,
            MessageText = message,
            Mobiles = mobileList,
            SendDateTime = sendDateTime
        };

        return await PostAsync<SmsIrSendBulkResponse>("send/bulk", request, cancellationToken);
    }

    public async Task<SmsIrBulkSendResponse> SendLikeToLikeAsync(IEnumerable<string> mobiles, IEnumerable<string> messages, long? sendDateTime = null,
        CancellationToken cancellationToken = default)
    {
        var mobileList = mobiles
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        var messageList = messages
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        if (mobileList.Count == 0)
            throw new ArgumentException("حداقل یک شماره موبایل باید وارد شود.");

        if (mobileList.Count != messageList.Count)
            throw new ArgumentException(
                "تعداد شماره موبایل و متن پیام باید برابر باشد.");

        var request = new SmsIrSendLikeToLikeRequest {
            LineNumber = _options.LineNumber,
            Mobiles = mobileList,
            MessageTexts = messageList,
            SendDateTime = sendDateTime
        };

        return await PostAsync<SmsIrBulkSendResponse>(
            "send/likeToLike",
            request,
            cancellationToken);
    }

    public async Task<SmsIrSendResponse> SendVerifyCodeAsync(string mobile, int templateId, IEnumerable<SmsIrParameter> parameters,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mobile);

        var parameterList = parameters?.ToList() ?? [];

        if (parameterList.Count == 0)
            throw new ArgumentException("حداقل یک پارامتر برای قالب پیامک باید وارد شود.");

        var request = new SmsIrVerifyRequest {
            Mobile = mobile,
            TemplateId = templateId,
            Parameters = parameterList
        };

        return await PostAsync<SmsIrSendResponse>("send/verify", request, cancellationToken);
    }

    public async Task DeleteScheduledAsync(long packId, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.DeleteAsync($"send/scheduled/{packId}", cancellationToken);

        await EnsureSuccessAsync(response);
    }

    public async Task<SmsIrSendResponse> GetMessageAsync(long messageId, CancellationToken cancellationToken = default)
    {
        return await GetAsync<SmsIrSendResponse>($"send/{messageId}", cancellationToken);
    }

    public async Task<SmsIrBulkSendResponse> GetPackAsync(long packId, CancellationToken cancellationToken = default)
    {
        return await GetAsync<SmsIrBulkSendResponse>($"send/pack/{packId}", cancellationToken);
    }

    public async Task<decimal> GetCreditAsync(CancellationToken cancellationToken = default)
    {
        var result = await GetAsync<SmsIrCreditResponse>("credit", cancellationToken);

        return result?.Credit ?? 0;
    }

    public async Task<IReadOnlyList<SmsIrLine>> GetLinesAsync(CancellationToken cancellationToken = default)
    {
        var result = await GetAsync<List<SmsIrLine>>("line", cancellationToken);

        return result ?? [];
    }

    private async Task<T> PostAsync<T>(string endpoint, object request, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsJsonAsync(endpoint, request, JsonOptions, cancellationToken);

        return await ReadResponseAsync<T>(response, cancellationToken);
    }

    private async Task<T> GetAsync<T>(string endpoint, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(endpoint, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            await EnsureSuccessAsync(response);
        }

        var result = await response.Content.ReadFromJsonAsync<SmsIrApiResponse<T>>(JsonOptions, cancellationToken);


        if (result is null)
            throw new SmsIrApiException((int)response.StatusCode, "پاسخ نامعتبر از SMS.ir دریافت شد.");

        return result.Data;
    }

    private static async Task<T> ReadResponseAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            string message = null;

            try
            {
                var error = JsonSerializer.Deserialize<SmsIrApiResponse<JsonElement>>(content, JsonOptions);

                message = error?.Message;
            }
            catch
            {
                // Ignore invalid error payload.
            }

            throw new SmsIrApiException((int)response.StatusCode, message ?? content);
        }

        var result = JsonSerializer.Deserialize<SmsIrApiResponse<T>>(content, JsonOptions);

        if (result is null)
            throw new SmsIrApiException((int)response.StatusCode, "پاسخ نامعتبر از SMS.ir دریافت شد.");

        if (result.Data is null)
            throw new SmsIrApiException((int)response.StatusCode, result.Message ?? "پاسخ نامعتبر از SMS.ir دریافت شد.");

        return result.Data;
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
            return;

        var content = await response.Content.ReadAsStringAsync();

        string message = null;

        try
        {
            var error = JsonSerializer.Deserialize<SmsIrApiResponse<JsonElement>>(content, JsonOptions);

            message = error?.Message;
        }
        catch
        {
            // Ignore invalid error payload.
        }

        throw new SmsIrApiException((int)response.StatusCode, message ?? content);
    }
}