using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using CRM.Core.Domain.Sms.Enums;
using CRM.EndPoint.FrontEnd.Models.Configuration;
using CRM.EndPoint.FrontEnd.Models.Sms;
using CRM.EndPoint.FrontEnd.Services.Extensions;
using FarazSMS;
using Microsoft.Extensions.Configuration;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace CRM.EndPoint.FrontEnd.Services.Sms;

public class FarazSmsService {
    private readonly DateTimeServices dtService;
    private readonly SmsSetting config;
    private readonly FarazSMS.FarazSMS _farazSms;

    public FarazSmsService(HttpClient httpc, IConfiguration config, DateTimeServices dtService)
    {
        this.dtService = dtService;
        this.config = config.GetSection("FarazSmsConfig").Get<SmsSetting>();
        var apiKey = this.config?.ApiKey ?? throw new InvalidOperationException("FarazSMS API Key is not configured.");

        _farazSms = new FarazSMS.FarazSMS(apiKey, httpClient: httpc);
    }


    private static string GetDeliveryStateTxt(string status)
    {
        return status switch {
            "init" => "در حال آماده‌سازی",
            "pending-approval" => "در انتظار تأیید",
            "insufficient-balance" => "موجودی ناکافی",
            "cancelled" => "لغو شده",
            "rejected" => "رد شده",
            "in-queue" => "در صف ارسال",
            "sent" => "ارسال شده",
            _ => "نامشخص"
        };
    }

    private static string GetTypeSmsTxt(string type)
    {
        return type switch {
            "pattern" => "پترن",
            "simple" => "عادی",
            "automatic-response" => "پاسخ خودکار",
            _ => type
        };
    }

    public async Task<SmsDashboardModel> GetDashboardAsync()
    {
        var responceCredit = await _farazSms.Balance();
        var responceLines = await _farazSms.Lines();

        var balance = JsonSerializer.Deserialize<FarazBalanceResponse>(responceCredit.GetRawText());
        var lineList = JsonSerializer.Deserialize<FarazLinesResponse>(responceLines.GetRawText());

        return new SmsDashboardModel {
            Provider = "FarazSMS.ir",
            Credit = decimal.Parse(balance?.data?.balance_amount ?? "0"),
            Lines = lineList?.data.Where(f => f.is_dedicated).Select(f => f.number).ToList() ?? []
        };
    }

