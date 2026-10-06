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
using CRM.EndPoint.FrontEnd.Models.Factor;
using CRM.EndPoint.FrontEnd.Models.Sepidar;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

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

            int parentId = -5;

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

        private async Task<int> GetNextPaymentHeaderNumberAsync(CancellationToken cancellationToken = default)
        {
            var lastNumber = await s1Db.PaymentHeaders.MaxAsync(x => (int?)x.Number, cancellationToken) ?? 0;

            return lastNumber + 1;
        }

        private async Task<int> GetCurrentYear(CancellationToken cancellationToken = default)
        {
            var lastNumber = await s1Db.PaymentHeaders.MaxAsync(x => (int?)x.Number, cancellationToken) ?? 0;

            return lastNumber + 1;
        }


        public async Task<int> GetNextIdAsync(string tableName, CancellationToken cancellationToken = default)
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

            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync(cancellationToken);

            var result = await command.ExecuteScalarAsync(cancellationToken);

            return Convert.ToInt32(result);
        }


        public async Task AddPaymentHeaderAsync(Cash cash, CancellationToken cancellationToken = default)
        {
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
            var currentYearSpidar = await GetCurrentFinancialYearSpidar();

            var cashDate = Int32.Parse(TextService.ToEnglishNumber(cash.CostDate?.Substring(0, 4)));
            var currentYear = Int32.Parse(TextService.ToEnglishNumber(currentYearSpidar?.Title));
            if (cashDate != currentYear)
            {
                throw new Exception("سال مالی با تاریخ پرداخت مغایرت دارد");
            }

            // -----------------------------
            // 4. شماره اعلامیه پرداخت
            // -----------------------------

            var paymentNumber = await GetNextPaymentHeaderNumberAsync(cancellationToken);

            // -----------------------------
            // 4. PaymentHeader
            // -----------------------------


            var paymentHeaderId = await GetNextIdAsync("RPA.PaymentHeader", cancellationToken);

            var paymentHeader = new PaymentHeader {
                PaymentHeaderId = paymentHeaderId, // بعداً با spGetNextId  //REQUIRED
                Type = 2, // فعلا
                AccountSlRef = accountSlRef,
                DlRef = dlRef,
                Number = paymentNumber, //REQUIRED
                Date = cash.CostDateMiladi,
                CurrencyRef = 1, // فعلا  //REQUIRED
                Description = cash.CostDescription ?? string.Empty,
                Description_En = cash.CostDescription ?? string.Empty,
                Discount = 0,
                // مبلغ اصلی پرداخت
                TotalAmount = cash.Amount,
                ItemType = 4, // فعلا
                State = 2, // فعلا
                CreatorForm = "0", // فعلا
                Creator = 1,
                CreationDate = DateTime.Now,
                LastModifier = 1,
                LastModificationDate = DateTime.Now,
                Version = 2, // فعلا
                Rate = 1,
                CashRef = 2, // صندوق مرکزی: 2 | صندوق غير رسمي:17
                Amount = 0,
                AmountInBaseCurrency = 0,
                ReceiptHeaderRef = null,
                FiscalYearRef = currentYearSpidar.FiscalYearId, //REQUIRED
                VoucherRef = null,
                DiscountRate = 1,
                DiscountInBaseCurrency = 0,
                TotalAmountInBaseCurrency = cash.Amount,

                // اگر کارمزد وجود دارد
                BankFeeSlRef = cash.Fee > 0 ? await GetAccountIdByMoeinCodeAsync("7704", cancellationToken) : null
            };


            s1Db.PaymentHeaders.Add(paymentHeader);


            await s1Db.SaveChangesAsync();
        }

        private async Task<CurrentFinancialYearDto> GetCurrentFinancialYearSpidar()
        {
            var currentYearSpidar = await s1Db.FiscalYears.AsNoTracking().OrderByDescending(f => f.StartDate)
                .Select(f => new CurrentFinancialYearDto() {FiscalYearId = f.FiscalYearId,Title = f.Title })
                .FirstOrDefaultAsync();
            return currentYearSpidar;
        }
    }
}