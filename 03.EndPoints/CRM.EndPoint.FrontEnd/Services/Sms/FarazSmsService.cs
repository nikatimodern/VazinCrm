using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CRM.EndPoint.FrontEnd.Models.Configuration;
using CRM.EndPoint.FrontEnd.Models.Sms;
using CRM.EndPoint.FrontEnd.Services.Extensions;
using FarazSMS;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace CRM.EndPoint.FrontEnd.Services.Sms;

public class FarazSmsService {
    private readonly DateTimeServices dtService;
    private readonly FarazSmsSetting farazConfig;
    private readonly FarazSMS.FarazSMS _farazSms;

    public FarazSmsService(HttpClient httpc, IConfiguration config, DateTimeServices dtService)
    {
        this.dtService = dtService;
        farazConfig = config.GetSection("FarazSmsConfig").Get<FarazSmsSetting>();
        var apiKey = farazConfig?.ApiKey ?? throw new InvalidOperationException("FarazSMS API Key is not configured.");

        _farazSms = new FarazSMS.FarazSMS(apiKey, httpClient: httpc);
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

        var raw = response.GetRawText();
        try
        {
            using JsonDocument document = JsonDocument.Parse(raw);

            var dataArray = document
                .RootElement
                .GetProperty("data")
                .GetProperty("data");

            for (int i = 0; i < dataArray.GetArrayLength(); i++)
            {
                var item = dataArray[i];

                if (item.TryGetProperty("snapshot", out var snapshot))
                {
                    // فقط موارد غیر Object را لاگ کن
                    if (snapshot.ValueKind != JsonValueKind.Object)
                    {
                        Console.WriteLine("========================================");
                        Console.WriteLine($"❌ Invalid snapshot at Index: {i}");
                        Console.WriteLine($"Snapshot Type: {snapshot.ValueKind}");
                        Console.WriteLine($"Snapshot Value: {snapshot}");
                        Console.WriteLine($"Full Item: {item}");
                        Console.WriteLine("========================================");
                    }
                }
                else
                {
                    Console.WriteLine("========================================");
                    Console.WriteLine($"⚠️ snapshot property missing at Index: {i}");
                    Console.WriteLine($"Full Item: {item}");
                    Console.WriteLine("========================================");
                }
            }

            return new SmsReportResult();
            /*

            var result = JsonSerializer.Deserialize<FarazReportResponse>(raw);
//            Console.WriteLine(raw);

            var data = result?.data ?? new FarazReportData();

            var items = data.data?.ConvertAll(x => {
                var status = x.status;
                var type = x.type;

                var sendDate = x.schedule.ToLocalTime();
                var createdAt = x.snapshot?.created_at?.ToLocalTime();

                var line = x.overridden_line ?? x.line;

                var messageText = x.pattern_text ?? x.metadata?.text;

                return new SmsReportModel {
                    MessageId = x.id,
                    Mobile = x.metadata?.recipient,
                    Number = line?.number,

                    Type = type,
                    TypeTxt = type switch {
                        "pattern" => "پترن",
                        "simple" => "عادی",
                        _ => type
                    },

                    DeliveryState = status,
                    DeliveryStateTxt = status switch {
                        "init" => "در حال آماده‌سازی",
                        "pending-approval" => "در انتظار تأیید",
                        "insufficient-balance" => "موجودی ناکافی",
                        "cancelled" => "لغو شده",
                        "rejected" => "رد شده",
                        "in-queue" => "در صف ارسال",
                        "sent" => "ارسال شده",
                        _ => "نامشخص"
                    },

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
            };*/
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

        switch (job.Type)
        {
            // تایید پیش‌فاکتور توسط مشتری
            case SmsType.AcceptPfactorToCustomer:
                patternCode = farazConfig.Patterns.AcceptPfactorToCustomer;
                attrs = new Dictionary<string, string> {
                    ["number"] = job.SerialNumber,
                    ["link"] = job.PfactorId?.ToString().Substring(0, 7)
                };
                break;

            // تایید پیش‌فاکتور توسط تامین
            case SmsType.AcceptPfactorToTamin:
                patternCode = farazConfig.Patterns.AcceptPfactorToTamin;
                attrs = new Dictionary<string, string> {
                    ["number"] = job.SerialNumber,
                    ["link"] = "Cartable/Index?tab=TTaminExpertId"
                };
                break;

            // تایید مدیر فروش (برای مشتری)
            case SmsType.AcceptedSalesAdminToCustomer:
                patternCode = farazConfig.Patterns.AcceptedSalesAdminToCustomer;
                attrs = new Dictionary<string, string> {
                    ["number"] = job.SerialNumber
                };
                break;

            // محصول آماده تحویل (برای مشتری)
            case SmsType.ReadyToDeliverToCustomer:
                patternCode = farazConfig.Patterns.ReadyToDeliverToCustomer;
                attrs = new Dictionary<string, string> {
                    ["number"] = job.SerialNumber
                };
                break;

            // لینک واریز پیش پرداخت (برای مشتری)
            case SmsType.PaymentLinkToCustomer:
                patternCode = farazConfig.Patterns.PaymentPfactorLink;
                attrs = new Dictionary<string, string> {
                    ["number"] = job.SerialNumber,
                    ["link"] = job.PfactorId?.ToString().Substring(0, 7)
                };
                break;

            // ارسال OTP
            case SmsType.OtpLogin:
                patternCode = farazConfig.Patterns.OtpLogin;
                attrs = new Dictionary<string, string> {
                    ["number"] = job.Code
                };
                break;

            case SmsType.OtpForgot:
                patternCode = farazConfig.Patterns.OtpForgot;
                attrs = new Dictionary<string, string> {
                    ["number"] = job.Code
                };
                break;

            // chat reminder
            case SmsType.ChatReminder:
                patternCode = farazConfig.Patterns.ChatReminder;
                attrs = new Dictionary<string, string> {
                    ["number"] = job.Mobile
                };
                break;

            // فیدبک مشتری بعد از خرید
            case SmsType.CustomerFeedback:
                patternCode = farazConfig.Patterns.CustomerFeedback;
                attrs = new Dictionary<string, string> {
                    ["number"] = job.SerialNumber
                };
                break;

            default:
                throw new NotSupportedException();
        }

        var res = new ResponseSmsModel();

        if (farazConfig.ActiveStatus != true)
        {
            res.Code = 0;
            res.Message = "ارسال پیامک غیر فعال است";
            return res;
        }

        try
        {
            var response = await _farazSms.SendPattern(patternCode, job.Mobile, attrs, farazConfig.LineNumber);

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

        if (farazConfig.ActiveStatus != true)
        {
            res.Code = 0;
            res.Message = "ارسال پیامک غیر فعال است";
            return res;
        }


        try
        {
            var response = await _farazSms.SendSimple(message, mobiles, farazConfig.LineNumber);

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
}