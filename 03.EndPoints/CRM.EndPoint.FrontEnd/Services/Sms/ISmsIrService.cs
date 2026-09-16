using System.Threading.Tasks;
using CRM.EndPoint.FrontEnd.Models.Sms;
using IPE.SmsIrClient.Models.Requests;
using IPE.SmsIrClient.Models.Results;

namespace CRM.EndPoint.FrontEnd.Services.Sms;


public interface ISmsIrService
{
    Task<SmsIrResult<SendResult>> SendAsync(string mobile, string messageText);

    Task<SmsIrResult<SendResult>> BulkSendAsync(string messageText, string[] mobiles, int? sendDateTime = null);

    Task<ResponseSmsModel> SendPatternSms(SmsJob job);
}