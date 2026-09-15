using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CRM.EndPoint.FrontEnd.Models.Sms;

namespace CRM.EndPoint.FrontEnd.Services.Sms;

public interface ISmsIrService {
    Task<SmsIrSendResponse> SendAsync(
        string mobile,
        string message,
        long? sendDateTime = null,
        CancellationToken cancellationToken = default);

    Task<SmsIrSendBulkResponse> SendBulkAsync(IEnumerable<string> mobiles,
        string message,
        long? sendDateTime = null,
        CancellationToken cancellationToken = default);

    Task<SmsIrBulkSendResponse> SendLikeToLikeAsync(
        IEnumerable<string> mobiles,
        IEnumerable<string> messages,
        long? sendDateTime = null,
        CancellationToken cancellationToken = default);

    Task<SmsIrSendResponse> SendVerifyCodeAsync(
        string mobile,
        int templateId,
        IEnumerable<SmsIrParameter> parameters,
        CancellationToken cancellationToken = default);

    Task DeleteScheduledAsync(
        long packId,
        CancellationToken cancellationToken = default);

    Task<SmsIrSendResponse?> GetMessageAsync(
        long messageId,
        CancellationToken cancellationToken = default);

    Task<SmsIrBulkSendResponse?> GetPackAsync(
        long packId,
        CancellationToken cancellationToken = default);

    Task<decimal> GetCreditAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SmsIrLine>> GetLinesAsync(
        CancellationToken cancellationToken = default);
}