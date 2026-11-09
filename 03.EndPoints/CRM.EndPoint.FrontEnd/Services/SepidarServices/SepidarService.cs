using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.Cashes.Entities;
using CRM.Core.Domain.Factors.Entities;
using CRM.Core.Domain.PreFactors.Entities;
using CRM.Core.Domain.Sepidar.Banks.Entites;
using CRM.Core.Domain.Sepidar.DLs.Entites;
using CRM.Core.Domain.Sepidar.Invoices.Entities;
using CRM.Core.Domain.Sepidar.InventoryDeliveries.Entities;
using CRM.Core.Domain.Sepidar.Invoices.Models;
using CRM.Core.Domain.Sepidar.Items.Entities;
using CRM.Core.Domain.Sepidar.Payments.Entites;
using CRM.Core.Domain.Sepidar.Quotations.Entites;
using CRM.Core.Domain.Sepidar.SaleTypes.Entities;
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
        var openedHere = connection.State != ConnectionState.Open;

        if (openedHere)
            await sepidarContext.Database.OpenConnectionAsync(ct);

        try
        {
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
            command.Parameters.Add(tableParameter);

            var incParameter = command.CreateParameter();
            incParameter.ParameterName = "@IncValue";
            incParameter.Value = incValue;
            command.Parameters.Add(incParameter);

            var transaction = sepidarContext.Database.CurrentTransaction;
            if (transaction != null)
                command.Transaction = transaction.GetDbTransaction();

            var result = await command.ExecuteScalarAsync(ct);

            return Convert.ToInt32(result);
        }
        finally
        {
            if (openedHere)
                await sepidarContext.Database.CloseConnectionAsync();
        }
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
        catch (DbUpdateException)
        {
            await trans.RollbackAsync(ct);
            throw new DbUpdateException("ذخیره اطلاعات در دیتابیس با خطا مواجه شد.");
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
        catch (DbUpdateException)
        {
            await trans.RollbackAsync(ct);
            throw new DbUpdateException("ذخیره اطلاعات در دیتابیس با خطا مواجه شد.");
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

    public async Task<Quotation> GetQuotationAsync(int id, CancellationToken ct = default)
    {
        return await sepidarContext.Quotations
            .AsNoTracking()
            .Where(x => x.QuotationId == id)
            .SingleOrDefaultAsync(ct);
    }

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

    private async Task<Unit> GetSepidarUnitByIdAsync(int id, CancellationToken ct = default)
    {
        return await sepidarContext.Units
            .AsNoTracking()
            .Where(x => x.UnitID == id)
            .SingleOrDefaultAsync(ct);
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

    private async Task<SaleType> GetSaleTypeByIdAsync(int id, CancellationToken ct = default)
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

    public async Task<List<SepidarStockItemDto>> GetStocksByItemIds(List<int> refIds, CancellationToken ct = default)
    {
        return await sepidarContext.ItemStocks
            .AsNoTracking()
            .Where(x => refIds.Contains(x.ItemRef) && x.Stock.IsActive)
            .GroupBy(x => x.ItemRef)
            .Select(g => new SepidarStockItemDto {
                ItemRef = g.Key,
                Stocks = g.Select(x => new SepidarStockDto {
                    Id = x.Stock.StockID,
                    Code = x.Stock.Code,
                    Title = TextService.NormalizePersianText(x.Stock.Title),
                }).Distinct().ToList()
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

        var quotationId = await AddQuotationAsync(request, ct);

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

    private async Task<int> AddQuotationAsync(SepidarQuotationRequest request, CancellationToken ct = default)
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


            quotation.QuotationItems = await GenerateQuotationItemsAsync(quotationId, request.Items, ct);

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


    private async Task<List<QuotationItem>> GenerateQuotationItemsAsync(int quotationId, List<SepidarQuotationItemRequest> items,
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

            nameof(Invoice) => await sepidarContext.SalesInvoices
                .AsNoTracking()
                .AnyAsync(x => x.InvoiceId == id),

            _ => null
        };
    }


    //--------------------------------------------------------- Item ---------------------------------------------------------

    public async Task SendItemToSepidar(ExternalSoftwareAddItemRequest request, CancellationToken ct = default)
    {
        // unit validation
        var unit = await GetSepidarUnitByIdAsync(request.ItemUnit, ct);
        if (unit == null)
            throw new InvalidOperationException("واحد انتخاب‌شده در نرم‌افزار مالی پیدا نشد.");

        // code validation
        await CheckItemCodeAsync(request.ItemCode, ct);


        // stocks validation
        var stocks = await GetStocks(ct);

        var selectedStockIds = request.StockIds.ToHashSet();
        var validStockIds = stocks.Where(x => request.StockIds.Contains(x.Id)).Select(x => x.Id).ToHashSet();

        if (validStockIds.Count != selectedStockIds.Count)
            throw new InvalidOperationException("یک یا چند انبار انتخاب‌شده در نرم‌افزار مالی پیدا نشد.");

        await AddItemAsync(request, ct);
    }

    private async Task CheckItemCodeAsync(string code, CancellationToken ct = default)
    {
        var codeExists = await sepidarContext.Items
            .AsNoTracking()
            .Where(x => x.Code == code)
            .AnyAsync(ct);

        if (codeExists)
            throw new InvalidOperationException($"کد محصول {code} قبلاً در نرم‌افزار مالی ثبت شده است.");
    }

    private async Task AddItemAsync(ExternalSoftwareAddItemRequest request, CancellationToken ct)
    {
        await using var trans = await sepidarContext.Database.BeginTransactionAsync(ct);

        try
        {
            var itemId = await GetNextIdAsync("INV.Item", ct: ct);

            var now = DateTime.Now;

            var item = new Item {
                ItemID = itemId,
                Type = 1, // کالا: 1 | خدمت: 2 | کالای دارایی: 3
                Code = request.ItemCode,
                Title = request.ItemTitle,
                Title_En = request.ItemTitle,
                UnitRef = request.ItemUnit,
                DefaultStockRef = null, // فعلا
                Creator = 1,
                CreationDate = now,
                LastModifier = 1,
                LastModificationDate = now,
                Version = 0,
                IsActive = true,
                TaxRate = request.ItemTaxRate!.Value,
            };

            item.ItemStocks = await GenerateItemStocks(request.StockIds, ct);


            sepidarContext.Items.Add(item);
            await sepidarContext.SaveChangesAsync(ct);
            await trans.CommitAsync(ct);
        }
        catch (DbUpdateException)
        {
            await trans.RollbackAsync(ct);
            throw new DbUpdateException("ذخیره اطلاعات در دیتابیس با خطا مواجه شد.");
        }
        catch (Exception e)
        {
            await trans.RollbackAsync(ct);
            Console.WriteLine(e);
            throw;
        }
    }

    private async Task<List<ItemStock>> GenerateItemStocks(List<int> stockIds, CancellationToken ct)
    {
        var itemStockCount = stockIds.Count;
        var lastItemStockId = await GetNextIdAsync("INV.ItemStock", itemStockCount, ct);
        var firstItemStockId = lastItemStockId - itemStockCount + 1;

        var itemStocks = new List<ItemStock>();
        for (var index = 0; index < itemStockCount; index++)
        {
            var stockId = stockIds[index];

            itemStocks.Add(new ItemStock {
                ItemStockID = firstItemStockId + index,
                StockRef = stockId,
                Version = 1
            });
        }

        return itemStocks;
    }


    public async Task<string> GetLastItemCodeAsync(CancellationToken ct = default)
    {
        var lastCode = await sepidarContext.Items
            .AsNoTracking()
            .OrderByDescending(x => x.CreationDate)
            .Select(x => x.Code)
            .FirstOrDefaultAsync(cancellationToken: ct);


        return lastCode;
    }


    //--------------------------------------------------------- Invoice ---------------------------------------------------------

    // Reuse the selected Sepidar service/context for all readable preview references.
    // Item and stock lookups include historical (possibly inactive) records because
    // the exported invoice must use exactly the entities stored on the quotation.
    public async Task<SepidarInvoiceExportPreview> CompleteInvoiceExportPreviewAsync(
        SepidarInvoiceExportPreview preview, CancellationToken ct = default)
    {
        var quotation = await GetQuotationAsync(preview.QuotationId, ct)
                        ?? throw new InvalidOperationException("پیش‌فاکتور مقصد برای پیش‌نمایش پیدا نشد.");

        var customer = await GetSepidarCustomerByIdAsync(quotation.CustomerPartyRef, ct)
                       ?? throw new InvalidOperationException("مشتری پیش‌فاکتور مقصد پیدا نشد.");
        preview.CustomerName = string.IsNullOrWhiteSpace(customer.DlCode)
            ? customer.FullName
            : $"{customer.DlCode} - {customer.FullName}";

        var saleType = (await GetSaleTypes(ct))
                       .SingleOrDefault(x => x.SaleTypeId == quotation.SaleTypeRef)
                       ?? throw new InvalidOperationException("نوع فروش پیش‌فاکتور مقصد پیدا نشد.");
        preview.SaleTypeTitle = $"{saleType.Number} - {saleType.Title}";

        var itemIds = preview.Items.Select(x => x.ItemRef).Distinct().ToArray();
        var stockIds = preview.Items.Select(x => x.StockRef).Distinct().ToArray();

        var items = await sepidarContext.Items.AsNoTracking()
            .Where(x => itemIds.Contains(x.ItemID))
            .Select(x => new { x.ItemID, x.Code, x.Title })
            .ToDictionaryAsync(x => x.ItemID, ct);

        var stocks = await sepidarContext.Stocks.AsNoTracking()
            .Where(x => stockIds.Contains(x.StockID))
            .Select(x => new { x.StockID, x.Code, x.Title })
            .ToDictionaryAsync(x => x.StockID, ct);

        foreach (var line in preview.Items)
        {
            if (!items.TryGetValue(line.ItemRef, out var item))
                throw new InvalidOperationException(
                    $"ردیف {line.RowId}: کالای متصل به پیش‌فاکتور سپیدار ({line.ItemRef}) پیدا نشد.");
            if (!stocks.TryGetValue(line.StockRef, out var stock))
                throw new InvalidOperationException(
                    $"ردیف {line.RowId}: انبار متصل به پیش‌فاکتور سپیدار ({line.StockRef}) پیدا نشد.");

            line.ItemTitle = $"{item.Code} - {TextService.NormalizePersianText(item.Title)}";
            line.StockTitle = $"{stock.Code} - {TextService.NormalizePersianText(stock.Title)}";
        }

        return preview;
    }


    public async Task<int> AddInvoiceAsync(Factor factor, SepidarInvoiceExportPreview preview, CancellationToken ct = default)
    {
        if (preview?.Items == null || preview.Items.Count == 0)
            throw new InvalidOperationException("فاکتور باید حداقل یک ردیف کالا داشته باشد.");

        if (!factor.DateMiladi.HasValue)
            throw new InvalidOperationException("تاریخ فاکتور CRM معتبر نیست.");

        var quotation = await sepidarContext.Quotations.AsNoTracking().SingleOrDefaultAsync(x => x.QuotationId == preview.QuotationId, ct)
                        ?? throw new InvalidOperationException("پیش‌فاکتور مربوط در سپیدار پیدا نشد.");

        if (quotation.FiscalYearRef != preview.FiscalYearRef)
            throw new InvalidOperationException("سال مالی پیش‌فاکتور سپیدار تغییر کرده است.");

        await using var transaction = await sepidarContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try
        {
            // Serialize CRM exports, including inventory numbers and stock checks, per Sepidar database.
            await AcquireSepidarInvoiceExportLockAsync(ct);

            var invoiceNumber = await GetNextInvoiceNumberAsync(preview.FiscalYearRef, ct);
            var invoiceId = await GetNextIdAsync("SLS.Invoice", ct: ct);
            var now = DateTime.Now;

            var invoice = new SalesInvoiceRecord {
                InvoiceId = invoiceId,
                QuotationRef = quotation.QuotationId,
                CustomerPartyRef = quotation.CustomerPartyRef,
                CustomerRealName = quotation.CustomerRealName,
                CustomerRealName_En = quotation.CustomerRealName_En,
                SaleTypeRef = quotation.SaleTypeRef,
                PartyAddressRef = quotation.PartyAddressRef,
                Number = invoiceNumber,  // فعلا
                Date = factor.DateMiladi!.Value, // فعلا
                CurrencyRef = quotation.CurrencyRef,
                SLRef = 725, // فعلا
                DeliveryLocationRef = quotation.DeliveryLocationRef,
                State = 1, // ثبت: 1 | ابطال شده: 2
                Price = preview.Price,
                PriceInBaseCurrency = preview.Price,
                Discount = preview.Discount,
                DiscountInBaseCurrency = preview.Discount,
                Addition = 0,
                AdditionInBaseCurrency = 0,
                Tax = preview.Tax,
                TaxInBaseCurrency = preview.Tax,
                Duty = 0,
                DutyInBaseCurrency = 0,
                NetPriceInBaseCurrency = preview.NetPrice,
                Rate = quotation.Rate,
                Version = 1,
                Creator = 1,
                CreationDate = now,
                LastModifier = 1,
                LastModificationDate = now,
                FiscalYearRef = quotation.FiscalYearRef,
                VoucherRef = null, // فعلا
                BaseOnInventoryDelivery = false,
                OrderRef = null,
                ShouldControlCustomerCredit = true,
                Guid = null,
                AdditionFactor_VatEffective = 0,
                AdditionFactorInBaseCurrency_VatEffective = 0,
                AdditionFactor_VatIneffective = 0,
                AdditionFactorInBaseCurrency_VatIneffective = 0,
                AgreementRef = null,
                TaxPayerBillIssueDateTime = factor.DateMiladi,
                SettlementType = 1, // نوع تسویه: 1: نقدی | 2: نسیه | 3: نقدی/نسیه
                Description = null,
                SignatureRef = null
            };

            invoice.InvoiceItems = await GenerateInvoiceItemsAsync(invoiceId, preview.Items, quotation.Rate, ct);

            // Lock and recalculate affected inventory rows before writing either document.
            var summaryTable = CreateInventorySummaryTable(invoice.InvoiceItems, invoice.FiscalYearRef);
            await LockInventoryStockSummaryAsync(summaryTable, ct);
            await UpdateInventoryStockSummaryAsync(summaryTable, ct);
            var balancesBefore = await ReadInventoryStockBalancesAsync(invoice.InvoiceItems, invoice.FiscalYearRef, ct);
            ValidateInventoryAvailability(invoice.InvoiceItems, balancesBefore);

            sepidarContext.SalesInvoices.Add(invoice);
            await sepidarContext.SaveChangesAsync(ct);

            // The delivery items reference the real newly allocated SLS.InvoiceItem IDs.
            await AddInventoryDeliveryAsync(invoice, ct);
            await UpdateInventoryStockSummaryAsync(summaryTable, ct);

            var balancesAfter = await ReadInventoryStockBalancesAsync(invoice.InvoiceItems, invoice.FiscalYearRef, ct);
            ValidateInventoryMovement(invoice.InvoiceItems, balancesBefore, balancesAfter);

            await transaction.CommitAsync(ct);
            return invoiceId;
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    private async Task<int> GetNextInvoiceNumberAsync(int fiscalYearId, CancellationToken ct = default)
    {
        if (sepidarContext.Database.CurrentTransaction == null)
            throw new InvalidOperationException("شماره‌گذاری فاکتور باید داخل تراکنش انجام شود.");

        var lastNumber = await sepidarContext.SalesInvoices.AsNoTracking()
            .Where(x => x.FiscalYearRef == fiscalYearId)
            .MaxAsync(x => (int?)x.Number, ct) ?? 0;

        return checked(lastNumber + 1);
    }


    private readonly record struct InventoryStockKey(int StockRef, int ItemRef, int? TracingRef);
    private readonly record struct InventoryStockBalance(decimal PhysicalQuantity, decimal UndeliveredSaleQuantity);

    private async Task AcquireSepidarInvoiceExportLockAsync(CancellationToken ct)
    {
        var transaction = sepidarContext.Database.CurrentTransaction?.GetDbTransaction() as SqlTransaction
            ?? throw new InvalidOperationException("Sepidar invoice export requires an active SQL transaction.");
        var connection = sepidarContext.Database.GetDbConnection() as SqlConnection
            ?? throw new InvalidOperationException("The Sepidar connection must be a SQL Server connection.");

        await using var command = new SqlCommand(@"
            DECLARE @LockResult int;
            EXEC @LockResult = sys.sp_getapplock
                @Resource = 'CRM.Sepidar.InvoiceAndDelivery',
                @LockMode = 'Exclusive',
                @LockOwner = 'Transaction',
                @LockTimeout = 30000;
            SELECT @LockResult;", connection, transaction);

        var result = Convert.ToInt32(await command.ExecuteScalarAsync(ct));
        if (result < 0)
            throw new InvalidOperationException("امکان قفل‌گذاری برای صدور فاکتور و خروج انبار سپیدار وجود ندارد.");
    }

    private static DataTable CreateInventorySummaryTable(
        IEnumerable<SalesInvoiceItemRecord> invoiceItems, int fiscalYearId)
    {
        var table = new DataTable();
        table.Columns.Add("StockID", typeof(int));
        table.Columns.Add("ItemID", typeof(int));
        table.Columns.Add("TracingID", typeof(int));
        table.Columns.Add("FiscalYearID", typeof(int));
        table.Columns.Add("FeedFromClosingOperation", typeof(bool));

        foreach (var key in invoiceItems
            .Select(x => new InventoryStockKey(x.StockRef, x.ItemRef, x.TracingRef))
            .Distinct()
            .OrderBy(x => x.StockRef).ThenBy(x => x.ItemRef).ThenBy(x => x.TracingRef))
        {
            table.Rows.Add(
                key.StockRef,
                key.ItemRef,
                key.TracingRef.HasValue ? (object)key.TracingRef.Value : DBNull.Value,
                fiscalYearId,
                false);
        }

        return table;
    }

    private SqlCommand CreateInventorySummaryCommand(
        string sql, CommandType commandType, DataTable summaryTable)
    {
        var transaction = sepidarContext.Database.CurrentTransaction?.GetDbTransaction() as SqlTransaction
            ?? throw new InvalidOperationException("Inventory changes require an active Sepidar SQL transaction.");
        var connection = sepidarContext.Database.GetDbConnection() as SqlConnection
            ?? throw new InvalidOperationException("The Sepidar connection must be a SQL Server connection.");

        var command = new SqlCommand(sql, connection, transaction) { CommandType = commandType };
        command.Parameters.Add(new SqlParameter("@SummaryTable", SqlDbType.Structured) {
            TypeName = "INV.SummaryRecordTable",
            Value = summaryTable
        });
        return command;
    }

    private async Task LockInventoryStockSummaryAsync(DataTable summaryTable, CancellationToken ct)
    {
        await using var command = CreateInventorySummaryCommand(
            "INV.spLockItemStockSummary", CommandType.StoredProcedure, summaryTable);
        await command.ExecuteNonQueryAsync(ct);
    }

    private async Task UpdateInventoryStockSummaryAsync(DataTable summaryTable, CancellationToken ct)
    {
        await using var command = CreateInventorySummaryCommand(
            "INV.spUpdateItemStockSummary", CommandType.StoredProcedure, summaryTable);

        // Recalculate reservations as well as stock and undelivered invoice quantities.
        command.Parameters.Add("@UpdateSaleWithReserve", SqlDbType.Bit).Value = true;
        command.Parameters.Add("@IncludeRegisteredOrders", SqlDbType.Bit).Value = false;
        await command.ExecuteNonQueryAsync(ct);
    }

    private async Task<Dictionary<InventoryStockKey, InventoryStockBalance>> ReadInventoryStockBalancesAsync(
        IEnumerable<SalesInvoiceItemRecord> invoiceItems, int fiscalYearId, CancellationToken ct)
    {
        var keys = invoiceItems
            .Select(x => new InventoryStockKey(x.StockRef, x.ItemRef, x.TracingRef))
            .Distinct()
            .ToArray();

        var stockRefs = keys.Select(x => x.StockRef).Distinct().ToArray();
        var itemRefs = keys.Select(x => x.ItemRef).Distinct().ToArray();

        // Query the mapped INV.ItemStockSummary entity. Use the item's primary unit,
        // as the summary can contain an additional row for the secondary unit.
        var summaries = await sepidarContext.ItemStockSummaries.AsNoTracking()
            .Where(x => x.FiscalYearRef == fiscalYearId
                && stockRefs.Contains(x.StockRef)
                && itemRefs.Contains(x.ItemRef)
                && x.UnitRef == x.Item.UnitRef)
            .Select(x => new {
                x.StockRef,
                x.ItemRef,
                x.TracingRef,
                x.Quantity,
                x.SaleQuantity
            })
            .ToListAsync(ct);

        var balances = keys.ToDictionary(
            x => x, _ => new InventoryStockBalance(0m, 0m));

        foreach (var summary in summaries)
        {
            var key = new InventoryStockKey(summary.StockRef, summary.ItemRef, summary.TracingRef);
            if (!balances.TryGetValue(key, out var current))
                continue;

            balances[key] = new InventoryStockBalance(
                current.PhysicalQuantity + summary.Quantity,
                current.UndeliveredSaleQuantity + (summary.SaleQuantity ?? 0m));
        }

        return balances;
    }

    private static void ValidateInventoryAvailability(
        IEnumerable<SalesInvoiceItemRecord> invoiceItems,
        IReadOnlyDictionary<InventoryStockKey, InventoryStockBalance> balances)
    {
        foreach (var group in invoiceItems.GroupBy(
            x => new InventoryStockKey(x.StockRef, x.ItemRef, x.TracingRef)))
        {
            if (!balances.TryGetValue(group.Key, out var balance))
                throw new InvalidOperationException(
                    $"موجودی کالای {group.Key.ItemRef} در انبار {group.Key.StockRef} قابل بررسی نیست.");

            // Existing undelivered invoices also represent demand against physical stock.
            // Open quotation reservations are deliberately not subtracted here: the
            // quotation being invoiced may already reserve the same quantity.
            var available = balance.PhysicalQuantity - Math.Max(0m, balance.UndeliveredSaleQuantity);
            var needed = group.Sum(x => x.Quantity);
            if (available < needed)
                throw new InvalidOperationException(
                    $"موجودی کالای {group.Key.ItemRef} در انبار {group.Key.StockRef} کافی نیست. " +
                    $"موجودی قابل خروج: {available:N4}، مقدار درخواستی: {needed:N4}.");
        }
    }

    private static void ValidateInventoryMovement(
        IEnumerable<SalesInvoiceItemRecord> invoiceItems,
        IReadOnlyDictionary<InventoryStockKey, InventoryStockBalance> before,
        IReadOnlyDictionary<InventoryStockKey, InventoryStockBalance> after)
    {
        foreach (var group in invoiceItems.GroupBy(
            x => new InventoryStockKey(x.StockRef, x.ItemRef, x.TracingRef)))
        {
            if (!before.TryGetValue(group.Key, out var previous)
                || !after.TryGetValue(group.Key, out var current))
                throw new InvalidOperationException("خلاصه موجودی پس از ثبت خروج انبار قابل بررسی نیست.");

            var expected = previous.PhysicalQuantity - group.Sum(x => x.Quantity);
            if (expected < 0 || current.PhysicalQuantity != expected)
                throw new InvalidOperationException(
                    $"موجودی کالای {group.Key.ItemRef} در انبار {group.Key.StockRef} " +
                    "پس از ثبت خروج با مقدار مورد انتظار مطابقت ندارد.");
        }
    }

    private async Task<int> GetNextInventoryDeliveryNumberAsync(
        int fiscalYearId, int stockRef, CancellationToken ct)
    {
        if (sepidarContext.Database.CurrentTransaction == null)
            throw new InvalidOperationException("Inventory delivery numbering requires a transaction.");

        // Sepidar data shows independent sequences per fiscal year, stock, type and return status.
        var lastNumber = await sepidarContext.InventoryDeliveries.AsNoTracking()
            .Where(x => x.FiscalYearRef == fiscalYearId
                && x.StockRef == stockRef
                && x.Type == 1 && !x.IsReturn)
            .MaxAsync(x => (int?)x.Number, ct) ?? 0;

        return checked(lastNumber + 1);
    }

    private async Task AddInventoryDeliveryAsync(SalesInvoiceRecord invoice, CancellationToken ct)
    {
        if (sepidarContext.Database.CurrentTransaction == null)
            throw new InvalidOperationException("Inventory delivery must use the invoice SQL transaction.");

        var receiverDlRef = await sepidarContext.Parties.AsNoTracking()
            .Where(x => x.PartyId == invoice.CustomerPartyRef)
            .Select(x => (int?)x.DLRef)
            .SingleOrDefaultAsync(ct);
        if (!receiverDlRef.HasValue || receiverDlRef.Value <= 0)
            throw new InvalidOperationException("تفصیلی مشتری فاکتور سپیدار پیدا نشد.");

        var costOfGoodsAccountId = await GetAccountIdByMoeinCodeAsync("6001", ct);
        if (!await sepidarContext.Accounts.AsNoTracking()
            .AnyAsync(x => x.AccountId == costOfGoodsAccountId && x.IsActive, ct))
            throw new InvalidOperationException("حساب معین 6001 سپیدار فعال نیست.");

        var now = DateTime.Now;
        foreach (var group in invoice.InvoiceItems.GroupBy(x => x.StockRef).OrderBy(x => x.Key))
        {
            if (!await sepidarContext.Stocks.AsNoTracking()
                .AnyAsync(x => x.StockID == group.Key && x.IsActive, ct))
                throw new InvalidOperationException($"انبار سپیدار با شناسه {group.Key} فعال نیست.");

            var deliveryId = await GetNextIdAsync("INV.InventoryDelivery", ct: ct);
            var deliveryNumber = await GetNextInventoryDeliveryNumberAsync(
                invoice.FiscalYearRef, group.Key, ct);
            var delivery = new InventoryDeliveryRecord {
                InventoryDeliveryID = deliveryId,
                IsReturn = false,
                Type = 1,
                StockRef = group.Key,
                ReceiverDLRef = receiverDlRef.Value,
                Number = deliveryNumber,
                Date = invoice.Date,
                TotalPrice = 0m,
                AccountingVoucherRef = null,
                FiscalYearRef = invoice.FiscalYearRef,
                DestinationStockRef = null,
                CreatorForm = 1,
                Creator = invoice.Creator,
                CreationDate = now,
                LastModifier = invoice.LastModifier,
                LastModificationDate = now,
                Version = 1,
                Description = null
            };

            delivery.Items = await GenerateInventoryDeliveryItemsAsync(
                deliveryId, invoice.Number, invoice.InvoiceItems
                    .Where(x => x.StockRef == group.Key)
                    .OrderBy(x => x.RowID).ToList(), costOfGoodsAccountId, ct);

            sepidarContext.InventoryDeliveries.Add(delivery);
        }

        await sepidarContext.SaveChangesAsync(ct);
    }

    private async Task<List<InventoryDeliveryItemRecord>> GenerateInventoryDeliveryItemsAsync(
        int deliveryId, int invoiceNumber, List<SalesInvoiceItemRecord> invoiceItems,
        int accountId, CancellationToken ct)
    {
        var itemCount = invoiceItems.Count;
        if (itemCount == 0)
            throw new InvalidOperationException("سند خروج باید حداقل یک ردیف داشته باشد.");

        var lastItemId = await GetNextIdAsync("INV.InventoryDeliveryItem", itemCount, ct);
        var firstItemId = lastItemId - itemCount + 1;
        var result = new List<InventoryDeliveryItemRecord>(itemCount);

        for (var index = 0; index < itemCount; index++)
        {
            var invoiceItem = invoiceItems[index];
            result.Add(new InventoryDeliveryItemRecord {
                InventoryDeliveryItemID = firstItemId + index,
                InventoryDeliveryRef = deliveryId,
                IsReturn = false,
                RowNumber = index + 1,
                BaseInvoiceItem = invoiceItem.InvoiceItemID,
                BaseInventoryDeliveryItem = null,
                BaseReturnedInvoiceItem = null,
                QuotationItemRef = null,
                ItemRef = invoiceItem.ItemRef,
                TracingRef = invoiceItem.TracingRef,
                Quantity = invoiceItem.Quantity,
                SecondaryQuantity = invoiceItem.SecondaryQuantity,
                RemainingQuantity = 0m,
                RemainingSecondaryQuantity = null,
                SLAccountRef = accountId,
                Price = null,
                Description = $"بابت قلم فاکتور شماره {invoiceNumber}، ردیف {invoiceItem.RowID}",
                Description_En = $"بابت قلم فاکتور شماره {invoiceNumber}، ردیف {invoiceItem.RowID}",
                Version = 1,
                ProductOrderRef = null,
                ParityCheck = null,
                WeighingRef = null,
                ItemRequestItemRef = null,
                ItemDescription = null
            });
        }

        return result;
    }

    private async Task<List<SalesInvoiceItemRecord>> GenerateInvoiceItemsAsync(int invoiceId, List<SepidarInvoiceExportItemPreview> items,
        decimal rate,
        CancellationToken ct = default)
    {
        if (items == null || items.Count == 0)
            throw new InvalidOperationException("فاکتور باید حداقل یک ردیف کالا داشته باشد.");

        var itemCount = items.Count;
        var lastItemId = await GetNextIdAsync("SLS.InvoiceItem", itemCount, ct);
        var firstItemId = lastItemId - itemCount + 1;
        var invoiceItems = new List<SalesInvoiceItemRecord>(itemCount);

        for (var index = 0; index < itemCount; index++)
        {
            var item = items[index];
            if (item.ItemRef <= 0 || item.StockRef <= 0 || item.Quantity <= 0 || item.QuotationItemId <= 0)
                throw new InvalidOperationException($"ردیف {item.RowId}: کالا، انبار یا تعداد فاکتور سپیدار نامعتبر است.");

            invoiceItems.Add(new SalesInvoiceItemRecord {
                InvoiceItemID = firstItemId + index,
                InvoiceRef = invoiceId,
                RowID = item.RowId,
                ItemRef = item.ItemRef,
                TracingRef = null,
                StockRef = item.StockRef,
                Quantity = item.Quantity,
                SecondaryQuantity = null,
                Fee = item.Fee,
                Price = item.Price,
                PriceInBaseCurrency = item.Price,
                Discount = item.Discount,
                DiscountInBaseCurrency = item.Discount,
                PriceInfoDiscountRate = 0,
                PriceInfoPriceDiscount = 0,
                PriceInfoPercentDiscount = 0,
                CustomerDiscount = 0,
                CustomerDiscountRate = 0,
                Addition = 0,
                AdditionInBaseCurrency = 0,
                Tax = item.Tax,
                TaxInBaseCurrency = item.Tax,
                Duty = 0,
                DutyInBaseCurrency = 0,
                NetPriceInBaseCurrency = item.NetPrice,
                Rate = rate,
                QuotationItemRef = item.QuotationItemId,
                Description = null,
                Description_En = null,
                DiscountInvoiceItemRef = null,
                ProductPackRef = null,
                ProductPackQuantity = null,
                AggregateAmountDiscountRate = 0,
                AggregateAmountPriceDiscount = 0,
                AggregateAmountPercentDiscount = 0,
                OrderItemRef = null,
                AdditionFactor_VatEffective = 0,
                AdditionFactorInBaseCurrency_VatEffective = 0,
                AdditionFactor_VatIneffective = 0,
                AdditionFactorInBaseCurrency_VatIneffective = 0,
                DiscountItemGroupRef = null,
                BankFeeForCurrencySale = 0,
                BankFeeForCurrencySaleInBaseCurrency = 0,
                IsAggregateDiscountInvoiceItem = false,
                TaxPayerCurrencyPurchaseRate = 0
            });
        }

        return invoiceItems;
    }
}