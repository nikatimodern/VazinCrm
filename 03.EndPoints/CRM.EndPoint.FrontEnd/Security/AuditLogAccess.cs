using System;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.LogSystems.Entities;

namespace CRM.EndPoint.FrontEnd.Security;

/// <summary>Logs are projections of protected records, not a second way to read those records.</summary>
public static class AuditLogAccess
{
    public static async Task<bool> CanReadRecordAsync(DocumentAccessPolicy access, string entity, string id, CancellationToken ct)
    {
        if (!Guid.TryParse(id, out var key) || key == Guid.Empty) return false;
        return entity switch
        {
            "Customer" => await access.HasDashboardSourceViewAsync("Customer", ct) && await access.CanCustomerAsync(key, "View", ct),
            "Deal" => await access.HasDashboardSourceViewAsync("Deal", ct) && await access.CanDealAsync(key, "View", ct),
            "PreFactor" or "Factor" or "OrderHead" or "Supply" or "SupplyFactor" or "Accountancy" or
            "RequestAmount" or "Payment" or "BankAccount" or "Checkbook" or "Check" or "Transfer" or
            "Cashier" or "Cash" or "BaseCash" or "BaseCenterCash" or "CashGroup" or "Warehouse" or
            "WarehouseProduct" or "WarehouseTransaction" or "WarehouseVoucher" or "Contract" or
            "Carizo" or "Report" or "CallRecording" or "Contact" or "Agent" =>
                access.IsEnforced(entity) && await access.CanAsync(entity, "View", key, ct),
            "AppUser" => await access.HasAsync("LogSystem", "ViewUserEvents", ct),
            // Other security-administration events and unmapped types remain denied.
            _ => false
        };
    }

    public static async Task<bool> CanReadAsync(DocumentAccessPolicy access, LogSystem log, CancellationToken ct)
    {
        if (log.EntityName == "AppUser")
            return log.ActionType is LogActionEnum.ثبت or LogActionEnum.ورود_کاربر or LogActionEnum.خروج_کاربر &&
                string.IsNullOrWhiteSpace(log.ParentEntityName) && string.IsNullOrWhiteSpace(log.ParentEntityId) &&
                await CanReadRecordAsync(access, log.EntityName, log.EntityId, ct);
        var hasParent = !string.IsNullOrWhiteSpace(log.ParentEntityName) || !string.IsNullOrWhiteSpace(log.ParentEntityId);
        if (hasParent && !await CanReadRecordAsync(access, log.ParentEntityName, log.ParentEntityId, ct)) return false;
        // Attachment changes inherit their verified parent. Other rows must also authorize the actual record.
        return log.EntityName == "Attachment" && hasParent ||
            await CanReadRecordAsync(access, log.EntityName, log.EntityId, ct);
    }
}
