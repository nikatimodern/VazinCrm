#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.Deals.Entities;
using CRM.Core.Domain.Deals.Enums;
using CRM.Core.Domain.QualityControls.Entities;
using CRM.Core.Domain.QualityControls.Models;
using CRM.Core.Domain.QualityControls.Repositories;
using CRM.Framework.Domain;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using CRM.Infrastructures.Sql.Accountancies.QueryExtensions;
using CRM.Core.Domain.Accountancies.Enum;
using CRM.Core.Domain.Factors.Enum;
using System.Globalization;

namespace CRM.Infrastructures.Sql.QualityControls.Repositories;

public sealed class QualityControlRepository(CRMContextDb db) : IQualityControlRepository
{
    public async Task<QualityControlListResult> GetDealsAsync(QualityControlListRequest request, QualityControlAccess access, CancellationToken cancellationToken)
    {
        var deals = VisibleDeals(access);
        var recordsTotal = await deals.CountAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            deals = deals.Where(deal => deal.SerialNumber.ToString() == search || deal.CustomerCode.ToString() == search
                || (deal.Title != null && deal.Title.Contains(search))
                || (deal.CustomerName != null && deal.CustomerName.Contains(search))
                || (deal.CusCompanyName != null && deal.CusCompanyName.Contains(search)));
        }
        // Calculate order-status counts before applying quality-control status filters.
        var financialCounts = await deals.GroupBy(_ => 1)
            .Select(group => new QualityControlFinancialCounts(
                group.Count(),
                group.Count(deal => deal.FinancialStatus == DealFinancialStausEnum.تایید_شده),
                group.Count(deal => deal.FinancialStatus == DealFinancialStausEnum.تایید_نشده)))
            .FirstOrDefaultAsync(cancellationToken) ?? new(0, 0, 0);
        // Legacy rows have Status = NULL until the separate backfill is run.
        // For those rows only, derive the outcome from existing answers.
        deals = request.Status switch
        {
            QualityControlStatus.NotReviewed => deals.Where(deal =>
                !db.QualityControls.Any(control => control.DealId == deal.Id)),
            QualityControlStatus.Reviewed => deals.Where(deal =>
                db.QualityControls.Any(control => control.DealId == deal.Id)),
            QualityControlStatus.NoDiscrepancy => deals.Where(deal =>
                db.QualityControls.Any(control => control.DealId == deal.Id &&
                    (control.Status == QualityControlOutcome.NoDiscrepancy ||
                     (control.Status == null &&
                      db.QualityControlAnswers.Any(answer => answer.QualityControlId == control.Id) &&
                      !db.QualityControlAnswers.Any(answer => answer.QualityControlId == control.Id && answer.Answer == 1))))),
            QualityControlStatus.HasDiscrepancy => deals.Where(deal =>
                db.QualityControls.Any(control => control.DealId == deal.Id &&
                    (control.Status == QualityControlOutcome.HasDiscrepancy ||
                     (control.Status == null &&
                      db.QualityControlAnswers.Any(answer => answer.QualityControlId == control.Id && answer.Answer == 1))))),
            QualityControlStatus.NoResponse => deals.Where(deal =>
                db.QualityControls.Any(control => control.DealId == deal.Id &&
                    (control.Status == QualityControlOutcome.NoResponse ||
                     (control.Status == null &&
                      !db.QualityControlAnswers.Any(answer => answer.QualityControlId == control.Id))))),
            QualityControlStatus.InvalidNumber => deals.Where(deal =>
                db.QualityControls.Any(control => control.DealId == deal.Id &&
                    control.Status == QualityControlOutcome.InvalidNumber)),
            _ => deals
        };
        if (request.FinancialStatus.HasValue)
            deals = deals.Where(deal => deal.FinancialStatus == (DealFinancialStausEnum)request.FinancialStatus.Value);
        var recordsFiltered = await deals.CountAsync(cancellationToken);
        var descending = request.SortDescending;
        var ordered = request.SortBy switch
        {
            "serialNumber" => descending ? deals.OrderByDescending(deal => deal.SerialNumber) : deals.OrderBy(deal => deal.SerialNumber),
            "title" => descending ? deals.OrderByDescending(deal => deal.Title) : deals.OrderBy(deal => deal.Title),
            "customerName" => descending ? deals.OrderByDescending(deal => deal.CustomerName) : deals.OrderBy(deal => deal.CustomerName),
            "userCreator" => descending ? deals.OrderByDescending(deal => deal.UserCreator) : deals.OrderBy(deal => deal.UserCreator),
            "financialApproved" => descending ? deals.OrderByDescending(deal => deal.FinancialStatus) : deals.OrderBy(deal => deal.FinancialStatus),
            "status" => descending
                ? deals.OrderByDescending(deal => db.QualityControls.Where(control => control.DealId == deal.Id)
                        .Select(control => control.Status == QualityControlOutcome.InvalidNumber ? 5
                            : control.Status == QualityControlOutcome.NoResponse ? 4
                            : control.Status == QualityControlOutcome.HasDiscrepancy ? 2
                            : control.Status == QualityControlOutcome.NoDiscrepancy ? 1
                            : !db.QualityControlAnswers.Any(answer => answer.QualityControlId == control.Id) ? 4
                            : db.QualityControlAnswers.Any(answer => answer.QualityControlId == control.Id && answer.Answer == 1) ? 2 : 1)
                        .FirstOrDefault())
                : deals.OrderBy(deal => db.QualityControls.Where(control => control.DealId == deal.Id)
                        .Select(control => control.Status == QualityControlOutcome.InvalidNumber ? 5
                            : control.Status == QualityControlOutcome.NoResponse ? 4
                            : control.Status == QualityControlOutcome.HasDiscrepancy ? 2
                            : control.Status == QualityControlOutcome.NoDiscrepancy ? 1
                            : !db.QualityControlAnswers.Any(answer => answer.QualityControlId == control.Id) ? 4
                            : db.QualityControlAnswers.Any(answer => answer.QualityControlId == control.Id && answer.Answer == 1) ? 2 : 1)
                        .FirstOrDefault()),
            _ => descending ? deals.OrderByDescending(deal => deal.CreateDate) : deals.OrderBy(deal => deal.CreateDate)
        };
        var data = await ordered.ThenByDescending(deal => deal.Id)
            .Skip(Math.Max(0, request.Start)).Take(Math.Clamp(request.Length, 1, 100))
            .Select(deal => new QualityControlRow {
                RowVersion = db.QualityControls.Where(control => control.DealId == deal.Id)
                    .Select(control => control.RowVersion).FirstOrDefault(),
                DealId = deal.Id, SerialNumber = deal.SerialNumber, Title = deal.Title,
                CustomerName = deal.CustomerName, CustomerCode = deal.CustomerCode,
                CreateDate = deal.CreateDateTxt, UserCreator = deal.UserCreator,
                FinancialApproved = deal.FinancialStatus == DealFinancialStausEnum.تایید_شده,
                Status = !db.QualityControls.Any(control => control.DealId == deal.Id)
                    ? QualityControlStatus.NotReviewed
                    : db.QualityControls.Any(control => control.DealId == deal.Id &&
                        control.Status == QualityControlOutcome.InvalidNumber)
                        ? QualityControlStatus.InvalidNumber
                    : db.QualityControls.Any(control => control.DealId == deal.Id &&
                        (control.Status == QualityControlOutcome.NoResponse ||
                         (control.Status == null &&
                          !db.QualityControlAnswers.Any(answer => answer.QualityControlId == control.Id))))
                        ? QualityControlStatus.NoResponse
                    : db.QualityControls.Any(control => control.DealId == deal.Id &&
                        (control.Status == QualityControlOutcome.HasDiscrepancy ||
                         (control.Status == null &&
                          db.QualityControlAnswers.Any(answer => answer.QualityControlId == control.Id && answer.Answer == 1))))
                        ? QualityControlStatus.HasDiscrepancy
                        : QualityControlStatus.NoDiscrepancy
            }).ToListAsync(cancellationToken);
        return new QualityControlListResult(request.Draw, recordsTotal, recordsFiltered, data, financialCounts);
    }

    public async Task<IReadOnlyList<QualityControlQuestionDto>> GetQuestionsAsync(CancellationToken cancellationToken)
    {
        return await db.QualityControlQuestions.AsNoTracking().Where(question => question.IsActive)
            .OrderBy(question => question.SortOrder).ThenBy(question => question.Id)
            .Select(question => new QualityControlQuestionDto(question.Id, question.Title, question.QuestionText,
                question.ZeroAnswerText, question.OneAnswerText, question.SortOrder, null))
            .ToListAsync(cancellationToken);
    }

    public async Task<QualityControlForm?> GetFormAsync(Guid dealId, QualityControlAccess access, CancellationToken cancellationToken)
    {
        var deal = await VisibleDeals(access).Where(item => item.Id == dealId)
            .Select(item => new QualityControlDeal(item.Id, item.SerialNumber, item.Title, item.CustomerName, item.CustomerCode,
                item.CustomerId, item.UserCreator, item.CreateDateTxt, item.Description,
                item.FinancialStatus == DealFinancialStausEnum.تایید_شده,
                new QualityControlDealCounts(
                    db.PreFactors.Count(x => x.DealId == item.Id),
                    db.Factors.Count(x => x.DealId == item.Id),
                    db.Accountancies.Count(x => x.DealId == item.Id),
                    db.OrderHeads.Count(x => x.DealId == item.Id),
                    db.Reports.Count(x => x.DealId == item.Id),
                    db.SupplyFactors.Count(x => x.DealId == item.Id)))
                { CustomerTel = item.Customer != null && item.Customer.Tel != null && item.Customer.Tel != "" ? item.Customer.Tel : item.CusTel,
                  CustomerMobile = item.Customer != null && item.Customer.Mobile != null && item.Customer.Mobile != "" ? item.Customer.Mobile : item.MobileNumber })
            .FirstOrDefaultAsync(cancellationToken);
        if (deal == null)
            return null;

        var preFactorManagers = await db.PreFactors.AsNoTracking()
            .Where(preFactor => preFactor.DealId == dealId && preFactor.UserTaminTxt != null)
            .Select(preFactor => preFactor.UserTaminTxt)
            .ToListAsync(cancellationToken);
        var orderManagers = await db.OrderHeads.AsNoTracking()
            .Where(order => order.DealId == dealId && order.UserTaminTxt != null)
            .Select(order => order.UserTaminTxt)
            .ToListAsync(cancellationToken);
        deal = deal with
        {
            SupplyManagers = string.Join("، ", preFactorManagers.Concat(orderManagers)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Select(name => name!.Trim())
                .Distinct())
        };

        // Suppliers are the factories assigned through sales-to-supply invoice item allocations.
        // Do not infer them from the internal UserTaminTxt or unallocated supply invoices.
        var supplierNames = await (
            from allocation in db.SupplyFactorItemAllocations.AsNoTracking()
            join salesItem in db.FactorItems.AsNoTracking()
                on allocation.FactorItemId equals (Guid?)salesItem.Id
            join salesFactor in db.Factors.AsNoTracking()
                on salesItem.factorId equals salesFactor.Id
            join supplyItem in db.SupplyFactorItems.AsNoTracking()
                on allocation.SupplyFactorItemId equals (Guid?)supplyItem.Id
            join supplyFactor in db.SupplyFactors.AsNoTracking()
                on supplyItem.SupplyFactorId equals supplyFactor.Id
            where salesFactor.DealId == dealId
                && allocation.Weight > 0
                && allocation.WarehouseId == null
            select supplyFactor.SupplierTxt
        ).ToListAsync(cancellationToken);
        deal = deal with
        {
            Suppliers = string.Join("، ", supplierNames
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Select(name => name!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase))
        };

        // Only approved cash deposits and collected cheques contribute to these totals.
        var realizedDeposits = db.Accountancies.AsNoTracking()
            .Where(payment => payment.AccType == AcEnum.واریزی
                && payment.Status == AccStatusEnum.تایید_شده
                && (payment.PayType == PayTypeEnum.نقدی
                    || (payment.PayType == PayTypeEnum.چکی && payment.Vosoled)));

        if (deal.CustomerId.HasValue)
        {
            var payments = await realizedDeposits
                .Where(payment => payment.CustomerId == deal.CustomerId.Value)
                .GroupBy(payment => payment.CurrencyType)
                .Select(group => new { Currency = group.Key, Count = group.Count(), Amount = group.Sum(payment => payment.credit) ?? 0 })
                .ToListAsync(cancellationToken);
            deal = deal with {
                CustomerPaymentCount = payments.Sum(payment => payment.Count),
                CustomerPaymentTotals = payments.Select(payment => new QualityControlPaymentTotal(
                    payment.Currency.ToString(), payment.Amount.ToString("#,0.############################", CultureInfo.InvariantCulture))).ToList()
            };
        }

        // Associate order deposits through their pre-factors, not a possibly missing DealId on the payment.
        var dealPayments = await realizedDeposits.ForPreFactor()
            .Where(payment => db.PreFactors.Any(preFactor =>
                preFactor.DealId == dealId && preFactor.Id == payment.pfid))
            .GroupBy(payment => payment.CurrencyType)
            .Select(group => new
            {
                Currency = group.Key,
                Count = group.Count(),
                Amount = group.Sum(payment => payment.credit) ?? 0
            })
            .ToListAsync(cancellationToken);
        deal = deal with
        {
            DealPaymentCount = dealPayments.Sum(payment => payment.Count),
            DealPaymentTotals = dealPayments.Select(payment => new QualityControlPaymentTotal(
                payment.Currency.ToString(), payment.Amount.ToString("#,0.############################", CultureInfo.InvariantCulture))).ToList()
        };

        var factorTotals = await db.Factors.AsNoTracking()
            .Where(factor => factor.DealId == dealId && factor.status == FactorStatus.تایید_شده)
            .GroupBy(factor => factor.CurrencyType)
            // Recalculate the invoice total from its persisted components. Older invoices may
            // have a zero/stale SumTotalAmountTax value even though their line totals exist.
            .Select(group => new
            {
                Currency = group.Key,
                Amount = group.Sum(factor => factor.SumTotalAmount + factor.TotalTax - factor.Discount)
            })
            .ToListAsync(cancellationToken);
        deal = deal with {
            FactorTotals = factorTotals.Select(total => new QualityControlPaymentTotal(total.Currency.ToString(),
                total.Amount.ToString("#,0.############################", CultureInfo.InvariantCulture))).ToList()
        };

        var control = await db.QualityControls.AsNoTracking().Include(item => item.Answers)
            .FirstOrDefaultAsync(item => item.DealId == dealId, cancellationToken);
        var questions = (await GetQuestionsAsync(cancellationToken)).ToDictionary(question => question.Id);
        if (control != null)
        {
            // Stored answers retain the wording shown when the question was first answered, including retired questions.
            foreach (var answer in control.Answers)
                questions[answer.QuestionId] = new QualityControlQuestionDto(answer.QuestionId, answer.QuestionTitleSnapshot,
                    answer.QuestionTextSnapshot, answer.ZeroAnswerTextSnapshot, answer.OneAnswerTextSnapshot,
                    answer.SortOrderSnapshot, answer.Answer);
        }
        return new QualityControlForm(deal, control?.Description, control?.RowVersion,
            questions.Values.OrderBy(question => question.SortOrder).ThenBy(question => question.Id).ToList(),
            control != null && (control.Status == QualityControlOutcome.NoResponse || (control.Status == null && control.Answers.Count == 0)));
    }

    public async Task<QualityControlSaveResult> SaveAsync(SaveQualityControlRequest request, QualityControlAccess access, string persianNow, CancellationToken cancellationToken)
    {
        if (!await VisibleDeals(access).AnyAsync(deal => deal.Id == request.DealId, cancellationToken))
            return new(404, "سفارش یافت نشد یا به آن دسترسی ندارید.");
        if (request.Description?.Length > 4000)
            return new(400, "توضیحات تکمیلی نباید بیشتر از ۴۰۰۰ نویسه باشد.");

        var control = await db.QualityControls.Include(item => item.Answers)
            .FirstOrDefaultAsync(item => item.DealId == request.DealId, cancellationToken);
        if (control == null ? request.RowVersion != null
            : request.RowVersion == null || !control.RowVersion.SequenceEqual(request.RowVersion))
            return Conflict();

        var definitions = await db.QualityControlQuestions.AsNoTracking().Where(question => question.IsActive)
            .ToDictionaryAsync(question => question.Id, cancellationToken);
        var expectedIds = definitions.Keys.ToHashSet();
        if (control != null)
            expectedIds.UnionWith(control.Answers.Select(answer => answer.QuestionId));
        var inputs = request.Answers;
        if (expectedIds.Count == 0 || inputs == null || inputs.Count != expectedIds.Count
            || inputs.Any(answer => answer == null || answer.QuestionId == Guid.Empty || answer.Answer is not (0 or 1))
            || inputs.Select(answer => answer.QuestionId).Distinct().Count() != inputs.Count
            || inputs.Any(answer => !expectedIds.Contains(answer.QuestionId)))
            return new(400, "پاسخ به تمام سؤالات کنترل کیفیت با مقدار صفر یا یک الزامی است.");

        var now = DateTime.Now;
        if (control == null)
        {
            control = new QualityControl { Id = Guid.NewGuid(), DealId = request.DealId };
            Stamp(control, access, now, persianNow, true);
            db.QualityControls.Add(control);
        }
        else
        {
            // The original row version remains part of EF's UPDATE predicate, closing the read/write race.
            db.Entry(control).Property(item => item.RowVersion).OriginalValue = request.RowVersion!;
            Stamp(control, access, now, persianNow, false);
        }
        control.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        control.Status = inputs.Any(answer => answer.Answer == 1)
            ? QualityControlOutcome.HasDiscrepancy : QualityControlOutcome.NoDiscrepancy;
        var existingAnswers = control.Answers.ToDictionary(answer => answer.QuestionId);
        foreach (var input in inputs)
        {
            if (existingAnswers.TryGetValue(input.QuestionId, out var answer))
            {
                answer.Answer = (byte)input.Answer!.Value;
                Stamp(answer, access, now, persianNow, false);
                continue;
            }
            var question = definitions[input.QuestionId];
            answer = new QualityControlAnswer {
                Id = Guid.NewGuid(), QualityControlId = control.Id, QuestionId = question.Id,
                Answer = (byte)input.Answer!.Value,
                QuestionTitleSnapshot = question.Title, QuestionTextSnapshot = question.QuestionText,
                ZeroAnswerTextSnapshot = question.ZeroAnswerText, OneAnswerTextSnapshot = question.OneAnswerText,
                SortOrderSnapshot = question.SortOrder
            };
            Stamp(answer, access, now, persianNow, true);
            control.Answers.Add(answer);
            db.QualityControlAnswers.Add(answer);
        }

        try
        {
            // A single SaveChanges transaction commits the form, its answers and audit fields together.
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict();
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            return Conflict();
        }
        var status = control.Answers.Any(answer => answer.Answer == 1)
            ? QualityControlStatus.HasDiscrepancy : QualityControlStatus.NoDiscrepancy;
        return new(200, "کنترل کیفیت با موفقیت ثبت شد.", status, control.RowVersion);
    }

    public Task<QualityControlSaveResult> MarkNoResponseAsync(MarkQualityControlNoResponseRequest request,
        QualityControlAccess access, string persianNow, CancellationToken cancellationToken)
        => MarkTemporaryStatusAsync(request.DealId, request.RowVersion, QualityControlOutcome.NoResponse, access, persianNow, cancellationToken);

    public Task<QualityControlSaveResult> MarkInvalidNumberAsync(MarkQualityControlInvalidNumberRequest request,
        QualityControlAccess access, string persianNow, CancellationToken cancellationToken)
        => MarkTemporaryStatusAsync(request.DealId, request.RowVersion, QualityControlOutcome.InvalidNumber, access, persianNow, cancellationToken);

    private async Task<QualityControlSaveResult> MarkTemporaryStatusAsync(Guid dealId, byte[]? rowVersion,
        QualityControlOutcome outcome, QualityControlAccess access, string persianNow, CancellationToken cancellationToken)
    {
        if (!await VisibleDeals(access).AnyAsync(deal => deal.Id == dealId, cancellationToken))
            return new(404, "سفارش یافت نشد یا به آن دسترسی ندارید.");
        // Temporary outcomes are valid only before a quality-control record exists.
        // Never replace a completed review or discard saved answers.
        if (rowVersion != null)
            return Conflict();
        if (await db.QualityControls.AnyAsync(item => item.DealId == dealId, cancellationToken))
            return new(409, "برای این سفارش کنترل کیفیت ثبت شده است. برای تغییر پاسخ‌ها از ویرایش کنترل کیفیت استفاده کنید.");

        var now = DateTime.Now;
        var control = new QualityControl
        {
            Id = Guid.NewGuid(),
            DealId = dealId,
            Status = outcome
        };
        Stamp(control, access, now, persianNow, true);
        db.QualityControls.Add(control);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict();
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            return Conflict();
        }
        var invalidNumber = outcome == QualityControlOutcome.InvalidNumber;
        var status = invalidNumber ? QualityControlStatus.InvalidNumber : QualityControlStatus.NoResponse;
        var label = invalidNumber ? "شماره نامعتبر" : "عدم پاسخ";
        return new(200, $"وضعیت کنترل کیفیت به {label} تغییر کرد.", status, control.RowVersion);
    }

    public async Task<QualityControlSaveResult> DeleteAsync(DeleteQualityControlRequest request, QualityControlAccess access, CancellationToken cancellationToken)
    {
        if (!await VisibleDeals(access).AnyAsync(deal => deal.Id == request.DealId, cancellationToken))
            return new(404, "سفارش یافت نشد یا به آن دسترسی ندارید.");
        var control = await db.QualityControls.Include(item => item.Answers)
            .FirstOrDefaultAsync(item => item.DealId == request.DealId, cancellationToken);
        if (control == null)
            return new(404, "کنترل کیفیت این سفارش قبلاً حذف شده است.");
        if (request.RowVersion == null || !control.RowVersion.SequenceEqual(request.RowVersion))
            return Conflict();
        db.Entry(control).Property(item => item.RowVersion).OriginalValue = request.RowVersion;
        db.QualityControls.Remove(control);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict();
        }
        return new(200, "کنترل کیفیت با موفقیت حذف شد.", QualityControlStatus.NotReviewed);
    }


    // The new customer queue is built from eligible sales-invoice lines. Existing deal-level
    // QualityControls remain untouched and accessible through the legacy review screen.
    public async Task<QualityControlCustomerListResult> GetCustomersAsync(
        QualityControlCustomerListRequest request, QualityControlAccess access, CancellationToken cancellationToken)
    {
        var eligible =
            from item in db.FactorItems.AsNoTracking()
            join factor in db.Factors.AsNoTracking() on item.factorId equals factor.Id
            join deal in VisibleDeals(access) on factor.DealId equals (Guid?)deal.Id
            where (factor.CustomerId ?? deal.CustomerId).HasValue
                && deal.FinancialStatus == DealFinancialStausEnum.تایید_شده
                && factor.status == FactorStatus.تایید_شده && !factor.Returned
            select new { CustomerId = (factor.CustomerId ?? deal.CustomerId)!.Value, ItemId = item.Id, DealId = deal.Id, FactorId = factor.Id };

        var grouped = eligible.GroupBy(item => item.CustomerId).Select(group => new
        {
            CustomerId = group.Key,
            Orders = group.Select(item => item.DealId).Distinct().Count(),
            Invoices = group.Select(item => item.FactorId).Distinct().Count(),
            TotalItems = group.Count(),
            ReviewedItems = group.Count(item => db.QualityControlItemReviews
                .Any(review => review.FactorItemId == item.ItemId)),
            DiscrepancyItems = group.Count(item => db.QualityControlItemReviews
                .Any(review => review.FactorItemId == item.ItemId && review.Status == QualityControlOutcome.HasDiscrepancy))
        });
        var customers =
            from groupedItem in grouped
            join customer in db.Customers.AsNoTracking() on groupedItem.CustomerId equals customer.Id
            select new
            {
                groupedItem.CustomerId, groupedItem.Orders, groupedItem.Invoices,
                groupedItem.TotalItems, groupedItem.ReviewedItems, groupedItem.DiscrepancyItems,
                CustomerName = customer.CompanyName, customer.CustomerCode, customer.Mobile
            };
        var total = await customers.CountAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            customers = customers.Where(row => (row.CustomerName != null && row.CustomerName.Contains(search))
                || (row.Mobile != null && row.Mobile.Contains(search))
                || row.CustomerCode.ToString() == search);
        }

        customers = request.Status switch
        {
            0 => customers.Where(row => row.ReviewedItems < row.TotalItems),
            1 => customers.Where(row => row.ReviewedItems == row.TotalItems),
            2 => customers.Where(row => row.DiscrepancyItems > 0),
            _ => customers
        };
        var filtered = await customers.CountAsync(cancellationToken);
        var data = await customers.OrderByDescending(row => row.TotalItems - row.ReviewedItems)
            .ThenBy(row => row.CustomerName).ThenBy(row => row.CustomerId)
            .Skip(request.Start).Take(request.Length)
            .Select(row => new QualityControlCustomerRow(row.CustomerId, row.CustomerName,
                row.CustomerCode, row.Mobile, row.Orders, row.Invoices,
                row.TotalItems, row.ReviewedItems, row.DiscrepancyItems))
            .ToListAsync(cancellationToken);
        return new(total, filtered, data);
    }

    public async Task<QualityControlCustomerForm?> GetCustomerFormAsync(
        Guid customerId, QualityControlAccess access, CancellationToken cancellationToken)
    {
        var customer = await db.Customers.AsNoTracking().Where(item => item.Id == customerId)
            .Select(item => new { item.Id, item.CompanyName, item.CustomerCode, item.Mobile })
            .FirstOrDefaultAsync(cancellationToken);
        if (customer == null)
            return null;

        var raw = await (
            from item in db.FactorItems.AsNoTracking()
            join factor in db.Factors.AsNoTracking() on item.factorId equals factor.Id
            join deal in VisibleDeals(access) on factor.DealId equals (Guid?)deal.Id
            where (factor.CustomerId ?? deal.CustomerId) == customerId
                && deal.FinancialStatus == DealFinancialStausEnum.تایید_شده
                && factor.status == FactorStatus.تایید_شده && !factor.Returned
            orderby deal.SerialNumber descending, factor.SerialNumber descending, item.Row
            select new
            {
                FactorItemId = item.Id, item.ProductSmallName, item.Product, item.WeightCount, item.UnitTxt,
                DealId = deal.Id, DealSerial = deal.SerialNumber, DealTitle = deal.Title,
                FactorId = factor.Id, FactorSerial = factor.SerialNumber, FactorDate = factor.Date,
                SupplyExpert = db.PreFactors.Where(preFactor => preFactor.Id == factor.PfactorID)
                    .Select(preFactor => preFactor.UserTaminTxt).FirstOrDefault() ?? factor.UserTaminTxt
            }).ToListAsync(cancellationToken);
        if (raw.Count == 0)
            return null;

        var dealIds = raw.Select(item => item.DealId).Distinct().ToArray();
        var legacyReviewed = (await db.QualityControls.AsNoTracking()
            .Where(control => dealIds.Contains(control.DealId))
            .Select(control => control.DealId).ToListAsync(cancellationToken)).ToHashSet();
        var ids = raw.Select(item => item.FactorItemId).ToArray();
        var reviews = await db.QualityControlItemReviews.AsNoTracking()
            .Include(review => review.Answers)
            .Where(review => ids.Contains(review.FactorItemId))
            .ToDictionaryAsync(review => review.FactorItemId, cancellationToken);

        // Item allocation is the source of supplier provenance, not UserTaminTxt.
        var suppliers = await (
            from allocation in db.SupplyFactorItemAllocations.AsNoTracking()
            join supplyItem in db.SupplyFactorItems.AsNoTracking()
                on allocation.SupplyFactorItemId equals (Guid?)supplyItem.Id
            join supplyFactor in db.SupplyFactors.AsNoTracking()
                on supplyItem.SupplyFactorId equals supplyFactor.Id
            where allocation.FactorItemId.HasValue
                && ids.Contains(allocation.FactorItemId.Value) && allocation.Weight > 0
            select new { allocation.FactorItemId, supplyFactor.SupplierId, supplyFactor.SupplierTxt }
        ).ToListAsync(cancellationToken);
        var supplierMap = suppliers.GroupBy(item => item.FactorItemId!.Value)
            .ToDictionary(group => group.Key, group => string.Join("، ",
                group.GroupBy(item => item.SupplierId)
                    .Select(names => names.Select(item => item.SupplierTxt)
                        .FirstOrDefault(name => !string.IsNullOrWhiteSpace(name))?.Trim())
                    .Where(name => !string.IsNullOrWhiteSpace(name))));

        var questions = await GetQuestionsAsync(cancellationToken);
        var orders = raw.GroupBy(item => new { item.DealId, item.DealSerial, item.DealTitle })
            .Select(order => new QualityControlCustomerOrder(order.Key.DealId,
                order.Key.DealSerial, order.Key.DealTitle,
                order.GroupBy(item => new { item.FactorId, item.FactorSerial, item.FactorDate })
                    .Select(invoice => new QualityControlCustomerInvoice(invoice.Key.FactorId,
                        invoice.Key.FactorSerial, invoice.Key.FactorDate,
                        invoice.Select(item =>
                        {
                            reviews.TryGetValue(item.FactorItemId, out var review);
                            supplierMap.TryGetValue(item.FactorItemId, out var supplierNames);
                            var savedQuestions = review?.Answers.Select(answer =>
                                new QualityControlQuestionDto(answer.QuestionId, answer.QuestionTitleSnapshot,
                                    answer.QuestionTextSnapshot, answer.ZeroAnswerTextSnapshot,
                                    answer.OneAnswerTextSnapshot, answer.SortOrderSnapshot, answer.Answer))
                                .OrderBy(question => question.SortOrder).ToList()
                                ?? new List<QualityControlQuestionDto>();
                            var status = review == null ? QualityControlStatus.NotReviewed
                                : review.Status == QualityControlOutcome.HasDiscrepancy
                                    ? QualityControlStatus.HasDiscrepancy : QualityControlStatus.NoDiscrepancy;
                            return new QualityControlInvoiceItem(item.FactorItemId, item.ProductSmallName,
                                item.Product, item.WeightCount, item.UnitTxt, supplierNames, item.SupplyExpert,
                                status, review?.Description, review?.RowVersion, savedQuestions);
                        }).ToList())).ToList()) { HasLegacyReview = legacyReviewed.Contains(order.Key.DealId) }).ToList();
        return new(customer.Id, customer.CompanyName, customer.CustomerCode, customer.Mobile, orders, questions);
    }

    public async Task<SaveQualityControlItemsResult> SaveItemsAsync(
        SaveQualityControlItemsRequest request, QualityControlAccess access, string persianNow,
        CancellationToken cancellationToken)
    {
        var inputs = request.Items;
        if (request.CustomerId == Guid.Empty || inputs == null || inputs.Count is < 1 or > 2000
            || inputs.Any(item => item == null || item.FactorItemId == Guid.Empty)
            || inputs.Select(item => item.FactorItemId).Distinct().Count() != inputs.Count)
            return new(400, "فهرست اقلام معتبر نیست.");
        if (!request.SaveAll && inputs.Count != 1)
            return new(400, "برای ثبت یک قلم فقط یک آیتم ارسال کنید.");

        var allowedIds = await (
            from item in db.FactorItems.AsNoTracking()
            join factor in db.Factors.AsNoTracking() on item.factorId equals factor.Id
            join deal in VisibleDeals(access) on factor.DealId equals (Guid?)deal.Id
            where (factor.CustomerId ?? deal.CustomerId) == request.CustomerId
                && deal.FinancialStatus == DealFinancialStausEnum.تایید_شده
                && factor.status == FactorStatus.تایید_شده && !factor.Returned
            select item.Id
        ).ToListAsync(cancellationToken);
        var allowed = allowedIds.ToHashSet();
        var selected = inputs.Select(item => item.FactorItemId).ToHashSet();
        if (allowed.Count == 0)
            return new(404, "مشتری یا فاکتورهای قابل بررسی یافت نشد.");
        if (!selected.IsSubsetOf(allowed) || (request.SaveAll && !selected.SetEquals(allowed)))
            return new(400, "اقلام ارسال‌شده با فاکتورهای تحویل‌شده این مشتری مطابقت ندارد. فرم را مجدداً باز کنید.");

        var definitions = await db.QualityControlQuestions.AsNoTracking()
            .Where(question => question.IsActive).ToDictionaryAsync(question => question.Id, cancellationToken);
        foreach (var input in inputs)
        {
            if (input.Status is not (QualityControlStatus.NoDiscrepancy or QualityControlStatus.HasDiscrepancy)
                || input.Description?.Length > 4000)
                return new(400, "وضعیت یا طول شرح مغایرت معتبر نیست.");
            if (input.Status == QualityControlStatus.NoDiscrepancy)
            {
                if (input.Answers?.Count > 0)
                    return new(400, "قلم بدون مغایرت نباید پاسخ مغایرت داشته باشد.");
                continue;
            }

            var answers = input.Answers;
            if (definitions.Count == 0 || answers == null || answers.Count != definitions.Count
                || answers.Any(answer => answer == null || answer.QuestionId == Guid.Empty || answer.Answer is not (0 or 1))
                || answers.Select(answer => answer.QuestionId).Distinct().Count() != answers.Count
                || answers.Any(answer => !definitions.ContainsKey(answer.QuestionId))
                || !answers.Any(answer => answer.Answer == 1))
                return new(400, "برای قلم دارای مغایرت، پاسخ تمام سؤال‌ها و حداقل یک پاسخ مغایرت الزامی است.");
            // Description is intentionally optional even for discrepant items.
        }

        var existing = await db.QualityControlItemReviews.Include(review => review.Answers)
            .Where(review => selected.Contains(review.FactorItemId))
            .ToDictionaryAsync(review => review.FactorItemId, cancellationToken);
        foreach (var input in inputs)
        {
            if (existing.TryGetValue(input.FactorItemId, out var review))
            {
                if (input.RowVersion == null || !review.RowVersion.SequenceEqual(input.RowVersion))
                    return new(409, "یکی از اقلام توسط کاربر دیگری تغییر کرده است؛ اطلاعات را دوباره بارگذاری کنید.");
            }
            else if (input.RowVersion != null)
                return new(409, "وضعیت قلم تغییر کرده است؛ فرم را دوباره باز کنید.");
        }

        var now = DateTime.Now;
        var updated = new List<QualityControlItemReview>(inputs.Count);
        foreach (var input in inputs)
        {
            if (!existing.TryGetValue(input.FactorItemId, out var review))
            {
                review = new QualityControlItemReview
                {
                    Id = Guid.NewGuid(), FactorItemId = input.FactorItemId
                };
                Stamp(review, access, now, persianNow, true);
                db.QualityControlItemReviews.Add(review);
            }
            else
            {
                db.Entry(review).Property(item => item.RowVersion).OriginalValue = input.RowVersion!;
                Stamp(review, access, now, persianNow, false);
            }
            review.Status = (QualityControlOutcome)input.Status;
            review.Description = string.IsNullOrWhiteSpace(input.Description) ? null : input.Description.Trim();

            var requestedAnswers = input.Status == QualityControlStatus.HasDiscrepancy
                ? input.Answers! : new List<QualityControlAnswerInput>();
            var keyed = review.Answers.ToDictionary(answer => answer.QuestionId);
            var answerIds = requestedAnswers.Select(answer => answer.QuestionId).ToHashSet();
            foreach (var stale in review.Answers.Where(answer => !answerIds.Contains(answer.QuestionId)).ToList())
                db.QualityControlItemAnswers.Remove(stale);
            foreach (var requested in requestedAnswers)
            {
                if (keyed.TryGetValue(requested.QuestionId, out var answer))
                {
                    answer.Answer = (byte)requested.Answer!.Value;
                    Stamp(answer, access, now, persianNow, false);
                }
                else
                {
                    var definition = definitions[requested.QuestionId];
                    answer = new QualityControlItemAnswer
                    {
                        Id = Guid.NewGuid(), QualityControlItemReviewId = review.Id,
                        QuestionId = definition.Id, Answer = (byte)requested.Answer!.Value,
                        QuestionTitleSnapshot = definition.Title, QuestionTextSnapshot = definition.QuestionText,
                        ZeroAnswerTextSnapshot = definition.ZeroAnswerText, OneAnswerTextSnapshot = definition.OneAnswerText,
                        SortOrderSnapshot = definition.SortOrder
                    };
                    Stamp(answer, access, now, persianNow, true);
                    db.QualityControlItemAnswers.Add(answer);
                }
            }
            updated.Add(review);
        }

        try
        {
            // EF Core saves all items and their answers in a single transaction.
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return new(409, "کنترل کیفیت تغییر کرده است؛ لطفاً فرم را مجدداً باز کنید.");
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            return new(409, "این قلم قبلاً ثبت شده است؛ لطفاً فرم را مجدداً باز کنید.");
        }
        return new(200, request.SaveAll ? "کنترل کیفیت تمام اقلام ثبت شد." : "کنترل کیفیت قلم ثبت شد.",
            updated.Select(review => new QualityControlItemVersion(review.FactorItemId, review.RowVersion)).ToList());
    }

    private IQueryable<Deal> VisibleDeals(QualityControlAccess access) => db.Deals.AsNoTracking()
        .Where(deal => access.ReadAll || deal.UserCreatorID == access.UserId);

    private static QualityControlSaveResult Conflict() => new(409,
        "کنترل کیفیت این سفارش توسط کاربر دیگری تغییر کرده است. فرم را دوباره باز کنید و تغییرات را بررسی کنید.");

    private static void Stamp(BaseEntity entity, QualityControlAccess access, DateTime now, string persianNow, bool create)
    {
        if (create)
        {
            entity.UserCreatorID = access.UserId;
            entity.UserCreator = access.UserName;
            entity.CreateDate = now;
            entity.CreateDateTxt = persianNow;
        }
        entity.UserModifyID = access.UserId;
        entity.UserModify = access.UserName;
        entity.ModifyDate = now;
        entity.ModifyDateTxt = persianNow;
    }
}
