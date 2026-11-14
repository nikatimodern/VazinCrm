using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.ASettings.Enums;
using CRM.Infrastructures.Sql;
using CRM.EndPoint.FrontEnd.Services;
using Generics.Infrastructures.Sql;
using Microsoft.EntityFrameworkCore;

namespace CRM.EndPoint.FrontEnd.Services.ExternalMappings
{
    public sealed partial class ExternalMappingService
    {
        private static (string Source, string Target) GetMappingEntityNames(
            ExternalMappingTypeEnum type, string sourceEntity = null, string targetEntity = null)
        {
            var defaultPair = type switch {
                ExternalMappingTypeEnum.تنخواه => ("Cash", "PaymentHeader"),
                ExternalMappingTypeEnum.پیش_فاکتور => ("PreFactor", "Quotation"),
                ExternalMappingTypeEnum.فاکتور => ("Factor", "Invoice"),
                ExternalMappingTypeEnum.مرکز_هزینه => ("BaseCenterCash", "DL"),
                ExternalMappingTypeEnum.مشتری => ("Customer", "Party"),
                ExternalMappingTypeEnum.کارشناسان => ("AppUser", "Party"),
                _ => throw new ArgumentException("نوع نگاشت پشتیبانی نمی‌شود.")
            };
            var source = string.IsNullOrWhiteSpace(sourceEntity) ? defaultPair.Item1 : sourceEntity.Trim();
            var target = string.IsNullOrWhiteSpace(targetEntity) ? defaultPair.Item2 : targetEntity.Trim();
            var sourceValid = source == defaultPair.Item1;
            var targetValid = target == defaultPair.Item2 ||
                type == ExternalMappingTypeEnum.تنخواه && target == "Voucher";
            if (!sourceValid || !targetValid)
                throw new ArgumentException("نوع مبدا یا معادل انتخاب‌شده با نوع نگاشت سازگار نیست.");
            return (source, target);
        }

        private static string FormatManualOption(string code, string name)
        {
            var normalizedName = TextService.NormalizePersianText(name ?? "").Trim();
            return string.IsNullOrWhiteSpace(code) ? normalizedName : $"{code} - {normalizedName}";
        }

