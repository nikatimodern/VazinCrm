using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.ASettings.Enums;
using CRM.Core.Domain.Cashes.Entities;
using CRM.Core.Domain.ExternalSoftwares.Entites;
using CRM.Core.Domain.PreFactors.Entities;
using CRM.Core.Domain.Sepidar.Banks.Entites;
using CRM.Core.Domain.Sepidar.DLs.Entites;
using CRM.Core.Domain.Sepidar.Invoices.Entities;
using CRM.Core.Domain.Sepidar.Items;
using CRM.Core.Domain.Sepidar.Items.Entities;
using CRM.Core.Domain.Sepidar.Parties;
using CRM.Core.Domain.Sepidar.Payments.Entites;
using CRM.Core.Domain.Sepidar.Quotations.Entites;
using CRM.Core.Domain.Sepidar.SaleTypes;
using CRM.Core.Domain.Sepidar.SaleTypes.Entities;
using CRM.Core.Domain.Sepidar.Stocks;
using CRM.Core.Domain.Sepidar.Units.Entities;
using CRM.Core.Domain.Sepidar.Vouchers.Entites;
using CRM.EndPoint.FrontEnd.Models;
using CRM.EndPoint.FrontEnd.Models.Factor;
using CRM.EndPoint.FrontEnd.Models.Sepidar;
using CRM.Infrastructures.Sql;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace CRM.EndPoint.FrontEnd.Services.SepidarServices;

