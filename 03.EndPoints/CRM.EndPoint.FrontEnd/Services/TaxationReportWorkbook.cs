using System.Globalization;
using CRM.Core.Domain.Taxation;
using CRM.EndPoint.FrontEnd.Models.Taxation;
using Microsoft.AspNetCore.Mvc;

namespace CRM.EndPoint.FrontEnd.Services;

internal static class TaxationReportWorkbook
{
    internal static FileContentResult Export(TaxReportData report, TaxReportScope scope) =>
        ExcelExportService.ExportToExcel(report.Rows,
        [
            new() { Header = "شماره فاکتور ERP", ValueSelector = row => row.Number },
            new() { Header = "مشتری", ValueSelector = row => row.Customer },
            new() { Header = "تاریخ فاکتور ERP", ValueSelector = row => row.Date, NumberFormat = "yyyy-mm-dd" },
            new() { Header = "مبلغ فاکتور ERP", ValueSelector = row => ExactAmount(row.Amount), NumberFormat = "#,##0.########" },
            new() { Header = "ارز", ValueSelector = row => row.Currency },
            new() { Header = "نوع سند", ValueSelector = row => TaxReportLabels.Kind(row.Kind) },
            new() { Header = "حافظه مالیاتی", ValueSelector = row => row.FiscalId },
            new() { Header = "شناسه مالیاتی", ValueSelector = row => row.TaxId },
            new() { Header = "وضعیت", ValueSelector = row => TaxReportLabels.Status(row.Status) },
            new() { Header = "رسید", ValueSelector = row => row.Acknowledged ? "دارد" : "ندارد" },
            new() { Header = "محیط", ValueSelector = _ => TaxReportLabels.Scope(scope) }
        ], "TaxInvoices-" + scope, convertToTable: report.Rows.Count > 0);

    private static object ExactAmount(decimal amount)
    {
        var text = amount.ToString("0.############################", CultureInfo.InvariantCulture);
        return text.TrimStart('-').Replace(".", "").TrimStart('0').Length > 15 ? text : (object)amount;
    }
}