        // Search is performed on the database; never load full financial documents into a select.
        public async Task<object> GetManualMappingOptionsAsync(
            Guid softwareId, int mapType, string side, string search,
            string sourceEntity, string targetEntity, CancellationToken ct)
        {
            if (softwareId != Sepidar1SoftwareId && softwareId != Sepidar2SoftwareId
                || !await db.ExternalSoftwares.AsNoTracking().AnyAsync(x => x.Id == softwareId, ct))
                throw new ArgumentException("نرم‌افزار انتخاب‌شده پشتیبانی نمی‌شود.");
            if (!Enum.IsDefined(typeof(ExternalMappingTypeEnum), mapType))
                throw new ArgumentException("نوع نگاشت معتبر نیست.");
            var names = GetMappingEntityNames((ExternalMappingTypeEnum)mapType, sourceEntity, targetEntity);
            search = TextService.NormalizePersianText(TextService.ToEnglishNumber((search ?? "").Trim()));
            if (search.Length > 100) throw new ArgumentException("عبارت جستجو بیش از حد طولانی است.");
            // Remote selects only search after the user types; avoid loading
            // arbitrary first-page data when the endpoint is called without a term.
            if (search.Length < 2)
                return new { options = Array.Empty<object>() };
            int.TryParse(search, out var number);
            Guid.TryParse(search, out var guid);
            const int size = 40;

            if (side == "crm")
            {
                switch (names.Source)
                {
                    case "AppUser":
                    {
                        var query = userManager.Users.AsNoTracking().Where(x => x.IsEmployee);
                        if (search.Length > 0)
                            query = query.Where(x => ((x.FirstName ?? "") + " " + (x.LastName ?? ""))
                                    .Replace("ي", "ی").Replace("ى", "ی").Replace("ك", "ک").Contains(search)
                                || (x.UserName != null && x.UserName.Contains(search))
                                || x.Id == search);
                        var records = await query.OrderBy(x => x.FirstName).ThenBy(x => x.LastName)
                            .Select(x => new { x.Id, x.FirstName, x.LastName, x.UserName })
                            .Take(size).ToListAsync(ct);
                        return new { options = records.Where(x => Guid.TryParse(x.Id, out _))
                            .Select(x => new { id = x.Id,
                                title = FormatManualOption(x.UserName, (x.FirstName + " " + x.LastName).Trim()),
                                detail = "" }) };
                    }
                    case "Customer":
                    {
                        var query = db.Customers.AsNoTracking();
                        if (search.Length > 0)
                            query = query.Where(x => x.Id == guid ||
                                (x.CompanyName != null && x.CompanyName.Replace("ي", "ی").Replace("ى", "ی").Replace("ك", "ک").Contains(search)) ||
                                (x.AgentName != null && x.AgentName.Replace("ي", "ی").Replace("ى", "ی").Replace("ك", "ک").Contains(search)) ||
                                (number > 0 && x.CustomerCode == number));
                        var rows = await query.OrderBy(x => x.CustomerCode)
                            .Select(x => new { x.Id, x.CompanyName, x.AgentName, x.CustomerCode })
                            .Take(size).ToListAsync(ct);
                        return new { options = rows.Select(x => new { id = x.Id.ToString(),
                            title = FormatManualOption(x.CustomerCode.ToString(), (x.CompanyName + " " + x.AgentName).Trim()),
                            detail = "" }) };
                    }
                    case "PreFactor":
                    {
                        var query = db.PreFactors.AsNoTracking();
                        if (search.Length > 0)
                            query = query.Where(x => x.Id == guid ||
                                (number > 0 && x.SerialNumber == number) ||
                                (x.Customer != null && x.Customer.Replace("ي", "ی").Replace("ى", "ی").Replace("ك", "ک").Contains(search)));
                        var rows = await query.OrderByDescending(x => x.SerialNumber)
                            .Select(x => new { x.Id, x.SerialNumber, x.Customer })
                            .Take(size).ToListAsync(ct);
                        return new { options = rows.Select(x => new { id = x.Id.ToString(),
                            title = FormatManualOption(x.SerialNumber.ToString(), "پیش‌فاکتور"),
                            detail = TextService.NormalizePersianText(x.Customer ?? "") }) };
                    }
                    case "Factor":
                    {
                        var query = db.Factors.AsNoTracking();
                        if (search.Length > 0)
                            query = query.Where(x => x.Id == guid ||
                                (number > 0 && x.SerialNumber == number) ||
                                (x.Customer != null && x.Customer.Replace("ي", "ی").Replace("ى", "ی").Replace("ك", "ک").Contains(search)));
                        var rows = await query.OrderByDescending(x => x.SerialNumber)
                            .Select(x => new { x.Id, x.SerialNumber, x.Customer })
                            .Take(size).ToListAsync(ct);
                        return new { options = rows.Select(x => new { id = x.Id.ToString(),
                            title = FormatManualOption(x.SerialNumber.ToString(), "فاکتور"), detail = TextService.NormalizePersianText(x.Customer ?? "") }) };
                    }
                    case "Cash":
                    {
                        var query = db.Cashes.AsNoTracking();
                        if (search.Length > 0)
                            query = query.Where(x => x.Id == guid ||
                                (number > 0 && x.Serial == number) ||
                                (x.CostDescription != null && x.CostDescription.Replace("ي", "ی").Replace("ى", "ی").Replace("ك", "ک").Contains(search)));
                        var rows = await query.OrderByDescending(x => x.Serial)
                            .Select(x => new { x.Id, x.Serial, x.CostDescription })
                            .Take(size).ToListAsync(ct);
                        return new { options = rows.Select(x => new { id = x.Id.ToString(),
                            title = FormatManualOption(x.Serial.ToString(), "تنخواه"), detail = TextService.NormalizePersianText(x.CostDescription ?? "") }) };
                    }
                    case "BaseCenterCash":
                    {
                        var query = db.BaseCenterCashes.AsNoTracking();
                        if (search.Length > 0)
                            query = query.Where(x => x.Id == guid ||
                                (x.Title != null && x.Title.Replace("ي", "ی").Replace("ى", "ی").Replace("ك", "ک").Contains(search)) ||
                                (x.Code != null && x.Code.Contains(search)));
                        var rows = await query.OrderBy(x => x.Code)
                            .Select(x => new { x.Id, x.Code, x.Title })
                            .Take(size).ToListAsync(ct);
                        return new { options = rows.Select(x => new { id = x.Id.ToString(),
                            title = FormatManualOption(x.Code, x.Title), detail = "" }) };
                    }
                }
            }
            else if (side == "external")
            {
                var targetDb = softwareId == Sepidar1SoftwareId
                    ? (SepidarContextDbBase)sdb : (SepidarContextDbBase)s2Db;
                switch (names.Target)
                {
                    case "Party":
                    {
                        // Expert mappings link CRM users to any person in the external
                        // software. Broker status is checked only when exporting an invoice.
                        var query = targetDb.Parties.AsNoTracking();
                        if (mapType != (int)ExternalMappingTypeEnum.کارشناسان)
                            query = query.Where(x => x.IsCustomer);
                        if (search.Length > 0)
                            query = query.Where(x => (number > 0 && x.PartyId == number)
                                || ((x.Name ?? "") + " " + (x.LastName ?? ""))
                                    .Replace("ي", "ی").Replace("ى", "ی").Replace("ك", "ک").Contains(search)
                                || (x.Dl != null && x.Dl.Code != null && x.Dl.Code.Contains(search)));
                        var rows = await query.OrderBy(x => x.Name).ThenBy(x => x.LastName)
                            .Select(x => new { x.PartyId, x.Name, x.LastName,
                                DlCode = x.Dl.Code }).Take(size).ToListAsync(ct);
                        return new { options = rows.Select(x => new { id = x.PartyId.ToString(),
                            title = FormatManualOption(x.DlCode, (x.Name + " " + x.LastName).Trim()),
                            detail = "" }) };
                    }
                    case "Quotation":
                    {
                        var query = targetDb.Quotations.AsNoTracking();
                        if (search.Length > 0)
                            query = query.Where(x => (number > 0 && (x.QuotationId == number || x.Number == number))
                                || (x.CustomerRealName != null && x.CustomerRealName.Replace("ي", "ی").Replace("ى", "ی").Replace("ك", "ک").Contains(search)));
                        var rows = await query.OrderByDescending(x => x.QuotationId)
                            .Select(x => new { x.QuotationId, x.Number, x.CustomerRealName })
                            .Take(size).ToListAsync(ct);
                        return new { options = rows.Select(x => new { id = x.QuotationId.ToString(),
                            title = FormatManualOption(x.Number.ToString(), "پیش‌فاکتور"), detail = TextService.NormalizePersianText(x.CustomerRealName ?? "") }) };
                    }
                    case "Invoice":
                    {
                        var query = targetDb.SalesInvoices.AsNoTracking();
                        if (search.Length > 0)
                            query = query.Where(x => (number > 0 && (x.InvoiceId == number || x.Number == number))
                                || (x.CustomerRealName != null && x.CustomerRealName.Replace("ي", "ی").Replace("ى", "ی").Replace("ك", "ک").Contains(search)));
                        var rows = await query.OrderByDescending(x => x.InvoiceId)
                            .Select(x => new { x.InvoiceId, x.Number, x.CustomerRealName })
                            .Take(size).ToListAsync(ct);
                        return new { options = rows.Select(x => new { id = x.InvoiceId.ToString(),
                            title = FormatManualOption(x.Number.ToString(), "فاکتور"), detail = TextService.NormalizePersianText(x.CustomerRealName ?? "") }) };
                    }
                    case "Voucher":
                    {
                        var query = targetDb.Vouchers.AsNoTracking();
                        if (search.Length > 0)
                            query = query.Where(x => (number > 0 && (x.VoucherId == number || x.Number == number))
                                || (x.Description != null && x.Description.Replace("ي", "ی").Replace("ى", "ی").Replace("ك", "ک").Contains(search)));
                        var rows = await query.OrderByDescending(x => x.VoucherId)
                            .Select(x => new { x.VoucherId, x.Number, x.Description })
                            .Take(size).ToListAsync(ct);
                        return new { options = rows.Select(x => new { id = x.VoucherId.ToString(),
                            title = FormatManualOption(x.Number.ToString(), "Voucher"), detail = TextService.NormalizePersianText(x.Description ?? "") }) };
                    }
                    case "PaymentHeader":
                    {
                        var query = targetDb.PaymentHeaders.AsNoTracking();
                        if (search.Length > 0)
                            query = query.Where(x => (number > 0 && (x.PaymentHeaderId == number || x.Number == number))
                                || (x.Description != null && x.Description.Replace("ي", "ی").Replace("ى", "ی").Replace("ك", "ک").Contains(search)));
                        var rows = await query.OrderByDescending(x => x.PaymentHeaderId)
                            .Select(x => new { x.PaymentHeaderId, x.Number, x.Description })
                            .Take(size).ToListAsync(ct);
                        return new { options = rows.Select(x => new { id = x.PaymentHeaderId.ToString(),
                            title = FormatManualOption(x.Number.ToString(), "اعلامیه پرداخت"), detail = TextService.NormalizePersianText(x.Description ?? "") }) };
                    }
                    case "DL":
                    {
                        var query = targetDb.DLs.AsNoTracking().Where(x => x.IsActive);
                        if (search.Length > 0)
                            query = query.Where(x => (number > 0 && x.DLId == number)
                                || x.Title.Replace("ي", "ی").Replace("ى", "ی").Replace("ك", "ک").Contains(search) || x.Code.Contains(search));
                        var rows = await query.OrderBy(x => x.Code)
                            .Select(x => new { x.DLId, x.Code, x.Title })
                            .Take(size).ToListAsync(ct);
                        return new { options = rows.Select(x => new { id = x.DLId.ToString(),
                            title = FormatManualOption(x.Code, x.Title), detail = "" }) };
                    }
                }
            }

            throw new ArgumentException("نوع مبدا یا مقصد برای جستجو معتبر نیست.");
        }
    }
}
