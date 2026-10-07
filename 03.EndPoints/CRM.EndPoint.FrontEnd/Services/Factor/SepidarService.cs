using System;
using System.Collections.Generic;
using System.Data;
using CRM.Infrastructures.Sql;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.Cashes.Entities;
using CRM.Core.Domain.Sepidar.Invoices.Entities;
using CRM.Core.Domain.Sepidar.Payments.Entites;
using CRM.Core.Domain.Sepidar.Voucher.Entites;
using CRM.EndPoint.FrontEnd.Models.Factor;
using CRM.EndPoint.FrontEnd.Models.Sepidar;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace CRM.EndPoint.FrontEnd.Services.Factor {
    public class SepidarService(Sepidar1ContextDb s1Db) {
        public async Task<Invoice> GetInvoiceAsync(int? invoiceId)
        {
            return await s1Db.vwInvoice
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.InvoiceId == invoiceId);
        }

        public async Task<Invoice> GetInvoiceWithItemsAsync(int invoiceId)
        {
            return await s1Db.vwInvoice
                .AsNoTracking()
                .Include(x => x.InvoiceItems)
                .FirstOrDefaultAsync(x => x.InvoiceId == invoiceId);
        }

        public async Task<List<Invoice>> GetInvoicesByQuotationNumberAsync(int? quotationNumber)
        {
            return await s1Db.vwInvoice
                .AsNoTracking()
                .Where(x => x.QuotationNumber == quotationNumber)
                .ToListAsync();
        }

        public async Task<List<SepidarInvoiceDto>> GetInvoicesByQuotationNumbersAsync(int[] serialNumbers, CancellationToken ct = default)
        {
            try
            {
                return await s1Db.vwInvoice
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
            return await s1Db.vwInvoice
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
            var note = s1Db.ExtraData
                .Where(f => f.EntityTypeName == "SG.Sales.InvoiceManagement.Common.DsInvoice" && f.EntityRef == refId)
                .FirstOrDefault().Note;
            return note;
        }


        //--------------------------------------------------------- TANKHAH ---------------------------------------------------------
        private async Task<int> GetCostCenterDlIdAsync(string costCenterCode, CancellationToken cancellationToken = default)
        {
            var dl = await s1Db.DLs.AsNoTracking().SingleOrDefaultAsync(x => x.Code == costCenterCode, cancellationToken);

            if (dl == null)
            {
                throw new Exception($"مرکز هزینه با کد {costCenterCode} در سپیدار پیدا نشد.");
            }

            return dl.DLId;
        }

        private async Task<int> GetFiscalYearIdAsync(DateTime date, CancellationToken cancellationToken = default)
        {
            var fiscalYear = await s1Db.FiscalYears.AsNoTracking()
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
            var currentYearSpidar = await s1Db.FiscalYears
                .AsNoTracking()
                .OrderByDescending(f => f.StartDate)
                .Select(f => new CurrentFinancialYearDto {
                    FiscalYearId = f.FiscalYearId,
                    Title = f.Title
                })
                .FirstOrDefaultAsync();

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
                var accounts = await s1Db.Accounts
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


        private async Task<int> GetNextIdAsync(string tableName, CancellationToken cancellationToken = default)
        {
            var connection = s1Db.Database.GetDbConnection();

            await using var command = connection.CreateCommand();

            command.CommandText = @"
                DECLARE @Id INT;

                EXEC FMK.spGetNextId
                    @TableName = @TableName,
                    @Id = @Id OUTPUT,
                    @IncValue = 1;

                SELECT @Id;
            ";

            var parameter = command.CreateParameter();
            parameter.ParameterName = "@TableName";
            parameter.Value = tableName;

            command.Parameters.Add(parameter);
            var transaction = s1Db.Database.CurrentTransaction;

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
            var paymentHeader = await s1Db.PaymentHeaders
                .AsNoTracking()
                .Where(x => x.PaymentHeaderId == id)
                .FirstOrDefaultAsync(cancellationToken: cancellationToken);

            return paymentHeader;
        }

        private async Task<int> GetNextPaymentHeaderNumberAsync(int fiscalYearId, CancellationToken cancellationToken = default)
        {
            var lastNumber = await s1Db.PaymentHeaders
                .AsNoTracking()
                .Where(x => x.FiscalYearRef == fiscalYearId)
                .MaxAsync(x => (int?)x.Number, cancellationToken) ?? 0;

            return lastNumber + 1;
        }

        public async Task<int> AddPaymentHeaderAsync(Cash cash, CancellationToken cancellationToken = default)
        {
            await using var trans = await s1Db.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                if (cash == null)
                    throw new Exception("هیچ تنخواهی برای ارسال به سپیدار وجود ندارد.");

                if (cash.CostCenter == null)
                    throw new Exception("مرکز هزینه برای تنخواه مشخص نشده است.");

                if (cash.CostTitle == null)
                    throw new Exception("عنوان هزینه برای تنخواه مشخص نشده است.");


                // -----------------------------
                // 1. مرکز هزینه
                // -----------------------------

                var costCenterCode = cash.CostCenter.Code;
                var dlRef = await GetCostCenterDlIdAsync(costCenterCode, cancellationToken);

                // -----------------------------
                // 2. حساب معین
                // -----------------------------

                var moeinCode = cash.CostTitle.MoeinCode.ToString();
                var accountSlRef = await GetAccountIdByMoeinCodeAsync(moeinCode, cancellationToken);

                // -----------------------------
                // 3. سال مالی
                // -----------------------------

                //var fiscalYearRef = await GetFiscalYearIdAsync(cash.CostDateMiladi, cancellationToken);
                var currentFinancialYear = await GetCurrentFinancialYearSpidar();

                var cashYear = Int32.Parse(TextService.ToEnglishNumber(cash.CostDate?.Substring(0, 4)));
                var sepidarYear = Int32.Parse(TextService.ToEnglishNumber(currentFinancialYear.Title));

                if (cashYear != sepidarYear)
                {
                    throw new Exception("سال مالی با تاریخ پرداخت مغایرت دارد");
                }

                var fiscalYearId = currentFinancialYear.FiscalYearId;

                // -----------------------------
                // 4. شماره اعلامیه پرداخت
                // -----------------------------

                var paymentNumber = await GetNextPaymentHeaderNumberAsync(fiscalYearId, cancellationToken);

                // -----------------------------
                // 5. PaymentHeaderId
                // -----------------------------

                var paymentHeaderId = await GetNextIdAsync("RPA.PaymentHeader", cancellationToken);

                var paymentHeader = new PaymentHeader {
                    PaymentHeaderId = paymentHeaderId, //REQUIRED
                    Type = 2, // پرداخت به سایر // جدول  FMK.
                    AccountSlRef = accountSlRef,
                    DlRef = dlRef,
                    Number = paymentNumber, //REQUIRED
                    Date = cash.CostDateMiladi,
                    CurrencyRef = 1, // فعلا  //REQUIRED
                    Description = cash.CostDescription ?? string.Empty,
                    Description_En = cash.CostDescription ?? string.Empty,
                    Discount = 0,
                    TotalAmount = cash.Amount,
                    ItemType = 4, // فعلا
                    State = 2, // فعلا
                    CreatorForm = "0", // فعلا
                    Creator = 1,
                    CreationDate = DateTime.Now,
                    LastModifier = 1,
                    LastModificationDate = DateTime.Now,
                    Version = 1, // فعلا
                    Rate = 1,
                    CashRef = 2, // صندوق مرکزی: 2 | صندوق غير رسمي:17
                    Amount = 0,
                    AmountInBaseCurrency = 0,
                    ReceiptHeaderRef = null,
                    FiscalYearRef = fiscalYearId, //REQUIRED
                    VoucherRef = null,
                    DiscountRate = 1,
                    DiscountInBaseCurrency = 0,
                    TotalAmountInBaseCurrency = cash.Amount,

                    // اگر کارمزد وجود دارد
                    BankFeeSlRef = cash.Fee > 0 ? await GetAccountIdByMoeinCodeAsync("7704", cancellationToken) : null
                };


                // s1Db.PaymentHeaders.Add(paymentHeader);
                var paymentDraft = await GeneratePaymentDraftAsync(cash, paymentHeader, cancellationToken);
                IList<PaymentDraft> paymentDraftList = new List<PaymentDraft>() { paymentDraft };
                paymentHeader.PaymentDrafts = paymentDraftList;


                s1Db.PaymentHeaders.Add(paymentHeader);

                await s1Db.SaveChangesAsync(cancellationToken);

                await trans.CommitAsync(cancellationToken);

                return paymentHeaderId;
            }
            catch (Exception e)
            {
                await trans.RollbackAsync(cancellationToken);
                Console.WriteLine(e);
                throw;
            }
        }

        //--------------------------------------------------------- PaymentDraft ---------------------------------------------------------

        private async Task<int> GetSepidarBankAccountIdAsync(Cash cash, CancellationToken cancellationToken = default)
        {
            var accountCode = cash.BankAccount?.AccountCode.ToString();

            if (string.IsNullOrWhiteSpace(accountCode))
            {
                throw new InvalidOperationException($"کد حساب بانکی برای رکورد {cash.Serial} مشخص نشده است.");
            }

            var bankAccounts = await s1Db.SepidarBankAccounts
                .AsNoTracking()
                .Where(x => x.AccountNo == accountCode)
                .Select(x => new {
                    x.BankAccountId,
                    x.AccountNo,
                    x.DlRef
                })
                .ToListAsync(cancellationToken);

            if (bankAccounts.Count == 0)
            {
                throw new InvalidOperationException($"حساب بانکی با کد {accountCode} در سپیدار پیدا نشد.");
            }

            if (bankAccounts.Count > 1)
            {
                throw new InvalidOperationException($"چند حساب بانکی با کد {accountCode} در سپیدار پیدا شد.");
            }

            return bankAccounts[0].BankAccountId;
        }


        private async Task<PaymentDraft> GeneratePaymentDraftAsync(Cash cash, PaymentHeader paymentHeader,
            CancellationToken cancellationToken = default)
        { /*
            if (cash == null)
                throw new ArgumentNullException(nameof(cash));

            if (cash.Amount <= 0)
                throw new InvalidOperationException(
                    $"مبلغ هزینه برای رکورد {cash.Serial} معتبر نیست.");

            if (cash.BankAccount == null)
                throw new InvalidOperationException(
                    $"حساب بانکی برای رکورد {cash.Serial} مشخص نشده است.");

            if (!cash.PaymentVoucherNumber.HasValue)
                throw new InvalidOperationException(
                    $"شماره اعلامیه پرداخت برای رکورد {cash.Serial} مشخص نشده است.");
*/


            // ---------------------------------------
            // 1. پیدا کردن PaymentHeader
            // ---------------------------------------

            /*var paymentHeader = await s1Db.PaymentHeaders
                .AsNoTracking()
                .SingleOrDefaultAsync(x => x.Number == cash.PaymentVoucherNumber.Value, cancellationToken);

            if (paymentHeader == null)
            {
                throw new InvalidOperationException(
                    $"PaymentHeader با شماره اعلامیه " +
                    $"{cash.PaymentVoucherNumber.Value} " +
                    $"برای رکورد {cash.Serial} پیدا نشد.");
            }*/


            // ---------------------------------------
            // 2. پیدا کردن حساب بانکی سپیدار
            // ---------------------------------------

            //var bankAccountRef = await GetSepidarBankAccountIdAsync(cash, cancellationToken);


            // ---------------------------------------
            // 3. ساخت PaymentDraft
            // ---------------------------------------

            var paymentDraftId = await GetNextIdAsync("RPA.PaymentDraft", cancellationToken);


            var paymentDraft = new PaymentDraft {
                PaymentDraftId = paymentDraftId,
                Number = "0", // در رکورد تست سپیدار Number = 0 بود
                Date = cash.CostDateMiladi,
                Amount = cash.Amount,
                BankAccountRef = 28, //bankAccountRef, باید اصلاح شود
                Description = cash.CostDescription,
                Description_En = null,
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
            var lastNumber = await s1Db.Vouchers
                .AsNoTracking()
                .Where(x => x.FiscalYearRef == fiscalYearId)
                .MaxAsync(x => (int?)x.Number, cancellationToken) ?? 0;

            return lastNumber + 1;
        }

        private async Task<int> GetNextVoucherDailyNumberAsync(DateTime date, int fiscalYearId, CancellationToken cancellationToken = default)
        {
            var lastDailyNumber = await s1Db.Vouchers
                .AsNoTracking()
                .Where(x => x.FiscalYearRef == fiscalYearId && x.Date.Date == date.Date)
                .MaxAsync(x => (int?)x.DailyNumber, cancellationToken) ?? 0;

            return lastDailyNumber + 1;
        }


        public async Task AddVoucherAsync(Cash cash, PaymentHeader paymentHeader, CancellationToken cancellationToken = default)
        {
            var voucherId = await GetNextIdAsync("ACC.Voucher", cancellationToken);

            // TODO: منطق دقیق تولید Number ووچر هنوز مشخص نشده
            var voucherNumber = await GetNextVoucherNumberAsync(paymentHeader.FiscalYearRef, cancellationToken);

            // TODO: منطق دقیق DailyNumber هنوز باید قطعی شود
            var dailyNumber = await GetNextVoucherDailyNumberAsync(cash.CostDateMiladi, paymentHeader.FiscalYearRef, cancellationToken);

            var description = $"بابت پرداخت طی اعلامیه پرداخت شماره {paymentHeader.Number} تاریخ {cash?.CostDate?.Substring(0, 10)}";

            var now = DateTime.Now;

            var voucher = new Voucher {
                VoucherId = voucherId,
                Number = voucherNumber, // TODO: منطق شماره‌گذاری باید قطعی شود
                Date = cash.CostDateMiladi,

                // در نمونه‌های واقعی Number و ReferenceNumber برابر بودند
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

            //voucher.VoucherItems = await GenerateVoucherItemsAsync(cash, voucher, cancellationToken);

            s1Db.Vouchers.Add(voucher);

            await s1Db.SaveChangesAsync(cancellationToken);
        }


        //--------------------------------------------------------- VoucherItem ---------------------------------------------------------

        private async Task<int?> GetVoucherItemDlRefAsync(int accountSlRef, int dlRef, CancellationToken cancellationToken = default)
        {
            var account = await s1Db.Accounts
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

            var bankAccount = await s1Db.SepidarBankAccounts
                .AsNoTracking()
                .SingleOrDefaultAsync(x => x.BankAccountId == paymentDraft.BankAccountRef.Value, cancellationToken);

            if (bankAccount == null)
                throw new InvalidOperationException($"حساب بانکی {paymentDraft.BankAccountRef.Value} در سپیدار پیدا نشد.");

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
            var bankDlRef = bankAccount.DlRef;

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

            const int bankAccountSlRef = 717; // TODO: فعلاً مطابق داده واقعی؛ بعداً Mapping شود


            // =========================================================
            // Row 1 - بدهکار: حساب هزینه
            // =========================================================

            var debitItemId = await GetNextIdAsync("ACC.VoucherItem", cancellationToken);

            var debitItem = new VoucherItem {
                VoucherItemId = debitItemId,

                VoucherRef = voucher.VoucherId,

                RowNumber = 1,

                AccountSLRef = paymentHeader.AccountSlRef.Value,

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
                DLRef = await GetVoucherItemDlRefAsync(paymentHeader.AccountSlRef.Value, paymentHeader.DlRef, cancellationToken),

                Debit = cash.Amount,

                Credit = 0,

                // در نمونه‌های PaymentManagement ریالی این فیلدها NULL بودند
                CurrencyRef = null,
                CurrencyRate = null,
                CurrencyDebit = null,
                CurrencyCredit = null,

                /*
                 * در تست مالی:
                 * TrackingNumber = PaymentDraft.Number
                 * TrackingDate = PaymentDraft.Date
                 *
                 * ولی در تمام حساب‌ها الزاماً Tracking فعال نیست.
                 * TODO: بهتر است HasTracking حساب بررسی شود.
                 */
                TrackingNumber = paymentDraft.Number, // TODO
                TrackingDate = paymentDraft.Date, // TODO

                IssuerEntityName = PaymentIssuerEntityName,
                IssuerEntityRef = paymentHeader.PaymentHeaderId,
                Description = cash.CostDescription ?? string.Empty,
                Description_En = cash.CostDescription ?? string.Empty,
                Version = 1
            };

            result.Add(debitItem);

            if (cash.Fee > 0)
            {
                if (!paymentHeader.BankFeeSlRef.HasValue)
                    throw new InvalidOperationException("حساب معین کارمزد بانکی مشخص نشده است.");

                var feeDebitItemId = await GetNextIdAsync("ACC.VoucherItem", cancellationToken);

                result.Add(new VoucherItem {
                    VoucherItemId = feeDebitItemId,
                    VoucherRef = voucher.VoucherId,

                    RowNumber = result.Count + 1,

                    AccountSLRef = paymentHeader.BankFeeSlRef.Value,

                    DLRef = null,

                    Debit = cash.Fee,
                    Credit = 0,

                    CurrencyRef = null,
                    CurrencyRate = null,
                    CurrencyDebit = null,
                    CurrencyCredit = null,

                    // در نمونه واقعی کارمزد این‌ها وجود داشتند
                    TrackingNumber = paymentDraft.Number,
                    TrackingDate = paymentDraft.Date,

                    IssuerEntityName = PaymentIssuerEntityName,
                    IssuerEntityRef = paymentHeader.PaymentHeaderId,

                    Description = "بابت هزینه کارمزد خدمات بانکی",
                    Description_En = string.Empty,

                    Version = 1
                });
            }


            // =========================================================
            // Row 2 - بستانکار: حساب بانکی
            // =========================================================

            var creditItemId = await GetNextIdAsync("ACC.VoucherItem", cancellationToken);

            var creditItem = new VoucherItem {
                VoucherItemId = creditItemId,

                VoucherRef = voucher.VoucherId,

                RowNumber = result.Count + 1,

                AccountSLRef = bankAccountSlRef, // TODO: Mapping به‌جای 717

                DLRef = bankDlRef,

                Debit = 0,

                Credit = cash.Amount,

                CurrencyRef = null,
                CurrencyRate = null,
                CurrencyDebit = null,
                CurrencyCredit = null,

                TrackingNumber = null,
                TrackingDate = null,

                IssuerEntityName = PaymentIssuerEntityName,

                IssuerEntityRef = paymentHeader.PaymentHeaderId,

                Description = cash.CostDescription ?? string.Empty,

                Description_En = cash.CostDescription ?? string.Empty,

                Version = 1
            };


            result.Add(creditItem);

            if (cash.Fee > 0)
            {
                var feeCreditItemId = await GetNextIdAsync("ACC.VoucherItem", cancellationToken);

                result.Add(new VoucherItem {
                    VoucherItemId = feeCreditItemId,
                    VoucherRef = voucher.VoucherId,

                    RowNumber = result.Count + 1,

                    AccountSLRef = bankAccountSlRef, // TODO: Mapping حساب بانک

                    DLRef = bankDlRef,

                    Debit = 0,
                    Credit = cash.Fee,

                    CurrencyRef = null,
                    CurrencyRate = null,
                    CurrencyDebit = null,
                    CurrencyCredit = null,

                    TrackingNumber = null,
                    TrackingDate = null,

                    IssuerEntityName = PaymentIssuerEntityName,
                    IssuerEntityRef = paymentHeader.PaymentHeaderId,

                    Description = cash.CostDescription ?? string.Empty,
                    Description_En = cash.CostDescription ?? string.Empty,

                    Version = 1
                });
            }


            return result;
        }
    }
}