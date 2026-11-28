using System.Collections.Generic;
using CRM.Core.Domain.Taxation;

namespace CRM.EndPoint.FrontEnd.Models.Taxation;

public sealed record TaxationReportModel(TaxReportFilter Filter, TaxReportData Report, IReadOnlyList<TaxMemory> Memories);

public static class TaxReportLabels
{
    public static string Status(TaxReportStatus status) => status switch
    {
        TaxReportStatus.NeedsReview => "در انتظار اعتبارسنجی",
        TaxReportStatus.NeedsData => "نیازمند تکمیل اطلاعات",
        TaxReportStatus.Queued => "در صف",
        TaxReportStatus.Ready => "آماده ارسال",
        TaxReportStatus.Sending => "در حال ارسال",
        TaxReportStatus.Waiting => "در انتظار نتیجه",
        TaxReportStatus.Accepted => "پذیرفته‌شده",
        TaxReportStatus.Rejected => "ردشده",
        TaxReportStatus.Unknown => "نامشخص",
        _ => "ثبت نشده"
    };

    public static string Kind(TaxDispatchKind kind) => kind switch
    {
        TaxDispatchKind.Correction => "اصلاحی",
        TaxDispatchKind.Cancellation => "ابطالی",
        TaxDispatchKind.Return => "برگشت از فروش",
        _ => "اصلی"
    };

    public static string Scope(TaxReportScope scope) => scope switch
    {
        TaxReportScope.Sandbox => "آزمایشی سازمان",
        TaxReportScope.Production => "عملیاتی سازمان",
        _ => "محلی"
    };
}