    public async Task<SmsReportResult> GetReportsAsync(int page = 1, int pageSize = 100)
    {
        var response = await _farazSms.SendRequests($"page={page}&limit={pageSize}");
        try
        {
            var raw = response.GetRawText();
            var result = JsonSerializer.Deserialize<FarazReportResponse>(raw);

            var data = result?.data ?? new FarazReportData();

            var items = data.data?.ConvertAll(x => {
                var status = x.status;
                var type = x.type;

                var sendDate = x.schedule.ToLocalTime();
                var createdAt = x.snapshot?.created_at?.ToLocalTime();

                var line = x.overridden_line ?? x.line;

                var messageText = x.pattern_text ?? x.metadata?.text;
                var patternType = SmsService.GetPatternType(x.metadata?.code, config.Patterns);

                return new SmsReportModel {
                    MessageId = x.id,
                    Mobile = x.metadata?.recipient,
                    Number = line?.number,

                    Type = type,
                    TypeTxt = GetTypeSmsTxt(type),

                    PatternType = patternType,
                    PatternTypeTxt = patternType.GetDescription(),

                    DeliveryState = status,
                    DeliveryStateTxt = GetDeliveryStateTxt(status),

                    CreatedAtFaraz = createdAt,
                    SendDateTime = sendDate,
                    SendDateTimeTxt = dtService.GetDateTimePersian(sendDate),

                    MessageText = messageText
                };
            });

            return new SmsReportResult {
                Data = items,
                Total = data.total,
                TotalPages = data.last_page
            };
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }


    public async Task<SmsReceiveResult> GetReceivesAsync(int page = 1, int pageSize = 100)
    {
        var response = await _farazSms.Inbox(page, pageSize);

        var result = JsonSerializer.Deserialize<FarazReceiveResponse>(response.GetRawText());
        var items = result?.data?.data?.ConvertAll(x => {
            var receivedDateTime = x.received_at.ToLocalTime();

            return new SmsReceiveModel {
                MessageId = x.id,
                Mobile = x.from,
                MessageText = x.message,
                Number = x.to,
                CreatedAtFaraz = x.created_at.ToLocalTime(),
                ReceivedDateTime = receivedDateTime,
                ReceivedDateTimeTxt = dtService.GetDateTimePersian(receivedDateTime),
                Seen = x.seen
            };
        }) ?? [];

        return new SmsReceiveResult {
            Data = items,
            Total = result?.data?.total ?? 0,
            TotalPages = result?.data?.last_page ?? 0
        };
    }


    public async Task<ResponseSmsModel> SendPatternAsync(SmsJob job)
    {
        string patternCode;
        Dictionary<string, string> attrs;

        switch (job.PatternType)
        {
            // تایید پیش‌فاکتور توسط مشتری
            case SmsPatternType.AcceptPfactorToCustomer:
                patternCode = config.Patterns.AcceptPfactorToCustomer;
                attrs = new Dictionary<string, string> {
                    ["number"] = job.SerialNumber,
                    ["link"] = job.PfactorId?.ToString().Substring(0, 7)
                };
                break;

            case SmsPatternType.ReacceptEditedPfactorToCustomer:
                patternCode = config.Patterns.ReacceptEditedPfactorToCustomer;
                attrs = new Dictionary<string, string> {
                    ["number"] = job.SerialNumber,
                    ["link"] = job.PfactorId?.ToString().Substring(0, 7)
                };
                break;

            // تایید پیش‌فاکتور توسط تامین
            case SmsPatternType.AcceptPfactorToTamin:
                patternCode = config.Patterns.AcceptPfactorToTamin;
                attrs = new Dictionary<string, string> {
                    ["number"] = job.SerialNumber,
                    ["link"] = "Cartable/Index?tab=TTaminExpertId"
                };
                break;

            // تایید مدیر فروش (برای مشتری)
            case SmsPatternType.AcceptedSalesAdminToCustomer:
                patternCode = config.Patterns.AcceptedSalesAdminToCustomer;
                attrs = new Dictionary<string, string> {
                    ["number"] = job.SerialNumber
                };
                break;

            // محصول آماده تحویل (برای مشتری)
            case SmsPatternType.ReadyToDeliverToCustomer:
                patternCode = config.Patterns.ReadyToDeliverToCustomer;
                attrs = new Dictionary<string, string> {
                    ["number"] = job.SerialNumber
                };
                break;

            // لینک واریز پیش پرداخت (برای مشتری)
            case SmsPatternType.PaymentLinkToCustomer:
                patternCode = config.Patterns.PaymentPfactorLink;
                attrs = new Dictionary<string, string> {
                    ["number"] = job.SerialNumber,
                    ["link"] = job.PfactorId?.ToString().Substring(0, 7)
                };
                break;

            // ارسال OTP
            case SmsPatternType.OtpLogin:
                patternCode = config.Patterns.OtpLogin;
                attrs = new Dictionary<string, string> {
                    ["number"] = job.Code
                };
                break;

            case SmsPatternType.OtpForgot:
                patternCode = config.Patterns.OtpForgot;
                attrs = new Dictionary<string, string> {
                    ["number"] = job.Code
                };
                break;

            // chat reminder
            case SmsPatternType.ChatReminder:
                patternCode = config.Patterns.ChatReminder;
                attrs = new Dictionary<string, string> {
                    ["number"] = job.Mobile
                };
                break;

            // فیدبک مشتری بعد از خرید
            case SmsPatternType.CustomerFeedback:
                patternCode = config.Patterns.CustomerFeedback;
                attrs = new Dictionary<string, string> {
                    ["number"] = job.SerialNumber
                };
                break;

            default:
                throw new NotSupportedException();
        }

        var res = new ResponseSmsModel();

        if (config.ActiveStatus != true)
        {
            res.Code = 0;
            res.Message = "ارسال پیامک غیر فعال است";
            return res;
        }

        try
        {
            var response = await _farazSms.SendPattern(patternCode, job.Mobile, attrs, config.LineNumber);

            var result = JsonSerializer.Deserialize<FarazSmsResponse>(response.GetRawText());

            if (result?.status == "success")
            {
                res.Code = 200;
                res.Message = "ارسال با موفقیت انجام شد";
            } else
            {
                res.Code = 0;
                res.Message = !string.IsNullOrWhiteSpace(result?.message) ? result.message : "خطای ارتباط با سرویس پیامک";
            }

            return res;
        }
        catch (FarazException ex)
        {
            res.Code = ex.StatusCode;
            res.Message = ex.Message;
            return res;
        }
        catch (Exception ex)
        {
            res.Code = 0;
            res.Message = ex.Message;
            return res;
        }
    }


    public async Task<ResponseSmsModel> SendTextAsync(string[] mobiles, string message)
    {
        var res = new ResponseSmsModel();

        if (config.ActiveStatus != true)
        {
            res.Code = 0;
            res.Message = "ارسال پیامک غیر فعال است";
            return res;
        }


        try
        {
            var response = await _farazSms.SendSimple(message, mobiles, config.LineNumber);

            var result = JsonSerializer.Deserialize<FarazSmsResponse>(response.GetRawText());

            if (result?.status == "success")
            {
                res.Code = 200;
                res.Message = "ارسال با موفقیت انجام شد";
            } else
            {
                res.Code = 0;
                res.Message = !string.IsNullOrWhiteSpace(result?.message) ? result.message : "خطای ارتباط با سرویس پیامک";
            }

            return res;
        }
        catch (FarazException ex)
        {
            res.Code = ex.StatusCode;
            res.Message = ex.Message;
            return res;
        }
        catch (Exception ex)
        {
            res.Code = 0;
            res.Message = ex.Message;
            return res;
        }
    }


    public sealed class FarazReportSnapshotConverter : System.Text.Json.Serialization.JsonConverter<FarazReportSnapshot> {
        public override FarazReportSnapshot Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            if (reader.TokenType == JsonTokenType.StartArray)
            {
                using var document = JsonDocument.ParseValue(ref reader);

                return null;
            }

            if (reader.TokenType == JsonTokenType.StartObject)
            {
                return JsonSerializer.Deserialize<FarazReportSnapshot>(ref reader, options);
            }

            return null;
        }

        public override void Write(Utf8JsonWriter writer, FarazReportSnapshot value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value, options);
        }
    }
}
