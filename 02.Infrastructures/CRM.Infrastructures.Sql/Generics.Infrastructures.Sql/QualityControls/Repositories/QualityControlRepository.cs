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
using CRM.Core.Domain.Factors.Enum;

namespace CRM.Infrastructures.Sql.QualityControls.Repositories;

public sealed class QualityControlRepository(CRMContextDb db) : IQualityControlRepository
{
    private async Task<IReadOnlyList<QualityControlQuestionDto>> GetQuestionsAsync(CancellationToken cancellationToken)
    {
        return await db.QualityControlQuestions.AsNoTracking().Where(question => question.IsActive)
            .OrderBy(question => question.SortOrder).ThenBy(question => question.Id)
            .Select(question => new QualityControlQuestionDto(question.Id, question.Title, question.QuestionText,
                question.ZeroAnswerText, question.OneAnswerText, question.SortOrder, null))
            .ToListAsync(cancellationToken);
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
                && !item.IsDeleted && !factor.IsDeleted && !deal.IsDeleted
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
            where !customer.IsDeleted
            select new
            {
                groupedItem.CustomerId, groupedItem.Orders, groupedItem.Invoices,
                groupedItem.TotalItems, groupedItem.ReviewedItems, groupedItem.DiscrepancyItems,
                CustomerName = customer.CompanyName != null && customer.CompanyName != ""
                    ? customer.CompanyName : customer.AgentName, customer.CustomerCode, customer.Mobile
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
        var customer = await db.Customers.AsNoTracking().Where(item => item.Id == customerId && !item.IsDeleted)
            .Select(item => new { item.Id, item.CompanyName, item.AgentName, item.CustomerCode, item.Mobile })
            .FirstOrDefaultAsync(cancellationToken);
        if (customer == null)
            return null;

        var raw = await (
            from item in db.FactorItems.AsNoTracking()
            join factor in db.Factors.AsNoTracking() on item.factorId equals factor.Id
            join deal in VisibleDeals(access) on factor.DealId equals (Guid?)deal.Id
            where (factor.CustomerId ?? deal.CustomerId) == customerId
                && !item.IsDeleted && !factor.IsDeleted && !deal.IsDeleted
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
        return new(customer.Id, !string.IsNullOrWhiteSpace(customer.CompanyName)
            ? customer.CompanyName : customer.AgentName, customer.CustomerCode, customer.Mobile, orders, questions);
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
                && !item.IsDeleted && !factor.IsDeleted && !deal.IsDeleted
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
