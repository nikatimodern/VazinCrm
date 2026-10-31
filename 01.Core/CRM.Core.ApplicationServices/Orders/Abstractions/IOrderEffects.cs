using CRM.Core.Domain.Orders.Entities;

namespace CRM.Core.ApplicationServices.Orders.Abstractions;
public interface IOrderEffects
{
    Task<string> RenderColumnAsync(List<OrderHead> heads, int passType, CancellationToken cancellationToken);
    Task<byte[]> RenderDocumentAsync(Guid id, bool image, CancellationToken cancellationToken);
    Task QueueReadyMessageAsync(string mobile, string serialNumber, CancellationToken cancellationToken);
}
