using System;
using CRM.Core.Domain.Accountancies.Entities;
using CRM.Core.Domain.Accountancies.Enum;
using CRM.Core.Domain.PreFactors.Enum;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.ExternalSoftwares.Enums;
using CRM.Infrastructures.Sql;
using CRM.EndPoint.FrontEnd.Services;
using Generics.Infrastructures.Sql;
using Microsoft.EntityFrameworkCore;

using static CRM.Core.Domain.ExternalSoftwares.ExternalSoftwareIds;

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
                ExternalMappingTypeEnum.تامین_کننده => ("Customer", "Party"),
                ExternalMappingTypeEnum.فاکتور_خرید => ("SupplyFactor", "InventoryPurchaseInvoice"),
                ExternalMappingTypeEnum.رسید_انبار => ("SupplyFactor", "InventoryReceipt"),
                ExternalMappingTypeEnum.کاربران => ("AppUser", ExternalUserEntityName),
                ExternalMappingTypeEnum.واریزی => (nameof(Accountancy), "ReceiptHeader"),
                ExternalMappingTypeEnum.حساب_بانکی => (nameof(BankAccount), "BankAccount"),
                _ => throw new ArgumentException("نوع ارتباط پشتیبانی نمی‌شود.")
            };
            var source = string.IsNullOrWhiteSpace(sourceEntity) ? defaultPair.Item1 : sourceEntity.Trim();
            var target = string.IsNullOrWhiteSpace(targetEntity) ? defaultPair.Item2 : targetEntity.Trim();
            var sourceValid = source == defaultPair.Item1;
            var targetValid = target == defaultPair.Item2 ||
                type == ExternalMappingTypeEnum.تنخواه && target == "Voucher" ||
                type == ExternalMappingTypeEnum.واریزی && target == "DebitCreditNote";
            if (!sourceValid || !targetValid)
                throw new ArgumentException("نوع رکورد ERP یا معادل انتخاب‌شده با نوع ارتباط سازگار نیست.");
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
                throw new ArgumentException("نوع ارتباط معتبر نیست.");
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
                    case nameof(Accountancy):
                    {
                        var query = EligibleDeposits(db.Accountancies.AsNoTracking());
                        query = names.Target == "DebitCreditNote"
                            ? query.Where(x => x.PayType == PayTypeEnum.تهاتری)
                            : query.Where(x => x.PayType != PayTypeEnum.تهاتری);
                        var hasAmount = decimal.TryParse(search, out var amount);
                        var rows = await query.Where(x => x.Id == guid
                                || (x.CustomerName ?? "").Replace("ي", "ی").Replace("ى", "ی").Replace("ك", "ک").Contains(search)
                                || (x.dateSh ?? "").Contains(search) || (x.ReciveDate ?? "").Contains(search)
                                || (x.VosolDate ?? "").Contains(search)
                                || (number > 0 && (x.SerialNum == number || x.CustomerCode == number || x.pfSerialNumber == number || x.fSerialNumber == number))
                                || (hasAmount && x.credit == amount))
                            .OrderByDescending(x => x.CreateDate)
                            .Select(x => new { x.Id, x.SerialNum, x.CustomerName, x.credit, x.PayTypeTxt, x.dateSh, x.ReciveDate, x.VosolDate, x.PayType })
                            .Take(size).ToListAsync(ct);
                        return new { options = rows.Select(x => new { id = x.Id.ToString(),
                            title = FormatManualOption(
                                $"{x.SerialNum} ({(string.IsNullOrWhiteSpace(x.PayTypeTxt) ? x.PayType.ToString() : x.PayTypeTxt)})",
                                $"{x.CustomerName} | {(x.PayType == PayTypeEnum.چکی ? x.ReciveDate : x.VosolDate ?? x.dateSh)} | {x.credit:N0} ریال"), detail = "" }) };
                    }
                    case nameof(BankAccount):
                    {
                        var rows = await db.BankAccounts.AsNoTracking()
                            .Where(x => !x.IsDeleted && x.Active && (x.Id == guid
                                || (x.AccountName ?? "").Replace("ي", "ی").Replace("ى", "ی").Replace("ك", "ک").Contains(search)
                                || (x.BankName ?? "").Replace("ي", "ی").Replace("ى", "ی").Replace("ك", "ک").Contains(search)
                                || (x.AccountNumber ?? "").Contains(search) || (x.ShebaNumber ?? "").Contains(search)
                                || (x.CartNumber ?? "").Contains(search)))
                            .OrderBy(x => x.BankName).ThenBy(x => x.AccountNumber)
                            .Select(x => new { x.Id, x.AccountNumber, x.AccountName, x.BankName })
                            .Take(size).ToListAsync(ct);
                        return new { options = rows.Select(x => new { id = x.Id.ToString(),
                            title = FormatManualOption(x.AccountNumber, $"{x.BankName} | {x.AccountName}"), detail = "" }) };
                    }
                    case "SupplyFactor":
                    {
                        var rows = await db.SupplyFactors.AsNoTracking()
                            .Where(x => x.Id == guid || x.SerialNumber.Contains(search)
                                || x.SupplierTxt.Replace("ي", "ی").Replace("ى", "ی").Replace("ك", "ک").Contains(search))
                            .OrderByDescending(x => x.SerialNumber).ThenBy(x => x.Id)
                            .Select(x => new { x.Id, x.SerialNumber, x.SupplierTxt })
                            .Take(size).ToListAsync(ct);
                        return new { options = rows.Select(x => new { id = x.Id.ToString(),
                            title = FormatManualOption(x.SerialNumber, x.SupplierTxt), detail = "" }) };
                    }
                    case "AppUser":
                    {
                        // Both expert-to-Party and user-to-FMK.User mappings require CRM employees.
                        var query = userManager.Users.AsNoTracking().Where(x => x.IsEmployee);
                        if (search.Length > 0)
                            query = query.Where(x => ((x.FirstName ?? "") + " " + (x.LastName ?? ""))
                                    .Replace("ي", "ی").Replace("ى", "ی").Replace("ك", "ک").Contains(search)
                                || (x.UserName != null && x.UserName.Replace("ي", "ی")
                                    .Replace("ى", "ی").Replace("ك", "ک").Contains(search))
                                || x.Id == search);
                        var records = await query.OrderBy(x => x.FirstName).ThenBy(x => x.LastName)
                            .Select(x => new { x.Id, x.FirstName, x.LastName, x.UserName })
                            .Take(size).ToListAsync(ct);
                        return new { options = records.Where(x => Guid.TryParse(x.Id, out _))
                            .Select(x => new { id = x.Id,
                                title = FormatManualOption(TextService.NormalizePersianText(x.UserName),
                                    (x.FirstName + " " + x.LastName).Trim()),
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
                            title = FormatManualOption(x.CustomerCode.ToString(),
                                CrmCustomerDisplay(x.CompanyName, x.AgentName)),
                            detail = "" }) };
                    }
                    case "PreFactor":
                    {
                        var query = db.PreFactors.AsNoTracking();
                        if (search.Length > 0)
                            query = query.Where(x => x.Id == guid ||
                                (number > 0 && x.SerialNumber == number) ||
                                (x.Customer != null && x.Customer.Replace("ي", "ی").Replace("ى", "ی").Replace("ك", "ک").Contains(search)) ||
                                (x.AgentName != null && x.AgentName.Replace("ي", "ی").Replace("ى", "ی").Replace("ك", "ک").Contains(search)));
                        var rows = await query.OrderByDescending(x => x.SerialNumber)
                            .Select(x => new { x.Id, x.SerialNumber, x.Customer, x.AgentName })
                            .Take(size).ToListAsync(ct);
                        return new { options = rows.Select(x => new { id = x.Id.ToString(),
                            title = FormatManualOption(x.SerialNumber.ToString(),
                                CrmCustomerDisplay(x.Customer, x.AgentName)), detail = "" }) };
                    }
                    case "Factor":
                    {
                        var query = db.Factors.AsNoTracking();
                        if (search.Length > 0)
                            query = query.Where(x => x.Id == guid ||
                                (number > 0 && x.SerialNumber == number) ||
                                (x.Customer != null && x.Customer.Replace("ي", "ی").Replace("ى", "ی").Replace("ك", "ک").Contains(search)) ||
                                (x.AgentName != null && x.AgentName.Replace("ي", "ی").Replace("ى", "ی").Replace("ك", "ک").Contains(search)));
                        var rows = await query.OrderByDescending(x => x.SerialNumber)
                            .Select(x => new { x.Id, x.SerialNumber, x.Customer, x.AgentName })
                            .Take(size).ToListAsync(ct);
                        return new { options = rows.Select(x => new { id = x.Id.ToString(),
                            title = FormatManualOption(x.SerialNumber.ToString(),
                                CrmCustomerDisplay(x.Customer, x.AgentName)), detail = "" }) };
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
                    case "ReceiptHeader":
                    {
                        var currency = await MappingRialCurrencyAsync(targetDb, ct);
                        var hasAmount = decimal.TryParse(search, out var amount);
                        var rows = await targetDb.ReceiptHeaders.AsNoTracking()
                            .Where(x => x.Type == 1 && (x.ItemType == 4 || x.ItemType == 8) && x.CurrencyRef == currency
                                && ((number > 0 && (x.ReceiptHeaderId == number || x.Number == number))
                                    || (hasAmount && x.TotalAmount == amount)
                                    || (x.Description ?? "").Replace("ي", "ی").Replace("ى", "ی").Replace("ك", "ک").Contains(search)
                                    || targetDb.DLs.Any(d => d.DLId == x.DlRef &&
                                        (d.Title ?? "").Replace("ي", "ی").Replace("ى", "ی").Replace("ك", "ک").Contains(search))))
                            .OrderByDescending(x => x.Date).ThenByDescending(x => x.Number)
                            .Select(x => new { Id = x.ReceiptHeaderId, x.Number, x.Date, x.TotalAmount,
                                Name = targetDb.DLs.Where(d => d.DLId == x.DlRef).Select(d => d.Title).FirstOrDefault() })
                            .Take(size).ToListAsync(ct);
                        return new { options = rows.Select(x => new { id = x.Id.ToString(),
                            title = FormatManualOption(x.Number.ToString(), $"{x.Name} | {FormatLegacyPersianDate(x.Date)} | {x.TotalAmount:N0} ریال"), detail = "" }) };
                    }
                    case "DebitCreditNote":
                    {
                        var currency = await MappingRialCurrencyAsync(targetDb, ct);
                        var hasAmount = decimal.TryParse(search, out var amount);
                        var rows = await targetDb.DebitCreditNotes.AsNoTracking()
                            .Where(x => x.CurrencyRef == currency &&
                                ((number > 0 && (x.DebitCreditNoteID == number || x.Number == number))
                                || (hasAmount && x.SumAmount == amount)
                                || (x.Description ?? "").Replace("ي", "ی").Replace("ى", "ی").Replace("ك", "ک").Contains(search)))
                            .OrderByDescending(x => x.Date).ThenByDescending(x => x.Number)
                            .Select(x => new { Id = x.DebitCreditNoteID, x.Number, x.Date, x.SumAmount, x.Description })
                            .Take(size).ToListAsync(ct);
                        return new { options = rows.Select(x => new { id = x.Id.ToString(),
                            title = FormatManualOption(x.Number.ToString(), $"{FormatLegacyPersianDate(x.Date)} | {x.SumAmount:N0} ریال | {x.Description}"), detail = "" }) };
                    }
                    case "BankAccount":
                    {
                        var currency = await MappingRialCurrencyAsync(targetDb, ct);
                        var rows = await targetDb.SepidarBankAccounts.AsNoTracking()
                            .Where(x => x.CurrencyRef == currency && ((number > 0 && x.BankAccountId == number)
                                || x.AccountNo.Contains(search) || (x.ShebaNumber ?? "").Contains(search)
                                || (x.CreditCardNumber ?? "").Contains(search)
                                || (x.Owner ?? "").Replace("ي", "ی").Replace("ى", "ی").Replace("ك", "ک").Contains(search)
                                || (x.Dl.Title ?? "").Replace("ي", "ی").Replace("ى", "ی").Replace("ك", "ک").Contains(search)
                                || (x.SepidarBankBranch.SepidarBank.Title ?? "").Replace("ي", "ی").Replace("ى", "ی").Replace("ك", "ک").Contains(search)
                                || x.Dl.Code.Contains(search)))
                            .OrderBy(x => x.AccountNo)
                            .Select(x => new { x.BankAccountId, x.AccountNo, Name = x.Dl.Title,
                                Bank = x.SepidarBankBranch.SepidarBank.Title })
                            .Take(size).ToListAsync(ct);
                        return new { options = rows.Select(x => new { id = x.BankAccountId.ToString(),
                            title = FormatManualOption(x.AccountNo, $"{x.Bank} | {x.Name}"), detail = "" }) };
                    }
                    case "InventoryPurchaseInvoice":
                    {
                        var rows = await targetDb.InventoryPurchaseInvoices.AsNoTracking()
                            .Where(x => x.Type == 1 && number > 0
                                && (x.InventoryPurchaseInvoiceID == number || x.Number == number || x.InvoiceNumber == number))
                            .OrderByDescending(x => x.InventoryPurchaseInvoiceID)
                            .Select(x => new { Id = x.InventoryPurchaseInvoiceID, x.Number, x.Date })
                            .Take(size).ToListAsync(ct);
                        return new { options = rows.Select(x => new { id = x.Id.ToString(),
                            title = FormatManualOption(x.Number.ToString(), "فاکتور خرید"),
                            detail = CRM.Framework.BaseServices.GetDatePersian(x.Date) }) };
                    }
                    case "InventoryReceipt":
                    {
                        var rows = await targetDb.InventoryReceipts.AsNoTracking()
                            .Where(x => !x.IsReturn && x.BasePurchaseInvoiceRef != null && number > 0
                                && (x.InventoryReceiptID == number || x.Number == number))
                            .OrderByDescending(x => x.InventoryReceiptID)
                            .Select(x => new { Id = x.InventoryReceiptID, x.Number, x.Date, x.BasePurchaseInvoiceRef })
                            .Take(size).ToListAsync(ct);
                        return new { options = rows.Select(x => new { id = x.Id.ToString(),
                            title = FormatManualOption(x.Number.ToString(), "رسید انبار"),
                            detail = $"{CRM.Framework.BaseServices.GetDatePersian(x.Date)} | شناسه رسید {x.Id} | شناسه فاکتور خرید {x.BasePurchaseInvoiceRef}" }) };
                    }
                    case ExternalUserEntityName:
                    {
                        var query = targetDb.SepidarUsers.AsNoTracking().Where(x => !x.IsDeleted && x.Status == true);
                        // The query is normalized above; apply the same replacements in SQL
                        // so Arabic letters stored by Sepidar match Persian keyboard input.
                        query = query.Where(x => (number > 0 && x.UserID == number)
                            || x.UserName.Replace("ي", "ی").Replace("ى", "ی").Replace("ك", "ک").Contains(search)
                            || x.Name.Replace("ي", "ی").Replace("ى", "ی").Replace("ك", "ک").Contains(search));
                        var rows = await query.OrderBy(x => x.UserName)
                            .Select(x => new { x.UserID, x.UserName, x.Name })
                            .Take(size).ToListAsync(ct);
                        return new { options = rows.Select(x => new {
                            id = x.UserID.ToString(),
                            title = FormatManualOption(TextService.NormalizePersianText(x.UserName), x.Name),
                            detail = "" }) };
                    }
                    case "Party":
                    {
                        // Expert mappings link CRM users to any person in the external
                        // software. Broker status is checked only when exporting an invoice.
                        var query = targetDb.Parties.AsNoTracking();
                        if (mapType == (int)ExternalMappingTypeEnum.تامین_کننده)
                            query = query.Where(x => x.IsVendor && x.Dl != null);
                        else if (mapType != (int)ExternalMappingTypeEnum.کارشناسان)
                            query = query.Where(x => x.IsCustomer);
                        if (search.Length > 0)
                            query = query.Where(x => (number > 0 && x.PartyId == number)
                                || ((x.Name ?? "") + " " + (x.LastName ?? ""))
                                    .Replace("ي", "ی").Replace("ى", "ی").Replace("ك", "ک").Contains(search)
                                || (x.Dl != null && x.Dl.Code != null && x.Dl.Code.Contains(search)));
                        var rows = await query.OrderBy(x => x.Name).ThenBy(x => x.LastName)
                            .Select(x => new { x.PartyId, x.Name, x.LastName,
                                DlCode = x.Dl.Code, x.IsCustomer, x.IsEmployee,
                                x.IsVendor, x.IsBroker }).Take(size).ToListAsync(ct);
                        return new { options = rows.Select(x => new { id = x.PartyId.ToString(),
                            title = FormatManualOption(x.DlCode, (x.Name + " " + x.LastName).Trim()),
                            x.IsCustomer, x.IsEmployee, x.IsVendor, x.IsBroker }) };
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
                            title = FormatManualOption(x.Number.ToString(), "سند حسابداری"), detail = TextService.NormalizePersianText(x.Description ?? "") }) };
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
