using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.ASettings.Enums;
using CRM.Core.Domain.Cashes.Entities;
using CRM.Core.Domain.Sepidar.DLs.Entites;
using CRM.Core.Domain.Sepidar.Invoices.Entities;
using CRM.Core.Domain.Sepidar.Payments.Entites;
using CRM.Core.Domain.Sepidar.Vouchers.Entites;
using CRM.EndPoint.FrontEnd.Models.Factor;
using CRM.EndPoint.FrontEnd.Models.Sepidar;
using CRM.Infrastructures.Sql;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace CRM.EndPoint.FrontEnd.Services.SepidarServices {
    public class SepidarService(SepidarContextDbBase sepidarContext, CRMContextDb db) {
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


        public async Task<(string entityName, string entityId)?> SentCashToSepidar(Cash cash, string tafsiliCode, int costCenterId,
            string tafsiliCash)
        {
            var dl = await GetDlAsync(tafsiliCode);
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


        private async Task<DL> GetDlAsync(string code, CancellationToken cancellationToken = default)
        {
            var dl = await sepidarContext.DLs.AsNoTracking().SingleOrDefaultAsync(x => x.Code == code, cancellationToken);

            if (dl == null)
            {
                throw new Exception($"حساب تفصیلی با کد {code} در سپیدار پیدا نشد.");
            }

            return dl;
        }

        private async Task<int> GetCashIdWithDlAsync(string dlCode, CancellationToken cancellationToken = default)
        {
            var dl = await GetDlAsync(dlCode, cancellationToken);

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


        private async Task<int> GetNextIdAsync(string tableName, int incValue = 1, CancellationToken cancellationToken = default)
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

            var result = await command.ExecuteScalarAsync(cancellationToken);


            return Convert.ToInt32(result);
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
            CancellationToken cancellationToken = default)
        {
            await using var trans = await sepidarContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                // 1. مرکز هزینه
//                var costCenterCode = cash.CostCenter.Code;
                // var dlRef = await GetCostCenterDlIdAsync(costCenterCode, cancellationToken);

                // 2. حساب معین
                var moeinCode = cash.CostTitle.MoeinCode.ToString();
                var accountSlRef = await GetAccountIdByMoeinCodeAsync(moeinCode, cancellationToken);

                // 3. سال مالی
                //var fiscalYearRef = await GetFiscalYearIdAsync(cash.CostDateMiladi, cancellationToken);
                var currentFinancialYear = await GetCurrentFinancialYearSpidar();

                CheckFiscalYearByCash(cash, currentFinancialYear);

                var fiscalYearId = currentFinancialYear.FiscalYearId;

                // 4. شماره اعلامیه پرداخت
                var paymentNumber = await GetNextPaymentHeaderNumberAsync(fiscalYearId, cancellationToken);

                // 5. PaymentHeaderId
                var paymentHeaderId = await GetNextIdAsync("RPA.PaymentHeader", cancellationToken: cancellationToken);

                var now = DateTime.Now;
                var paymentHeader = new PaymentHeader {
                    PaymentHeaderId = paymentHeaderId,
                    Type = 2, // پرداخت به سایر // جدول FMK.lookup
                    AccountSlRef = accountSlRef,
                    DlRef = costCenterId,
                    Number = paymentNumber,
                    Date = cash.CostDateMiladi,
                    CurrencyRef = 1, // فعلا
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
                    CashRef = sepidarCashId, // صندوق مرکزی: 2 | صندوق غير رسمي:17
                    Amount = 0,
                    AmountInBaseCurrency = 0,
                    ReceiptHeaderRef = null,
                    FiscalYearRef = fiscalYearId,
                    VoucherRef = null,
                    DiscountRate = 1,
                    DiscountInBaseCurrency = 0,
                    TotalAmountInBaseCurrency = cash.Amount,

                    // اگر کارمزد وجود دارد
                    BankFeeSlRef = cash.Fee > 0 ? await GetAccountIdByMoeinCodeAsync("7704", cancellationToken) : null
                };


                // Add PaymentDraft
                var paymentDraft = await GeneratePaymentDraftAsync(cash, tafsiliId, paymentHeader, cancellationToken);
                paymentHeader.PaymentDrafts = new List<PaymentDraft>();
                paymentHeader.PaymentDrafts.Add(paymentDraft);

                sepidarContext.PaymentHeaders.Add(paymentHeader);
                await sepidarContext.SaveChangesAsync(cancellationToken);

                // Add Voucher & VoucherItem
                var voucher = await AddVoucherAsync(cash, paymentHeader, cancellationToken);
                paymentHeader.Voucher = voucher;
                paymentHeader.Version = 2;

                await sepidarContext.SaveChangesAsync(cancellationToken);
                await trans.CommitAsync(cancellationToken);

                return (nameof(PaymentHeader), paymentHeader.PaymentHeaderId.ToString());
            }
            catch (Exception e)
            {
                await trans.RollbackAsync(cancellationToken);
                Console.WriteLine(e);
                throw;
            }
        }

        private static bool CheckFiscalYearByCash(Cash cash, CurrentFinancialYearDto currentFinancialYear)
        {
            var cashYear = int.Parse(TextService.ToEnglishNumber(cash.CostDate?.Substring(0, 4)));
            var sepidarYear = int.Parse(TextService.ToEnglishNumber(currentFinancialYear.Title));

            if (cashYear != sepidarYear)
            {
                throw new Exception("سال مالی با تاریخ پرداخت مغایرت دارد");
            }

            return true;
        }

        //--------------------------------------------------------- PaymentDraft ---------------------------------------------------------

        private async Task<int> GetSepidarBankAccountIdAsync(int dlRef, CancellationToken cancellationToken = default)
        {
            var bankAccountRef = await sepidarContext.SepidarBankAccounts
                .AsNoTracking()
                .Where(f => f.DlRef == dlRef)
                .Select(f => f.BankAccountId)
                .FirstOrDefaultAsync(cancellationToken);


            return bankAccountRef;
        }


        private async Task<PaymentDraft> GeneratePaymentDraftAsync(Cash cash, int dlRef, PaymentHeader paymentHeader,
            CancellationToken cancellationToken = default)
        {
            // 1. پیدا کردن حساب بانکی سپیدار
            var bankAccountRef = await GetSepidarBankAccountIdAsync(dlRef, cancellationToken);

            // 2. ساخت PaymentDraft
            var paymentDraftId = await GetNextIdAsync("RPA.PaymentDraft", cancellationToken: cancellationToken);

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
                CurrencyRef = paymentHeader.CurrencyRef, // ریال
                Rate = 1,
                AmountInBaseCurrency = cash.Amount,
                BankFeeAmount = cash.Fee > 0 ? cash.Fee : null,
                BankFeeAmountInBaseCurrency = cash.Fee > 0 ? cash.Fee : null,
                BankFeeAmountRate = 1
            };

            return paymentDraft;
        }

        //--------------------------------------------------------- VOUCHER ---------------------------------------------------------
        private async Task<int> GetNextVoucherNumberAsync(int fiscalYearId, CancellationToken cancellationToken = default)
        {
            var lastNumber = await sepidarContext.Vouchers
                .AsNoTracking()
                .Where(x => x.FiscalYearRef == fiscalYearId)
                .MaxAsync(x => (int?)x.Number, cancellationToken) ?? 0;

            return lastNumber + 1;
        }

        private async Task<int> GetNextVoucherDailyNumberAsync(DateTime date, int fiscalYearId, CancellationToken cancellationToken = default)
        {
            var lastDailyNumber = await sepidarContext.Vouchers
                .AsNoTracking()
                .Where(x => x.FiscalYearRef == fiscalYearId && x.Date.Date == date.Date)
                .MaxAsync(x => (int?)x.DailyNumber, cancellationToken) ?? 0;

            return lastDailyNumber + 1;
        }

        private const string PaymentIssuerEntityName2 =
            "SG.Accounting.VoucherManagement.Common.DsVoucher, " +
            "SG.Accounting.VoucherManagement.Common, " +
            "Version=1.0.0.0, Culture=neutral, PublicKeyToken=null";

        private async Task<(string, string)> AddVoucherAsync2(Cash cash, int tafsiliId, int costCenterId,
            CancellationToken cancellationToken = default)
        {
            await using var trans = await sepidarContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var voucherId = await GetNextIdAsync("ACC.Voucher", cancellationToken: cancellationToken);

                var currentFinancialYear = await GetCurrentFinancialYearSpidar();
                CheckFiscalYearByCash(cash, currentFinancialYear);
                var fiscalYearId = currentFinancialYear.FiscalYearId;

                var voucherNumber = await GetNextVoucherNumberAsync(fiscalYearId, cancellationToken);
                var dailyNumber = await GetNextVoucherDailyNumberAsync(cash.CostDateMiladi, fiscalYearId, cancellationToken);

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
                var lastVoucherItemId = await GetNextIdAsync("ACC.VoucherItem", voucherItemCount, cancellationToken);
                var firstVoucherItemId = lastVoucherItemId - voucherItemCount + 1;

                // =========================================================
                // Row 1 - بدهکار: حساب هزینه
                // =========================================================

                var moeinCode = cash.CostTitle.MoeinCode.ToString();
                var accountSlRef = await GetAccountIdByMoeinCodeAsync(moeinCode, cancellationToken);

                var debitDlRef = await GetVoucherItemDlRefAsync(accountSlRef, costCenterId, cancellationToken);

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

                        AccountSLRef = await GetAccountIdByMoeinCodeAsync("7704", cancellationToken),

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
//                const int bankAccountSlRef = 718; // تفاوت بین دو فانکشن مشترک
                var bankAccountSlRef = await GetAccountIdByMoeinCodeAsync("1002");
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
                await sepidarContext.SaveChangesAsync(cancellationToken);
                await trans.CommitAsync(cancellationToken);
                return (nameof(Voucher), voucher.VoucherId.ToString());
            }
            catch (Exception e)
            {
                await trans.RollbackAsync(cancellationToken);

                Console.WriteLine(e);
                throw;
            }
        }


        public async Task<Voucher> AddVoucherAsync(Cash cash, PaymentHeader paymentHeader, CancellationToken cancellationToken = default)
        {
            /*
            if (paymentHeader == null)
                throw new InvalidOperationException("اعلامیه پرداخت پیدا نشد.");

            if (!paymentHeader.AccountSlRef.HasValue)
                throw new InvalidOperationException("حساب معین اعلامیه پرداخت مشخص نشده است.");

            if (paymentHeader.FiscalYearRef <= 0)
                throw new InvalidOperationException("سال مالی اعلامیه پرداخت مشخص نشده است.");*/

//            await using var transaction = await s1Db.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                // 1. شماره سند
                var voucherNumber = await GetNextVoucherNumberAsync(paymentHeader.FiscalYearRef, cancellationToken);

                // 2. شماره روزانه
                var dailyNumber = await GetNextVoucherDailyNumberAsync(cash.CostDateMiladi, paymentHeader.FiscalYearRef, cancellationToken);


                var description =
                    $"بابت پرداخت طی اعلامیه پرداخت شماره " +
                    $"{paymentHeader.Number} تاریخ " +
                    $"{cash.CostDate?.Substring(0, 10)}";

                var now = DateTime.Now;


                // ---------------------------------------------
                // 4. PaymentHeader اطلاعات لازم برای VoucherItem
                //    را دارد، ولی قبل از ساخت Voucher باید
                //    Validationهای مربوط به آیتم را انجام دهیم.
                // ---------------------------------------------

                if (cash.Fee > 0 && !paymentHeader.BankFeeSlRef.HasValue)
                {
                    throw new InvalidOperationException("برای این پرداخت کارمزد ثبت شده ولی حساب معین کارمزد مشخص نشده است.");
                }


                // ---------------------------------------------
                // 5. حساب بانکی PaymentDraft
                // ---------------------------------------------

                // در این مرحله هنوز ID Voucher رزرو نشده.
                // PaymentDraft را برای Validation حساب بانکی می‌گیریم.

                // TODO:
                // اگر PaymentDraft را قبلاً ذخیره کرده‌ای،
                // بهتر است اینجا از همان رکورد موجود استفاده شود.
                var paymentDraft = await sepidarContext.PaymentDrafts.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.PaymentHeaderRef == paymentHeader.PaymentHeaderId, cancellationToken);

                if (paymentDraft == null)
                    throw new InvalidOperationException($"PaymentDraft مربوط به اعلامیه پرداخت {paymentHeader.Number} پیدا نشد.");

                if (!paymentDraft.BankAccountRef.HasValue)
                    throw new InvalidOperationException("حساب بانکی PaymentDraft مشخص نشده است.");


                // ---------------------------------------------
                // 6. حساب بانکی را بررسی می‌کنیم
                // ---------------------------------------------

                var bankAccount = await sepidarContext.SepidarBankAccounts
                    .AsNoTracking()
                    .SingleOrDefaultAsync(x => x.BankAccountId == paymentDraft.BankAccountRef.Value, cancellationToken);

                if (bankAccount == null)
                    throw new InvalidOperationException($"حساب بانکی سپیدار با شناسه {paymentDraft.BankAccountRef.Value} پیدا نشد.");


                // ---------------------------------------------
                // 7. DL بانک
                // ---------------------------------------------

                var bankDlExists = await sepidarContext.DLs
                    .AsNoTracking()
                    .AnyAsync(x => x.DLId == bankAccount.DlRef, cancellationToken);

                if (!bankDlExists)
                    throw new InvalidOperationException($"DL حساب بانکی با شناسه {bankAccount.DlRef} در سپیدار پیدا نشد.");


                // ---------------------------------------------
                // 8. DL مربوط به PaymentHeader
                // ---------------------------------------------

                var paymentHeaderDlExists = await sepidarContext.DLs
                    .AsNoTracking()
                    .AnyAsync(x => x.DLId == paymentHeader.DlRef, cancellationToken);

                if (!paymentHeaderDlExists)
                    throw new InvalidOperationException($"DL با شناسه {paymentHeader.DlRef} در سپیدار پیدا نشد.");


                // ---------------------------------------------
                // 9. حساب هزینه
                // ---------------------------------------------

                var expenseAccount = await sepidarContext.Accounts
                    .AsNoTracking()
                    .Where(x => x.AccountId == paymentHeader.AccountSlRef.Value)
                    .Select(x => new {
                        x.AccountId,
                        x.HasDL,
                        x.HasTracking
                    })
                    .SingleOrDefaultAsync(cancellationToken);

                if (expenseAccount == null)
                    throw new InvalidOperationException($"حساب معین {paymentHeader.AccountSlRef.Value} در سپیدار پیدا نشد.");


                // ---------------------------------------------
                // 10. حساب بانک
                // ---------------------------------------------

                // TODO: Mapping واقعی BankAccount -> AccountSLRef
                const int bankAccountSlRef = 717;

                var bankAccountEntity = await sepidarContext.Accounts
                    .AsNoTracking()
                    .Where(x => x.AccountId == bankAccountSlRef)
                    .Select(x => new { x.AccountId, x.HasTracking })
                    .SingleOrDefaultAsync(cancellationToken);

                if (bankAccountEntity == null)
                    throw new InvalidOperationException($"حساب معین بانک با شناسه {bankAccountSlRef} در سپیدار پیدا نشد.");


                // ---------------------------------------------
                // 11. حساب کارمزد
                // ---------------------------------------------

                if (cash.Fee > 0)
                {
                    var feeAccount = await sepidarContext.Accounts
                        .AsNoTracking()
                        .Where(x => x.AccountId == paymentHeader.BankFeeSlRef!.Value)
                        .Select(x => x.AccountId)
                        .SingleOrDefaultAsync(cancellationToken);

                    if (feeAccount == 0)
                        throw new InvalidOperationException($"حساب معین کارمزد با شناسه {paymentHeader.BankFeeSlRef!.Value} در سپیدار پیدا نشد.");
                }


                // =========================================================
                // از اینجا به بعد Validationهای لازم انجام شده
                // =========================================================

                // ---------------------------------------------
                // 12. PaymentHeaderId
                // ---------------------------------------------

                // ID ووچر را تازه اینجا رزرو می‌کنیم
                var voucherId = await GetNextIdAsync("ACC.Voucher", cancellationToken: cancellationToken);


                // ---------------------------------------------
                // 13. ساخت Voucher
                // ---------------------------------------------

                var voucher = new Voucher {
                    VoucherId = voucherId,
                    Number = voucherNumber, // TODO: منطق شماره‌گذاری باید قطعی شود

                    Date = cash.CostDateMiladi,

                    ReferenceNumber = voucherNumber, // TODO: هنوز قطعی نکرده‌ایم همیشه برابر Number باشد

                    SecondaryNumber = null,

                    State = 1, // مطابق Voucherهای واقعی PaymentManagement؛ معنای دقیق enum هنوز مشخص نشده
                    Type = 2, // مطابق Voucherهای ایجادشده توسط PaymentManagement؛ معنای دقیق enum هنوز مشخص نشده

                    FiscalYearRef = paymentHeader.FiscalYearRef,

                    Description = description,
                    Description_En = description,

                    Version = 1,
                    Creator = 1,
                    CreationDate = now,
                    LastModifier = 1,
                    LastModificationDate = now,

                    DailyNumber = dailyNumber, // TODO: منطق تولید باید قطعی شود
                    IssuerSystem = 2, // مطابق Voucherهای PaymentManagement؛ معنای enum هنوز استخراج نشده
                    IsMerged = false,
                    MergedIssuerSystem = null
                };


                // ---------------------------------------------
                // 14. ساخت VoucherItem
                // ---------------------------------------------

                voucher.VoucherItems = await GenerateVoucherItemsAsync(cash, paymentHeader, paymentDraft, voucher, cancellationToken);

                sepidarContext.Vouchers.Add(voucher);

                return voucher;

