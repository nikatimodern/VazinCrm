using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.Factors.Repositories;
using CRM.Core.Domain.Sepidar.Invoices.Entities;
using CRM.EndPoint.FrontEnd.Models.Sepidar;

namespace CRM.EndPoint.FrontEnd.Services.SepidarServices;

public partial class SepidarService
{
    internal sealed record SalesPostingSettings(bool AutoVoucher, bool AutoDelivery);

    internal async Task<SalesPostingSettings> GetSalesEditPostingSettingsAsync(CancellationToken ct) =>
        new(await GetRequiredSepidarBooleanSettingAsync("SalesInvoiceAutomaticVoucher", ct),
            await GetRequiredSepidarBooleanSettingAsync("SalesAutomaticallyIssueInventoryDelivery", ct));

    internal async Task PostEditedSalesInvoiceAsync(SalesInvoicePosting posting, SalesPostingSettings expected, CancellationToken ct)
    {
        if (sepidarContext.Database.CurrentTransaction == null)
            throw new InvalidOperationException("تراکنش ویرایش فاکتور فروش آغاز نشده است.");
        var settings = await GetSalesEditPostingSettingsAsync(ct);
        if (settings != expected)
            throw new InvalidOperationException("تنظیم صدور خودکار اسناد تغییر کرده است. فرم را دوباره باز کنید.");
        var invoice = posting.Invoice;
        var rows = invoice.InvoiceItems.ToList();
        var stockCodes = await GetInventoryStockCodesAsync(rows, ct);
        var controlStock = await GetRequiredSepidarBooleanSettingAsync("SaleControlStockQuantiy", ct);
        if (controlStock)
        {
            var itemCodes = await GetInventoryItemCodesAsync(rows, ct);
            var balances = await ReadInventoryStockBalancesAsync(rows, invoice.FiscalYearRef, ct);
            ValidateInventoryAvailability(rows, balances, stockCodes, itemCodes);
        }
        var broker = posting.BrokerPartyId is int partyId
            ? await CalculateInvoiceBrokerAsync(partyId, invoice.Price, invoice.Discount, invoice.Rate, ct)
            : null;

        invoice.State = 1;
        await sepidarContext.SaveChangesAsync(ct);
        if (broker != null)
            await AddInvoiceBrokerAsync(invoice.InvoiceId, broker, ct);
        if (settings.AutoDelivery)
            await AddInventoryDeliveryAsync(invoice, stockCodes, ct, posting.PreviousDeliveryNumbers);
        if (settings.AutoVoucher)
        {
            var voucher = await AddSalesInvoiceVoucherAsync(invoice, broker, ct, useModifierAsCreator: true);
            sepidarContext.Vouchers.Add(voucher);
            await sepidarContext.SaveChangesAsync(ct);
            invoice.VoucherRef = voucher.VoucherId;
            await sepidarContext.SaveChangesAsync(ct);
        }
    }

    private async Task AddInvoiceBrokerAsync(int invoiceId, SepidarInvoiceBrokerPreviewDto broker, CancellationToken ct)
    {
        sepidarContext.InvoiceBrokers.Add(new InvoiceBrokerRecord {
            BrokerID = await GetNextIdAsync("SLS.InvoiceBroker", ct: ct),
            InvoiceRef = invoiceId,
            PartyRef = broker.PartyId,
            Commission = broker.Commission,
            Rate = broker.CurrencyRate,
            CommissionInBaseCurrency = broker.CommissionInBaseCurrency
        });
        await sepidarContext.SaveChangesAsync(ct);
    }
}
