using CRM.Core.Domain.Reports.Entities;
namespace CRM.Core.ApplicationServices.Reports.Dtos;
public sealed record ReportListResult(List<Report> Reports, int PassType, bool IsManager, string CurrentUserId);
public sealed record ReportPartialResult(int Count, string Html);
public sealed record ReportExportFile(byte[] Contents, string ContentType, string FileName);
