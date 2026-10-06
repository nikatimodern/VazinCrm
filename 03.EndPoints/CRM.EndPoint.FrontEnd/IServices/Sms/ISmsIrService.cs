using System.Threading.Tasks;
using CRM.EndPoint.FrontEnd.Models.Sms;

namespace CRM.EndPoint.FrontEnd.IServices.Sms;

public interface ISmsIrService {
    Task<SmsDashboardModel> GetDashboardAsync();
    Task<SmsReportResult> GetReportsAsync(int page = 1, int pageSize = 100);
    Task<SmsReceiveResult> GetReceivesAsync(int page = 1, int pageSize = 100, bool sortByNewest = true);

    Task<ResponseSmsModel> SendTextAsync(string[] mobiles, string messageText, int? sendDateTime = null);

    Task<ResponseSmsModel> SendPatternAsync(SmsJob job);
}