public class SepidarService(SepidarContextDbBase sepidarContext) {
    public async Task<Invoice> GetInvoiceAsync(int? invoiceId)
    {
        return await sepidarContext.vwInvoice
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.InvoiceId == invoiceId);
    }

    public async Task<Invoice> GetInvoiceWithItemsAsync(int invoiceId)
    {
        return await sepidarContext.vwInvoice
            .AsNoTracking()
            .Include(x => x.InvoiceItems)
            .FirstOrDefaultAsync(x => x.InvoiceId == invoiceId);
    }

    public async Task<List<Invoice>> GetInvoicesByQuotationNumberAsync(int? quotationNumber)
    {
        return await sepidarContext.vwInvoice
            .AsNoTracking()
            .Where(x => x.QuotationNumber == quotationNumber)
            .ToListAsync();
    }

    public async Task<List<SepidarInvoiceDto>> GetInvoicesByQuotationNumbersAsync(int[] serialNumbers, CancellationToken ct = default)
    {
        try
        {
            return await sepidarContext.vwInvoice
                .AsNoTracking()
                .Where(x => x.QuotationNumber.HasValue && Enumerable.Contains(serialNumbers, x.QuotationNumber.Value))
                .Select(x => new SepidarInvoiceDto {
                    QuotationNumber = x.QuotationNumber.Value,
                    CreationDate = x.CreationDate
                })
                .ToListAsync(cancellationToken: ct);
        }
        catch (SqlException)
        {
            return null;
        }
    }


    public async Task<SepidarInvoiceDto> GetInvoiceSummaryAsync(int invoiceId)
    {
        return await sepidarContext.vwInvoice
            .AsNoTracking()
            .Where(x => x.InvoiceId == invoiceId)
            .Select(x => new SepidarInvoiceDto {
                InvoiceId = x.InvoiceId,
                Number = x.Number,
                Date = x.Date,
                DateTXT = x.DateTXT,
                CustomerPartyRef = x.CustomerPartyRef,
                CustomerPartyName = x.CustomerPartyName
            })
            .FirstOrDefaultAsync();
    }


    public string GetNoteInvoice(int? refId)
    {
        var note = sepidarContext.ExtraData
            .Where(f => f.EntityTypeName == "SG.Sales.InvoiceManagement.Common.DsInvoice" && f.EntityRef == refId)
            .FirstOrDefault().Note;
        return note;
    }


    //--------------------------------------------------------- TANKHAH ---------------------------------------------------------


    public async Task<(string entityName, string entityId)?> SendCashToSepidar(Cash cash, string tafsiliCode, int costCenterId,
        string tafsiliCash)
    {
        var dl = await GetDlByCodeAsync(tafsiliCode);
        var tafsiliId = dl.DLId;

        var sepidarCashId = await GetCashIdWithDlAsync(tafsiliCash);

        switch (dl.Type)
        {
            case 4: // حساب بانكي
                return await AddPaymentHeaderAsync(cash, tafsiliId, costCenterId, sepidarCashId);

            case 2: // طرف مقابل
                return await AddVoucherAsync2(cash, tafsiliId, costCenterId);

            default:
                return null;
        }
    }


    private async Task<DL> GetDlByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var dl = await sepidarContext.DLs.AsNoTracking().SingleOrDefaultAsync(x => x.Code == code, cancellationToken);

        if (dl == null)
        {
            throw new Exception($"حساب تفصیلی با کد {code} در سپیدار پیدا نشد.");
        }

        return dl;
    }

    private async Task<DL> GetDlByIdAsync(int dlId, CancellationToken cancellationToken = default)
    {
        var dl = await sepidarContext.DLs.AsNoTracking().SingleOrDefaultAsync(x => x.DLId == dlId, cancellationToken);

        if (dl == null)
        {
            throw new InvalidOperationException($"DL حساب بانکی با شناسه {dlId} در سپیدار پیدا نشد.");
        }

        return dl;
    }

    private async Task<int> GetCashIdWithDlAsync(string dlCode, CancellationToken cancellationToken = default)
    {
        var dl = await GetDlByCodeAsync(dlCode, cancellationToken);

        var cash = await sepidarContext.SepidarCash.Where(x => x.DlRef == dl.DLId).SingleOrDefaultAsync(cancellationToken);

        if (cash == null)
        {
            throw new Exception($"صندوق با کد تفصیلی {dlCode} در سپیدار پیدا نشد.");
        }

        return cash.CashId;
    }

    private async Task<int> GetFiscalYearIdAsync(DateTime date, CancellationToken cancellationToken = default)
    {
        var fiscalYear = await sepidarContext.FiscalYears.AsNoTracking()
            .Where(x => x.StartDate <= date && x.EndDate >= date)
            .Select(x => new {
                x.FiscalYearId,
                x.Title,
                x.StartDate,
                x.EndDate
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (fiscalYear == null)
        {
            throw new Exception($"سال مالی برای تاریخ {date:yyyy-MM-dd} در سپیدار پیدا نشد.");
        }

        return fiscalYear.FiscalYearId;
    }


    private async Task<CurrentFinancialYearDto> GetCurrentFinancialYearSpidar()
    {
        var currentYearSpidar = await sepidarContext.FiscalYears
            .AsNoTracking()
            .OrderByDescending(f => f.StartDate)
            .Select(f => new CurrentFinancialYearDto {
                FiscalYearId = f.FiscalYearId,
                Title = f.Title
            })
            .FirstOrDefaultAsync();

        if (currentYearSpidar == null)
        {
            throw new Exception("سال مالی یافت نشد.");
        }

        return currentYearSpidar;
    }


    private static void CheckFiscalYearWithCRM(string crmDate, CurrentFinancialYearDto currentFinancialYear)
    {
        var cashYear = int.Parse(TextService.ToEnglishNumber(crmDate?.Substring(0, 4)));
        var sepidarYear = int.Parse(TextService.ToEnglishNumber(currentFinancialYear?.Title));

        if (cashYear != sepidarYear)
        {
            throw new Exception("سال مالی با تاریخ پرداخت مغایرت دارد");
        }
    }


    private async Task<int> GetAccountIdByMoeinCodeAsync(string moeinCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(moeinCode))
            throw new Exception("کد معین خالی است.");

        // مثال:
        // 7704 -> 7 / 7 / 04

        var parts = new List<string> {
            moeinCode.Substring(0, 1)
        };

        if (moeinCode.Length >= 2)
            parts.Add(moeinCode.Substring(1, 1));

        if (moeinCode.Length > 2)
            parts.Add(moeinCode.Substring(2));

        var parentId = -5;

        foreach (var code in parts)
        {
            var accounts = await sepidarContext.Accounts
                .Where(x => x.ParentAccountRef == parentId && x.Code == code)
                .Select(x => new {
                    x.AccountId,
                    x.Code,
                    x.Title
                })
                .ToListAsync(cancellationToken);

            if (accounts.Count == 0)
            {
                throw new Exception($"بخش {code} از کد معین {moeinCode} در سپیدار پیدا نشد. ParentAccountRef = {parentId}");
            }

            if (accounts.Count > 1)
            {
                throw new Exception($"برای کد {code} و ParentAccountRef = {parentId} چند حساب پیدا شد.");
            }

            parentId = accounts[0].AccountId;
        }

        return parentId;
    }


    private async Task<int> GetNextIdAsync(string tableName, int incValue = 1, CancellationToken ct = default)
    {
        var connection = sepidarContext.Database.GetDbConnection();

        await using var command = connection.CreateCommand();

        command.CommandText = @"
                DECLARE @Id INT;

                EXEC FMK.spGetNextId
                    @TableName = @TableName,
                    @Id = @Id OUTPUT,
                    @IncValue = @IncValue;

                SELECT @Id;
            ";

        var tableParameter = command.CreateParameter();
        tableParameter.ParameterName = "@TableName";
        tableParameter.Value = tableName;

        var incParameter = command.CreateParameter();
        incParameter.ParameterName = "@IncValue";
        incParameter.Value = incValue;

        command.Parameters.Add(tableParameter);
        command.Parameters.Add(incParameter);

        var transaction = sepidarContext.Database.CurrentTransaction;

        if (transaction != null)
        {
            command.Transaction = transaction.GetDbTransaction();
        }

        var result = await command.ExecuteScalarAsync(ct);


        return Convert.ToInt32(result);
    }


    private async Task<int> GetBankAccountIdByDlRefAsync(int dlRef, CancellationToken ct = default)
    {
        var bankAccountRef = await sepidarContext.SepidarBankAccounts
            .AsNoTracking()
            .Where(f => f.DlRef == dlRef)
            .Select(f => f.BankAccountId)
            .FirstOrDefaultAsync(ct);


        return bankAccountRef;
    }


    private async Task<SepidarBankAccount> GetBankAccountByIdAsync(CancellationToken ct, int bankAccountId)
    {
        var bankAccount = await sepidarContext.SepidarBankAccounts
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.BankAccountId == bankAccountId, ct);

        if (bankAccount == null)
            throw new InvalidOperationException($"حساب بانکی سپیدار با شناسه {bankAccountId} پیدا نشد.");

        return bankAccount;
    }


    //--------------------------------------------------------- PaymentHeader ---------------------------------------------------------
    public async Task<PaymentHeader> GetPaymentHeader(int id, CancellationToken cancellationToken = default)
    {
        var paymentHeader = await sepidarContext.PaymentHeaders
            .AsNoTracking()
            .Where(x => x.PaymentHeaderId == id)
            .FirstOrDefaultAsync(cancellationToken);

        return paymentHeader;
    }

    private async Task<int> GetNextPaymentHeaderNumberAsync(int fiscalYearId, CancellationToken cancellationToken = default)
    {
        var lastNumber = await sepidarContext.PaymentHeaders
            .AsNoTracking()
            .Where(x => x.FiscalYearRef == fiscalYearId)
            .MaxAsync(x => (int?)x.Number, cancellationToken) ?? 0;

        return lastNumber + 1;
    }


    private async Task<(string, string)> AddPaymentHeaderAsync(Cash cash, int tafsiliId, int costCenterId, int sepidarCashId,
        CancellationToken ct = default)
    {
        await using var trans = await sepidarContext.Database.BeginTransactionAsync(ct);

        try
        {
            // حساب معین
            var moeinCode = cash.CostTitle.MoeinCode.ToString();
            var accountSlRef = await GetAccountIdByMoeinCodeAsync(moeinCode, ct);

            // سال مالی
            var currentFinancialYear = await GetCurrentFinancialYearSpidar();
            CheckFiscalYearWithCRM(cash.CostDate, currentFinancialYear);
            var fiscalYearId = currentFinancialYear.FiscalYearId;

            // شماره اعلامیه پرداخت
            var paymentNumber = await GetNextPaymentHeaderNumberAsync(fiscalYearId, ct);

            // PaymentHeaderId
            var paymentHeaderId = await GetNextIdAsync("RPA.PaymentHeader", ct: ct);

            var now = DateTime.Now;
            var paymentHeader = new PaymentHeader {
                PaymentHeaderId = paymentHeaderId,
                Type = 2, // پرداخت به سایر // FMK.lookup
                AccountSlRef = accountSlRef,
                DlRef = costCenterId,
                Number = paymentNumber,
                Date = cash.CostDateMiladi,
                CurrencyRef = 1, // ریال
                Description = cash.CostDescription ?? string.Empty,
                Description_En = cash.CostDescription ?? string.Empty,
                Discount = 0,
                TotalAmount = cash.Amount,
                ItemType = 4, // فعلا
                State = 2, // فعلا
                CreatorForm = "0", // فعلا
                Creator = 1,
                CreationDate = now,
                LastModifier = 1,
                LastModificationDate = now,
                Version = 1,
                Rate = 1,
                CashRef = sepidarCashId, // صندوق
                Amount = 0,
                AmountInBaseCurrency = 0,
                ReceiptHeaderRef = null,
                FiscalYearRef = fiscalYearId,
                VoucherRef = null,
                DiscountRate = 1,
                DiscountInBaseCurrency = 0,
                TotalAmountInBaseCurrency = cash.Amount,

                // اگر کارمزد وجود دارد
                BankFeeSlRef = cash.Fee > 0 ? await GetAccountIdByMoeinCodeAsync("7704", ct) : null
            };


            // Add PaymentDraft
            var paymentDraft = await GeneratePaymentDraftAsync(cash, tafsiliId, paymentHeader, ct);
            paymentHeader.PaymentDrafts = new List<PaymentDraft>();
            paymentHeader.PaymentDrafts.Add(paymentDraft);

            sepidarContext.PaymentHeaders.Add(paymentHeader);
            await sepidarContext.SaveChangesAsync(ct);

            // Add Voucher & VoucherItem
            var voucher = await AddVoucherAsync(cash, paymentHeader, ct);
            paymentHeader.Voucher = voucher;
            paymentHeader.Version = 2;

            await sepidarContext.SaveChangesAsync(ct);
            await trans.CommitAsync(ct);

            return (nameof(PaymentHeader), paymentHeader.PaymentHeaderId.ToString());
        }
        catch (Exception e)
        {
            await trans.RollbackAsync(ct);
            Console.WriteLine(e);
            throw;
        }
    }


    //--------------------------------------------------------- PaymentDraft ---------------------------------------------------------

    private async Task<PaymentDraft> GeneratePaymentDraftAsync(Cash cash, int dlRef, PaymentHeader paymentHeader, CancellationToken ct = default)
    {
        // پیدا کردن حساب بانکی سپیدار
        var bankAccountRef = await GetBankAccountIdByDlRefAsync(dlRef, ct);

        //  ساخت PaymentDraft
        var paymentDraftId = await GetNextIdAsync("RPA.PaymentDraft", ct: ct);

        var paymentDraft = new PaymentDraft {
            PaymentDraftId = paymentDraftId,
            Number = "0", // شماره پیگیری پرداخت
            Date = cash.CostDateMiladi,
            Amount = cash.Amount,
            BankAccountRef = bankAccountRef,
            Description = cash.CostDescription,
            Description_En = "",
            Version = 1,
            PaymentHeaderRef = paymentHeader.PaymentHeaderId,
            HeaderNumber = paymentHeader.Number,
            HeaderDate = paymentHeader.Date,
            CurrencyRef = paymentHeader.CurrencyRef,
            Rate = 1,
            AmountInBaseCurrency = cash.Amount,
            BankFeeAmount = cash.Fee > 0 ? cash.Fee : null,
            BankFeeAmountInBaseCurrency = cash.Fee > 0 ? cash.Fee : null,
            BankFeeAmountRate = 1
        };

        return paymentDraft;
    }


    private async Task<PaymentDraft> GetPaymentDraftByPaymentHeader(PaymentHeader paymentHeader, CancellationToken ct)
    {
        var paymentDraft = await sepidarContext.PaymentDrafts.AsNoTracking()
            .FirstOrDefaultAsync(x => x.PaymentHeaderRef == paymentHeader.PaymentHeaderId, ct);

        if (paymentDraft == null)
            throw new InvalidOperationException($"PaymentDraft مربوط به اعلامیه پرداخت {paymentHeader.Number} پیدا نشد.");

        if (!paymentDraft.BankAccountRef.HasValue)
            throw new InvalidOperationException("حساب بانکی PaymentDraft مشخص نشده است.");

        return paymentDraft;
    }

    //--------------------------------------------------------- VOUCHER ---------------------------------------------------------
    private async Task<int> GetNextVoucherNumberAsync(int fiscalYearId, CancellationToken ct = default)
    {
        var lastNumber = await sepidarContext.Vouchers
            .AsNoTracking()
            .Where(x => x.FiscalYearRef == fiscalYearId)
            .MaxAsync(x => (int?)x.Number, ct) ?? 0;

        return lastNumber + 1;
    }

    private async Task<int> GetNextVoucherDailyNumberAsync(DateTime date, int fiscalYearId, CancellationToken ct = default)
    {
        var lastDailyNumber = await sepidarContext.Vouchers
            .AsNoTracking()
            .Where(x => x.FiscalYearRef == fiscalYearId && x.Date.Date == date.Date)
            .MaxAsync(x => (int?)x.DailyNumber, ct) ?? 0;

        return lastDailyNumber + 1;
    }

    private const string PaymentIssuerEntityName2 =
        "SG.Accounting.VoucherManagement.Common.DsVoucher, " +
        "SG.Accounting.VoucherManagement.Common, " +
        "Version=1.0.0.0, Culture=neutral, PublicKeyToken=null";

    private async Task<(string, string)> AddVoucherAsync2(Cash cash, int tafsiliId, int costCenterId, CancellationToken ct = default)
    {
        await using var trans = await sepidarContext.Database.BeginTransactionAsync(ct);

        try
        {
            var voucherId = await GetNextIdAsync("ACC.Voucher", ct: ct);

            var currentFinancialYear = await GetCurrentFinancialYearSpidar();
            CheckFiscalYearWithCRM(cash.CostDate, currentFinancialYear);
            var fiscalYearId = currentFinancialYear.FiscalYearId;

            var voucherNumber = await GetNextVoucherNumberAsync(fiscalYearId, ct);
            var dailyNumber = await GetNextVoucherDailyNumberAsync(cash.CostDateMiladi, fiscalYearId, ct);

            var voucher = new Voucher {
                VoucherId = voucherId,
                Number = voucherNumber,
                Date = cash.CostDateMiladi,
                ReferenceNumber = voucherNumber,
                State = 1,
                Type = 1,
                FiscalYearRef = fiscalYearId,
                Description = cash.CostDescription,
                Description_En = cash.CostDescription,
                Version = 1,
                Creator = 1,
                CreationDate = DateTime.Now,
                LastModifier = 1,
                LastModificationDate = DateTime.Now,
                DailyNumber = dailyNumber,
                IssuerSystem = 3,
                IsMerged = false,
                MergedIssuerSystem = null
            };

            var voucherItems = new List<VoucherItem>();


            var voucherItemCount = cash.Fee > 0 ? 4 : 2;
            var lastVoucherItemId = await GetNextIdAsync("ACC.VoucherItem", voucherItemCount, ct);
            var firstVoucherItemId = lastVoucherItemId - voucherItemCount + 1;

            // =========================================================
            // Row 1 - بدهکار: حساب هزینه
            // =========================================================

            var moeinCode = cash.CostTitle.MoeinCode.ToString();
            var accountSlRef = await GetAccountIdByMoeinCodeAsync(moeinCode, ct);

            var debitDlRef = await GetVoucherItemDlRefAsync(accountSlRef, costCenterId, ct);

            var debitItem = new VoucherItem {
                VoucherItemId = firstVoucherItemId,
                VoucherRef = voucher.VoucherId,
                RowNumber = 1,

                AccountSLRef = accountSlRef,

                DLRef = debitDlRef,

                Debit = cash.Amount,

                Credit = 0,

                CurrencyRef = null,
                CurrencyRate = null,
                CurrencyDebit = null,
                CurrencyCredit = null,

                TrackingNumber = null, // تفاوت بین دو فانکشن مشترک
                TrackingDate = null, // تفاوت بین دو فانکشن مشترک

                IssuerEntityName = PaymentIssuerEntityName2, //// تفاوت بین دو فانکشن مشترک
                IssuerEntityRef = firstVoucherItemId,
                Description = cash.CostDescription,
                Description_En = cash.CostDescription,
                Version = 1
            };

            voucherItems.Add(debitItem);


            if (cash.Fee > 0)
            {
                const string feeDescription = "بابت هزینه کارمزد خدمات بانکی";

                voucherItems.Add(new VoucherItem {
                    VoucherItemId = firstVoucherItemId + 1,
                    VoucherRef = voucher.VoucherId,

                    RowNumber = voucherItems.Count + 1,

                    AccountSLRef = await GetAccountIdByMoeinCodeAsync("7704", ct),

                    DLRef = null,

                    Debit = cash.Fee,
                    Credit = 0,

                    CurrencyRef = null,
                    CurrencyRate = null,
                    CurrencyDebit = null,
                    CurrencyCredit = null,

                    TrackingNumber = null,
                    TrackingDate = null,

                    IssuerEntityName = PaymentIssuerEntityName2,
                    IssuerEntityRef = firstVoucherItemId + 1,

                    Description = feeDescription,
                    Description_En = feeDescription,

                    Version = 1
                });
            }


            // =========================================================
            // Row 2 - بستانکار: حساب بانکی
            // =========================================================

            var bankAccountSlRef = await GetAccountIdByMoeinCodeAsync("1002", ct); // تفاوت بین دو فانکشن مشترک
            var creditItem = new VoucherItem {
                VoucherItemId = firstVoucherItemId + (cash.Fee > 0 ? 2 : 1),
                VoucherRef = voucher.VoucherId,

                RowNumber = voucherItems.Count + 1,

                AccountSLRef = bankAccountSlRef,

                DLRef = tafsiliId,

                Debit = 0,
                Credit = cash.Amount,

                CurrencyRef = null,
                CurrencyRate = null,
                CurrencyDebit = null,
                CurrencyCredit = null,

                TrackingNumber = null,
                TrackingDate = null,

                IssuerEntityName = PaymentIssuerEntityName2,
                IssuerEntityRef = firstVoucherItemId + (cash.Fee > 0 ? 2 : 1),

                Description = cash.CostDescription,
                Description_En = cash.CostDescription,

                Version = 1
            };

            voucherItems.Add(creditItem);


            if (cash.Fee > 0)
            {
                voucherItems.Add(new VoucherItem {
                    VoucherItemId = firstVoucherItemId + 3,
                    VoucherRef = voucher.VoucherId,

                    RowNumber = voucherItems.Count + 1,

                    AccountSLRef = bankAccountSlRef,

                    DLRef = tafsiliId,

                    Debit = 0,
                    Credit = cash.Fee,

                    CurrencyRef = null,
                    CurrencyRate = null,
                    CurrencyDebit = null,
                    CurrencyCredit = null,

                    TrackingNumber = null,
                    TrackingDate = null,

                    IssuerEntityName = PaymentIssuerEntityName2,
                    IssuerEntityRef = firstVoucherItemId + 3,

                    Description = cash.CostDescription,
                    Description_En = cash.CostDescription,

                    Version = 1
                });
            }

            voucher.VoucherItems = voucherItems;

            sepidarContext.Vouchers.Add(voucher);
            await sepidarContext.SaveChangesAsync(ct);
            await trans.CommitAsync(ct);
            return (nameof(Voucher), voucher.VoucherId.ToString());
        }
        catch (Exception e)
        {
            await trans.RollbackAsync(ct);

            Console.WriteLine(e);
            throw;
        }
    }


    private async Task<Voucher> AddVoucherAsync(Cash cash, PaymentHeader paymentHeader, CancellationToken ct = default)
    {
        // شماره سند
        var voucherNumber = await GetNextVoucherNumberAsync(paymentHeader.FiscalYearRef, ct);

        // شماره روزانه
        var dailyNumber = await GetNextVoucherDailyNumberAsync(cash.CostDateMiladi, paymentHeader.FiscalYearRef, ct);

        // PaymentHeaderId
        var voucherId = await GetNextIdAsync("ACC.Voucher", ct: ct);

        // ساخت Voucher

        var description =
            $"بابت پرداخت طی اعلامیه پرداخت شماره " +
            $"{paymentHeader.Number} تاریخ " +
            $"{cash.CostDate?.Substring(0, 10)}";

        var now = DateTime.Now;

        var voucher = new Voucher {
            VoucherId = voucherId,
            Number = voucherNumber,

            Date = cash.CostDateMiladi,

            ReferenceNumber = voucherNumber, // هنوز قطعی نکرده‌ایم همیشه برابر Number باشد

            SecondaryNumber = null,

            State = 1, // فعلا
            Type = 2, // فعلا

            FiscalYearRef = paymentHeader.FiscalYearRef,

            Description = description,
            Description_En = description,

            Version = 1,
            Creator = 1,
            CreationDate = now,
            LastModifier = 1,
            LastModificationDate = now,

            DailyNumber = dailyNumber,
            IssuerSystem = 2, // فعلا
            IsMerged = false,
            MergedIssuerSystem = null
        };


        // ساخت VoucherItem
        voucher.VoucherItems = await GenerateVoucherItemsAsync(cash, paymentHeader, voucher, ct);

        sepidarContext.Vouchers.Add(voucher);

        return voucher;
    }


    //--------------------------------------------------------- VoucherItem ---------------------------------------------------------

    private async Task<int?> GetVoucherItemDlRefAsync(int accountSlRef, int dlRef, CancellationToken cancellationToken = default)
    {
        var account = await sepidarContext.Accounts
            .AsNoTracking()
            .Where(x => x.AccountId == accountSlRef)
            .Select(x => new {
                x.HasDL
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (account == null)
            throw new InvalidOperationException($"حساب {accountSlRef} در سپیدار پیدا نشد.");

        return account.HasDL ? dlRef : null;
    }

    private async Task<(string TrackingNumber, DateTime? TrackingDate)> GetTrackingAsync(int accountId, PaymentDraft paymentDraft,
        CancellationToken cancellationToken = default)
    {
        var hasTracking = await sepidarContext.Accounts
            .AsNoTracking()
            .Where(x => x.AccountId == accountId)
            .Select(x => x.HasTracking)
            .SingleOrDefaultAsync(cancellationToken);

        if (!hasTracking)
            return (null, null);

        return (paymentDraft.Number, paymentDraft.Date);
    }


    private const string PaymentIssuerEntityName =
        "SG.ReceiptAndPayment.PaymentManagement.Common.DsPayment, " +
        "SG.ReceiptAndPayment.PaymentManagement.Common, " +
        "Version=1.0.0.0, Culture=neutral, PublicKeyToken=null";

    private async Task<List<VoucherItem>> GenerateVoucherItemsAsync(Cash cash, PaymentHeader paymentHeader, Voucher voucher,
        CancellationToken ct = default)
    {
        var result = new List<VoucherItem>();

        // حساب بانکی PaymentDraft
        var paymentDraft = await GetPaymentDraftByPaymentHeader(paymentHeader, ct);

        var bankAccount = await GetBankAccountByIdAsync(ct, paymentDraft.BankAccountRef.Value);

        var bankAccountSlRef = await GetAccountIdByMoeinCodeAsync("1001", ct);

        var bankDl = await GetDlByIdAsync(bankAccount.DlRef, ct);

        var paymentHeaderDl = await GetDlByIdAsync(paymentHeader.DlRef, ct);

        var debitDescription =
            $"برداشت طی حواله شماره {paymentDraft.Number ?? "0"} " +
            $"تاریخ {cash.CostDate?.Substring(0, 10)} " +
            $"از حساب بانکی {bankDl.Title} " +
            $"بابت {cash.CostDescription}";

        var creditDescription =
            $"برداشت طی حواله شماره {paymentDraft.Number ?? "0"} " +
            $"تاریخ {cash.CostDate} " +
            $"از حساب بانکی {bankDl.Title} " +
            $"به {paymentHeaderDl.Title} " +
            $"بابت {cash.CostDescription}";


        var debitTracking = await GetTrackingAsync(paymentHeader.AccountSlRef.Value, paymentDraft, ct);

        var debitDlRef = await GetVoucherItemDlRefAsync(paymentHeader.AccountSlRef.Value, paymentHeader.DlRef, ct);


        // Tracking حساب بانک
        var creditTracking = await GetTrackingAsync(bankAccountSlRef, paymentDraft, ct);


        // کارمزد
        (string feeTrackingNumber, DateTime? feeTrackingDate) feeTracking = (null, null);

        if (cash.Fee > 0)
        {
            if (!paymentHeader.BankFeeSlRef.HasValue)
                throw new InvalidOperationException("حساب معین کارمزد بانکی مشخص نشده است.");

            feeTracking = await GetTrackingAsync(paymentHeader.BankFeeSlRef.Value, paymentDraft, ct);
        }


        var voucherItemCount = cash.Fee > 0 ? 4 : 2;

        var lastVoucherItemId = await GetNextIdAsync("ACC.VoucherItem", voucherItemCount, ct);

        var firstVoucherItemId = lastVoucherItemId - voucherItemCount + 1;


        // =========================================================
        // Row 1 - بدهکار: حساب هزینه
        // =========================================================

        var debitItem = new VoucherItem {
            VoucherItemId = firstVoucherItemId,
            VoucherRef = voucher.VoucherId,

            RowNumber = 1,

            AccountSLRef = paymentHeader.AccountSlRef.Value,

            DLRef = debitDlRef,

            Debit = cash.Amount,

            Credit = 0,

            // در نمونه‌های PaymentManagement ریالی این فیلدها NULL بودند
            CurrencyRef = null,
            CurrencyRate = null,
            CurrencyDebit = null,
            CurrencyCredit = null,

            TrackingNumber = debitTracking.TrackingNumber,
            TrackingDate = debitTracking.TrackingDate,

            IssuerEntityName = PaymentIssuerEntityName,
            IssuerEntityRef = paymentHeader.PaymentHeaderId,
            Description = debitDescription,
            Description_En = debitDescription,
            Version = 1
        };

        result.Add(debitItem);


        if (cash.Fee > 0)
        {
            const string feeDescription = "بابت هزینه کارمزد خدمات بانکی";

            result.Add(new VoucherItem {
                VoucherItemId = firstVoucherItemId + 1,
                VoucherRef = voucher.VoucherId,

                RowNumber = result.Count + 1,

                AccountSLRef = paymentHeader.BankFeeSlRef!.Value,

                DLRef = null,

                Debit = cash.Fee,
                Credit = 0,

                CurrencyRef = null,
                CurrencyRate = null,
                CurrencyDebit = null,
                CurrencyCredit = null,

                // در نمونه واقعی کارمزد این‌ها وجود داشتند
                TrackingNumber = feeTracking.feeTrackingNumber,
                TrackingDate = feeTracking.feeTrackingDate,

                IssuerEntityName = PaymentIssuerEntityName,
                IssuerEntityRef = paymentHeader.PaymentHeaderId,

                Description = feeDescription,
                Description_En = feeDescription,

                Version = 1
            });
        }


        // =========================================================
        // Row 2 - بستانکار: حساب بانکی
        // =========================================================

        var creditItem = new VoucherItem {
            VoucherItemId = firstVoucherItemId + (cash.Fee > 0 ? 2 : 1),
            VoucherRef = voucher.VoucherId,

            RowNumber = result.Count + 1,

            AccountSLRef = bankAccountSlRef, // TODO: Mapping به‌جای 717

            DLRef = bankAccount.DlRef,

            Debit = 0,
            Credit = cash.Amount,

            CurrencyRef = null,
            CurrencyRate = null,
            CurrencyDebit = null,
            CurrencyCredit = null,

            TrackingNumber = creditTracking.TrackingNumber,
            TrackingDate = creditTracking.TrackingDate,

            IssuerEntityName = PaymentIssuerEntityName,
            IssuerEntityRef = paymentHeader.PaymentHeaderId,

            Description = creditDescription,
            Description_En = creditDescription,

            Version = 1
        };

        result.Add(creditItem);


        if (cash.Fee > 0)
        {
            result.Add(new VoucherItem {
                VoucherItemId = firstVoucherItemId + 3,
                VoucherRef = voucher.VoucherId,

                RowNumber = result.Count + 1,

                AccountSLRef = bankAccountSlRef,

                DLRef = bankAccount.DlRef,

                Debit = 0,
                Credit = cash.Fee,

                CurrencyRef = null,
                CurrencyRate = null,
                CurrencyDebit = null,
                CurrencyCredit = null,

                TrackingNumber = creditTracking.TrackingNumber,
                TrackingDate = creditTracking.TrackingDate,

                IssuerEntityName = PaymentIssuerEntityName,
                IssuerEntityRef = paymentHeader.PaymentHeaderId,

                Description = creditDescription,
                Description_En = creditDescription,

                Version = 1
            });
        }

        return result;
    }

    //--------------------------------------------------------- QUOTATION ---------------------------------------------------------

    public async Task<List<SepidarPartyDto>> GetSepidarCustomersAsync(CancellationToken ct = default)
    {
        return await sepidarContext.Parties
            .AsNoTracking()
            .Where(x => x.IsCustomer)
            .OrderBy(x => x.Name)
            .Select(x => new SepidarPartyDto {
                Id = x.PartyId,
                DlCode = x.Dl.Code,
                DlRef = x.DLRef,
                FullName = TextService.NormalizePersianText(x.Name + " " + x.LastName),
            })
            .ToListAsync(ct);
    }

    public async Task<SepidarPartyDto> GetSepidarCustomerByIdAsync(int id, CancellationToken ct = default)
    {
        return await sepidarContext.Parties
            .AsNoTracking()
            .Where(x => x.IsCustomer)
            .Where(x => x.PartyId == id)
            .Select(x => new SepidarPartyDto {
                Id = x.PartyId,
                DlCode = x.Dl.Code,
                DlRef = x.DLRef,
                FullName = x.Name + " " + x.LastName,
                PartyMainAddressRef = x.PartyAddresses.Where(p => p.IsMain == 1).Select(p => (int?)p.PartyAddressId).FirstOrDefault()
            })
            .SingleOrDefaultAsync(ct);
    }

    public async Task<List<SepidarItemDto>> GetSepidarItemsAsync(CancellationToken ct = default)
    {
        return await sepidarContext.Items
            .AsNoTracking()
            .Where(x => x.IsActive && x.Sellable)
            .OrderBy(x => x.Code)
            .Select(x => new SepidarItemDto {
                Id = x.ItemID,
                Code = x.Code,
                Title = TextService.NormalizePersianText(x.Title),
                DefaultStockRef = x.DefaultStockRef
            })
            .ToListAsync(ct);
    }

    public async Task<List<SelectOptionDto<int>>> GetSepidarUnitsAsync(CancellationToken ct = default)
    {
        return await sepidarContext.Units
            .AsNoTracking()
            .Select(x => new SelectOptionDto<int> {
                Text = TextService.NormalizePersianText(x.Title),
                Value = x.UnitID
            })
            .ToListAsync(ct);
    }


    public async Task<List<SaleType>> GetSaleTypes(CancellationToken ct = default)
    {
        return await sepidarContext.SaleTypes
            .AsNoTracking()
            .OrderBy(x => x.Number)
            .Select(x => new SaleType {
                SaleTypeId = x.SaleTypeId,
                Number = x.Number,
                Title = TextService.NormalizePersianText(x.Title),
            })
            .ToListAsync(ct);
    }

    public async Task<SaleType> GetSaleTypeByIdAsync(int id, CancellationToken ct = default)
    {
        return await sepidarContext.SaleTypes
            .AsNoTracking()
            .Where(x => x.SaleTypeId == id)
            .Select(x => new SaleType {
                SaleTypeId = x.SaleTypeId,
                Number = x.Number,
                Title = TextService.NormalizePersianText(x.Title),
            })
            .SingleOrDefaultAsync(ct);
    }

    public async Task<List<SepidarStockDto>> GetStocks(CancellationToken ct = default)
    {
        return await sepidarContext.Stocks
            .AsNoTracking()
            .Where(f => f.IsActive)
            .Select(x => new SepidarStockDto {
                Id = x.StockID,
                Code = x.Code,
                Title = TextService.NormalizePersianText(x.Title),
            }).ToListAsync(ct);
    }

    public async Task<(string entityName, string entityId)?> SendPreFactorToSepidar(PreFactor preFactor, int externalCustomerId, int saleTypeId,
        List<ExternalPreFactorItemMapping> itemMappings, CancellationToken ct = default)
    {
        // Customer validation
        var customer = await GetSepidarCustomerByIdAsync(externalCustomerId, ct);

        if (customer == null)
            throw new InvalidOperationException("مشتری انتخاب‌شده در نرم‌افزار مالی پیدا نشد.");

        // SaleType validation
        var saleType = await GetSaleTypeByIdAsync(saleTypeId, ct);

        if (saleType == null)
            throw new InvalidOperationException("نوع فروش انتخاب‌شده در نرم‌افزار مالی پیدا نشد.");


        // stocks validation
        var stocks = await GetStocks(ct);

        var selectedStockIds = itemMappings.Select(x => x.ExternalStockId).ToHashSet();
        var validStockIds = stocks.Where(x => selectedStockIds.Contains(x.Id)).Select(x => x.Id).ToHashSet();

        if (validStockIds.Count != selectedStockIds.Count)
            throw new InvalidOperationException("یک یا چند انبار انتخاب‌شده در نرم‌افزار مالی پیدا نشد.");


        // items validation
        var externalItems = await GetSepidarItemsAsync(ct);

        var selectedItemIds = itemMappings.Select(x => x.ExternalItemId).ToHashSet();
        var validItemIds = externalItems.Where(x => selectedItemIds.Contains(x.Id)).Select(x => x.Id).ToHashSet();

        if (validItemIds.Count != selectedItemIds.Count)
            throw new InvalidOperationException("یک یا چند کالای انتخاب‌شده در نرم‌افزار مالی پیدا نشد.");


        var mappingByItemId = itemMappings.ToDictionary(x => x.PreFactorItemId);

        var items = new List<SepidarQuotationItemRequest>();
        foreach (var item in preFactor?.PreFactorItems ?? [])
        {
            if (!mappingByItemId.TryGetValue(item.Id, out var mapping))
                throw new InvalidOperationException($"نگاشت کالای سپیدار برای کالای ({item.Product}) مشخص نشده است.");

            if (mapping.ExternalItemId <= 0)
                throw new InvalidOperationException($"کالای سپیدار برای کالای ({item.Product}) مشخص نشده است.");

            if (mapping.ExternalStockId <= 0)
                throw new InvalidOperationException($"انبار سپیدار برای کالای ({item.Product}) مشخص نشده است.");

            items.Add(new SepidarQuotationItemRequest {
                PreFactorItem = item,
                ItemRef = mapping.ExternalItemId,
                StockRef = mapping.ExternalStockId
            });
        }

        var request = new SepidarQuotationRequest {
            PreFactor = preFactor,
            Party = customer,
            Items = items,
            SaleTypeRef = saleTypeId
        };

        var quotationId = await CreateQuotationAsync(request, ct);

        return (nameof(Quotation), quotationId.ToString());
    }


    private async Task CheckQuotationNumberAsync(int serialNumber, CancellationToken ct)
    {
        var numberExists = await sepidarContext.Quotations
            .AsNoTracking()
            .AnyAsync(x => x.Number == serialNumber, ct);

        if (numberExists)
            throw new InvalidOperationException($"شماره پیش‌فاکتور {serialNumber} قبلاً در نرم‌افزار مالی ثبت شده است.");
    }

    private async Task<int> CreateQuotationAsync(SepidarQuotationRequest request, CancellationToken ct = default)
    {
        if (request.Items == null || request.Items.Count == 0)
            throw new InvalidOperationException("پیش‌فاکتور باید حداقل یک ردیف کالا داشته باشد.");

        await CheckQuotationNumberAsync(request.PreFactor.SerialNumber, ct);

        if (!request.PreFactor.DateMiladi.HasValue)
            throw new InvalidOperationException("تاریخ پیش‌فاکتور ثبت نشده است.");

        var date = request.PreFactor.DateMiladi.Value;

        await using var transaction = await sepidarContext.Database.BeginTransactionAsync(ct);
        try
        {
            // سال مالی
            var currentFinancialYear = await GetCurrentFinancialYearSpidar();
            CheckFiscalYearWithCRM(request.PreFactor.Date, currentFinancialYear);
            var fiscalYearId = currentFinancialYear.FiscalYearId;

            var quotationId = await GetNextIdAsync("SLS.Quotation", ct: ct);

            var now = DateTime.Now;

            var quotation = new Quotation {
                QuotationId = quotationId,
                CustomerPartyRef = request.Party.Id,

                SaleTypeRef = request.SaleTypeRef, // عادی - خرده فروشی

                PartyAddressRef = request.Party.PartyMainAddressRef,

                Number = request.PreFactor.SerialNumber,

                Date = date,
                ExpirationDate = date.AddYears(2), // فعلا

                CurrencyRef = 1, // ریال
                DeliveryLocationRef = 1, // آدرس مشتری

                Price = request.PreFactor.SumTotalAmount,
                PriceInBaseCurrency = request.PreFactor.SumTotalAmount,
                Discount = request.PreFactor.Discount,
                DiscountInBaseCurrency = request.PreFactor.Discount,
                Addition = 0,
                AdditionInBaseCurrency = 0,
                Tax = request.PreFactor.TotalTax,
                TaxInBaseCurrency = request.PreFactor.TotalTax,
                Duty = 0, // احتمالا عوارض گمرکی میشه // فعلا
                DutyInBaseCurrency = 0,
                NetPriceInBaseCurrency = request.PreFactor.PaymentAmount,

                Rate = 1,
                Version = 1,

                Creator = 1,
                CreationDate = now,
                LastModifier = 1,
                LastModificationDate = now,

                Closed = false,
                FiscalYearRef = fiscalYearId,

                ReceiptRef = null,
                PaymentRef = null,

                CustomerRealName = request.Party.FullName,
                CustomerRealName_En = request.Party.FullName,

                Guid = null,

                AdditionFactor_VatEffective = 0,
                AdditionFactorInBaseCurrency_VatEffective = 0,
                AdditionFactor_VatIneffective = 0,
                AdditionFactorInBaseCurrency_VatIneffective = 0
            };


            quotation.QuotationItems = await CreateQuotationItemsAsync(quotationId, request.Items, ct);

            sepidarContext.Quotations.Add(quotation);

            await sepidarContext.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            return quotationId;
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(ct);
            throw new DbUpdateException("ذخیره اطلاعات در دیتابیس با خطا مواجه شد.");
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    private async Task<List<QuotationItem>> CreateQuotationItemsAsync(int quotationId, List<SepidarQuotationItemRequest> items,
        CancellationToken ct = default)
    {
        if (items == null || items.Count == 0)
            throw new InvalidOperationException("ردیف کالایی برای ثبت وجود ندارد.");

        var quotationItems = new List<QuotationItem>();

        var itemCount = items.Count;
        var lastItemId = await GetNextIdAsync("SLS.QuotationItem", itemCount, ct);
        var firstItemId = lastItemId - itemCount + 1;


        for (var index = 0; index < itemCount; index++)
        {
            var mappedItem = items[index];
            var item = mappedItem.PreFactorItem;

            if (mappedItem.ItemRef <= 0)
                throw new InvalidOperationException($"کالای مقصد برای ردیف {index + 1} معتبر نیست.");

            if (!int.TryParse(item.WeightCount, out var weight) || weight <= 0)
                throw new InvalidOperationException($"تعداد کالا در ردیف {index + 1} باید بیشتر از صفر باشد.");


            quotationItems.Add(new QuotationItem {
                QuotationItemID = firstItemId + index,
                QuotationRef = quotationId,
                RowID = index + 1,
                ItemRef = mappedItem.ItemRef,

                TracingRef = null,

                StockRef = mappedItem.StockRef, // محصولات خريداري شده (انبار)

                Quantity = weight,
                SecondaryQuantity = null,
                Fee = item.UnitAmount,

                Price = item.TotalAmount,
                PriceInBaseCurrency = item.TotalAmount,

                Discount = item.Discount,
                DiscountInBaseCurrency = item.Discount,

                PriceInfoDiscountRate = 0,
                PriceInfoPriceDiscount = 0,
                PriceInfoPercentDiscount = 0,
                CustomerDiscount = 0,
                CustomerDiscountRate = 0,

                Addition = 0,
                AdditionInBaseCurrency = 0,
                Tax = item.TaxAmount,
                TaxInBaseCurrency = item.TaxAmount,
                Duty = 0, // فعلا
                DutyInBaseCurrency = 0,

                NetPriceInBaseCurrency = item.TotalAmount,
                Rate = 1,
                UsedQuantity = 0, // TODO احتمالا مقدار فاکتور شده

                Description = null, // فعلا
                DiscountQuotationItemRef = null,
                Description_En = null,

                ProductPackRef = null,
                ProductPackQuantity = null,

                AggregateAmountDiscountRate = 0,
                AggregateAmountPriceDiscount = 0,
                AggregateAmountPercentDiscount = 0,

                AdditionFactor_VatEffective = 0,
                AdditionFactorInBaseCurrency_VatEffective = 0,
                AdditionFactor_VatIneffective = 0,
                AdditionFactorInBaseCurrency_VatIneffective = 0,

                IsAggregateDiscountInvoiceItem = false,
                IsReadyForProductOrder = true,
            });
        }

        return quotationItems;
    }

    public async Task<bool?> EntityExistsAsync(string entityName, string entityId)
    {
        if (string.IsNullOrWhiteSpace(entityName) || string.IsNullOrWhiteSpace(entityId))
            return null;

        if (!int.TryParse(entityId, out var id))
            return false;

        return entityName switch {
            nameof(Voucher) => await sepidarContext.Vouchers
                .AsNoTracking()
                .AnyAsync(x => x.VoucherId == id),

            nameof(PaymentHeader) => await sepidarContext.PaymentHeaders
                .AsNoTracking()
                .AnyAsync(x => x.PaymentHeaderId == id),

            nameof(Quotation) => await sepidarContext.Quotations
                .AsNoTracking()
                .AnyAsync(x => x.QuotationId == id),

            _ => null
        };
    }

    public async Task SendItemToSepidar(ExternalSoftwareAddItemRequest request, CancellationToken ct = default)
    {
        var item = new Item {
            Title = request.itemTitle,
            UnitRef = request.itemUnit
        };
    }
}