//                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
//                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
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

        private async Task<List<VoucherItem>> GenerateVoucherItemsAsync(
            Cash cash,
            PaymentHeader paymentHeader,
            PaymentDraft paymentDraft,
            Voucher voucher,
            CancellationToken cancellationToken = default)
        {
            var result = new List<VoucherItem>();

            // ---------------------------------------------
            // حساب بانکی پرداخت‌کننده
            // ---------------------------------------------

            if (!paymentDraft.BankAccountRef.HasValue)
                throw new InvalidOperationException("حساب بانکی PaymentDraft مشخص نشده است.");

            var bankAccount = await sepidarContext.SepidarBankAccounts
                .AsNoTracking()
                .SingleOrDefaultAsync(x => x.BankAccountId == paymentDraft.BankAccountRef.Value, cancellationToken);

            if (bankAccount == null)
                throw new InvalidOperationException($"حساب بانکی سپیدار با شناسه {paymentDraft.BankAccountRef.Value} پیدا نشد.");

            /*
             * در نمونه‌های واقعی:
             *
             * BankAccount 24 -> DLRef 954
             * VoucherItem Credit -> AccountSLRef 717 / DLRef 954
             *
             * BankAccount 28 -> DLRef 2191
             * VoucherItem Credit -> AccountSLRef 717 / DLRef 2191
             *
             * بنابراین DL بانک قطعی است.
             */

            /*
             * TODO:
             * در نمونه‌های واقعی حساب بانک ریالی AccountId = 717 بوده.
             * ولی هنوز منبع Mapping خودکار BankAccount -> AccountSLRef را
             * در دیتابیس پیدا نکرده‌ایم.
             *
             * فعلاً 717 را هاردکد نمی‌کنیم و از Mapping حساب معین می‌گیریم.
             *
             * اگر کد معین "01" مربوط به بانک‌های ریالی CRM/سپیدار است،
             * باید با همان الگوریتم سلسله‌مراتبی Account پیدا شود.
             */

//            const int bankAccountSlRef = 717; // TODO: فعلاً مطابق داده واقعی؛ بعداً Mapping شود
            var bankAccountSlRef = await GetAccountIdByMoeinCodeAsync("1001");

            var bankDl = await sepidarContext.DLs.AsNoTracking().SingleOrDefaultAsync(x => x.DLId == bankAccount.DlRef, cancellationToken);

            if (bankDl == null)
            {
                throw new InvalidOperationException($"DL حساب بانکی با شناسه {bankAccount.DlRef} در سپیدار پیدا نشد.");
            }

            var paymentHeaderDl = await sepidarContext.DLs.AsNoTracking().SingleOrDefaultAsync(x => x.DLId == paymentHeader.DlRef, cancellationToken);

            if (paymentHeaderDl == null)
            {
                throw new InvalidOperationException($"DL با شناسه {paymentHeader.DlRef} در سپیدار پیدا نشد.");
            }


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


            // =========================================================
            // Row 1 - بدهکار: حساب هزینه
            // =========================================================

            var debitTracking = await GetTrackingAsync(paymentHeader.AccountSlRef.Value, paymentDraft, cancellationToken);

            /*
             * نکته:
             * در بیشتر نمونه‌ها PaymentHeader.DlRef به VoucherItem منتقل شده.
             *
             * ولی تست AccountSLRef=850 داشت:
             * PaymentHeader.DlRef = 5
             * VoucherItem.DLRef = NULL
             *
             * چون Account 850، HasDL = false بود.
             *
             * پس باید HasDL حساب را بررسی کنیم.
             */
            var debitDlRef = await GetVoucherItemDlRefAsync(paymentHeader.AccountSlRef.Value, paymentHeader.DlRef, cancellationToken);


            // =========================================================
            // Tracking حساب بانک
            // =========================================================

            var creditTracking = await GetTrackingAsync(bankAccountSlRef, paymentDraft, cancellationToken);


            // =========================================================
            // کارمزد
            // =========================================================

            (string feeTrackingNumber, DateTime? feeTrackingDate) feeTracking = (null, null);

            if (cash.Fee > 0)
            {
                if (!paymentHeader.BankFeeSlRef.HasValue)
                    throw new InvalidOperationException("حساب معین کارمزد بانکی مشخص نشده است.");

                feeTracking = await GetTrackingAsync(paymentHeader.BankFeeSlRef.Value, paymentDraft, cancellationToken);
            }


            // =========================================================
            // اینجا تمام Validation و Queryهای مورد نیاز انجام شده
            // و از اینجا به بعد ID رزرو می‌کنیم
            // =========================================================

            var voucherItemCount = cash.Fee > 0 ? 4 : 2;

            var lastVoucherItemId = await GetNextIdAsync("ACC.VoucherItem", voucherItemCount, cancellationToken);

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

                TrackingNumber = debitTracking.TrackingNumber, // TODO
                TrackingDate = debitTracking.TrackingDate, // TODO

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

                    AccountSLRef = bankAccountSlRef, // TODO: Mapping حساب بانک

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
    }
}