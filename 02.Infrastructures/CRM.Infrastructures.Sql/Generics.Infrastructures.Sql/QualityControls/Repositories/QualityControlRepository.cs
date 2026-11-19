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
    private const int NoResponseCooldownSeconds = 60;

    private static int CooldownRemaining(DateTime? lastAttempt, DateTime now) =>
        lastAttempt.HasValue
            ? Math.Max(0, (int)Math.Ceiling((lastAttempt.Value.AddSeconds(NoResponseCooldownSeconds) - now).TotalSeconds))
            : 0;
    private async Task<IReadOnlyList<QualityControlQuestionDto>> GetQuestionsAsync(CancellationToken cancellationToken)
    {
        return await db.QualityControlQuestions.AsNoTracking().Where(question => question.IsActive)
            .OrderBy(question => question.SortOrder).ThenBy(question => question.Id)
            .Select(question => new QualityControlQuestionDto(question.Id, question.Title, question.QuestionText,
                question.ZeroAnswerText, question.OneAnswerText, question.SortOrder, null))
            .ToListAsync(cancellationToken);
    }

    private static QualityControlCustomerContactStatus CurrentContactStatus(
        QualityControlCustomerContact? contact, string? mobile, DateTime today, int pendingItems)
    {
        if (contact == null || pendingItems <= 0) return QualityControlCustomerContactStatus.None;
        return contact.Status switch
        {
            QualityControlCustomerContactStatus.NoResponse
                when contact.LastNoResponseAt?.Date >= today => QualityControlCustomerContactStatus.NoResponse,
            QualityControlCustomerContactStatus.InvalidNumber
                when string.Equals((contact.InvalidMobileAtMark ?? "").Trim(), (mobile ?? "").Trim(),
                    StringComparison.Ordinal) => QualityControlCustomerContactStatus.InvalidNumber,
            _ => QualityControlCustomerContactStatus.None
        };
    }

    // Customer queue counts only completed invoice-item reviews; historical deal-level records stay untouched.
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
                .Any(review => review.FactorItemId == item.ItemId && review.Status != QualityControlOutcome.NotReviewed)),
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
                    ? customer.CompanyName : customer.AgentName, customer.CustomerCode, customer.Mobile,
                // Only projected into SQL when the "latest" sort is selected.
                // Include QC item edits and changes to the customer's contact status.
                LastReviewModified = (
                    from invoiceItem in eligible
                    join review in db.QualityControlItemReviews.AsNoTracking()
                        on invoiceItem.ItemId equals review.FactorItemId
                    where invoiceItem.CustomerId == groupedItem.CustomerId
                    select review.ModifyDate
                ).Max(),
                LastContactModified = db.QualityControlCustomerContacts.AsNoTracking()
                    .Where(contact => contact.CustomerId == groupedItem.CustomerId)
                    .Max(contact => contact.ModifyDate)
            };
        // Summary is global for the eligible QC queue: it is not affected by search,
        // selected status or pagination. Read one grouped row per customer, not every item.
        var summaryRows = await customers.Select(row => new
        {
            row.CustomerId, row.Mobile, row.TotalItems, row.ReviewedItems, row.DiscrepancyItems
        }).ToListAsync(cancellationToken);
        var total = summaryRows.Count;
        var customerIds = summaryRows.Select(row => row.CustomerId).ToArray();
        var contacts = await db.QualityControlCustomerContacts.AsNoTracking()
            .Where(contact => customerIds.Contains(contact.CustomerId)
                && (contact.Status != QualityControlCustomerContactStatus.None
                    || contact.NoResponseAttempts > 0))
            .ToDictionaryAsync(contact => contact.CustomerId, cancellationToken);
        // No background job is needed: overdue "no answer" expires at calendar midnight,
        // and changed mobile numbers invalidate an existing "invalid number" flag.
        var today = DateTime.Today;
        var contactStatuses = summaryRows.ToDictionary(row => row.CustomerId, row =>
        {
            contacts.TryGetValue(row.CustomerId, out var contact);
            return CurrentContactStatus(contact, row.Mobile, today, row.TotalItems - row.ReviewedItems);
        });

        var invalidCustomers = 0;
        var noResponseCustomers = 0;
        var pendingCustomers = 0;
        var pendingItems = 0;
        var reviewedItems = 0;
        var discrepancyItems = 0;
        var totalItems = 0;
        var completedCustomers = 0;
        var dueFollowUpsToday = 0;
        var totalNoResponseAttempts = 0;
        foreach (var row in summaryRows)
        {
            totalItems += row.TotalItems;
            reviewedItems += row.ReviewedItems;
            discrepancyItems += row.DiscrepancyItems;
            if (row.ReviewedItems == row.TotalItems)
                completedCustomers++;

            // Previously unanswered customers become due the next calendar day;
            // overdue callbacks remain due until handled or their QC is completed.
            if (contacts.TryGetValue(row.CustomerId, out var contact))
            {
                totalNoResponseAttempts += contact.NoResponseAttempts;
                if (row.ReviewedItems < row.TotalItems
                    && contact.Status == QualityControlCustomerContactStatus.NoResponse
                    && contact.LastNoResponseAt.HasValue
                    && contact.LastNoResponseAt.Value.Date < today)
                    dueFollowUpsToday++;
            }

            var contactStatus = contactStatuses.GetValueOrDefault(row.CustomerId);
            if (contactStatus == QualityControlCustomerContactStatus.InvalidNumber)
                invalidCustomers++;
            else if (contactStatus == QualityControlCustomerContactStatus.NoResponse)
                noResponseCustomers++;
            else if (row.ReviewedItems < row.TotalItems)
            {
                pendingCustomers++;
                pendingItems += row.TotalItems - row.ReviewedItems;
            }
        }
        var summary = new QualityControlCustomerSummary(
            invalidCustomers, noResponseCustomers, pendingCustomers, pendingItems,
            reviewedItems, discrepancyItems, reviewedItems - discrepancyItems,
            totalItems, completedCustomers, dueFollowUpsToday, totalNoResponseAttempts);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            customers = customers.Where(row => (row.CustomerName != null && row.CustomerName.Contains(search))
                || (row.Mobile != null && row.Mobile.Contains(search))
                || row.CustomerCode.ToString() == search);
        }

        customers = request.Status switch
        {
            0 => customers.Where(row => row.ReviewedItems < row.TotalItems
                && !db.QualityControlCustomerContacts.Any(contact => contact.CustomerId == row.CustomerId
                    && ((contact.Status == QualityControlCustomerContactStatus.NoResponse
                            && contact.LastNoResponseAt >= today)
                        || (contact.Status == QualityControlCustomerContactStatus.InvalidNumber
                            && (contact.InvalidMobileAtMark ?? "").Trim() == (row.Mobile ?? "").Trim())))),
            1 => customers.Where(row => row.ReviewedItems == row.TotalItems),
            2 => customers.Where(row => row.DiscrepancyItems > 0),
            3 => customers.Where(row => row.ReviewedItems < row.TotalItems
                && db.QualityControlCustomerContacts.Any(contact =>
                    contact.CustomerId == row.CustomerId
                    && contact.Status == QualityControlCustomerContactStatus.NoResponse
                    && contact.LastNoResponseAt >= today)),
            4 => customers.Where(row => row.ReviewedItems < row.TotalItems
                && db.QualityControlCustomerContacts.Any(contact =>
                    contact.CustomerId == row.CustomerId
                    && contact.Status == QualityControlCustomerContactStatus.InvalidNumber
                    && (contact.InvalidMobileAtMark ?? "").Trim() == (row.Mobile ?? "").Trim())),
            5 => customers.Where(row => row.ReviewedItems > 0),
            _ => customers
        };
        var filtered = await customers.CountAsync(cancellationToken);
        // Respect only the user-selected sort. A recently reviewed customer must
        // never receive special priority or jump to the first row.
        var sorted = request.Sort switch
        {
            "discrepancy" => customers.OrderByDescending(row => row.DiscrepancyItems)
                .ThenByDescending(row => row.TotalItems - row.ReviewedItems),
            "reviewed" => customers.OrderByDescending(row => row.ReviewedItems)
                .ThenByDescending(row => row.DiscrepancyItems),
            "latest" => customers.OrderByDescending(row =>
                (row.LastReviewModified ?? DateTime.MinValue) >= (row.LastContactModified ?? DateTime.MinValue)
                    ? row.LastReviewModified : row.LastContactModified),
            "invoices" => customers.OrderByDescending(row => row.Invoices)
                .ThenByDescending(row => row.TotalItems - row.ReviewedItems),
            "orders" => customers.OrderByDescending(row => row.Orders)
                .ThenByDescending(row => row.TotalItems - row.ReviewedItems),
            "name" => customers.OrderBy(row => row.CustomerName),
            "code" => customers.OrderBy(row => row.CustomerCode),
            _ => customers.OrderByDescending(row => row.TotalItems - row.ReviewedItems)
                .ThenByDescending(row => row.DiscrepancyItems)
        };
        var page = await sorted.ThenBy(row => row.CustomerId)
            .Skip(request.Start).Take(request.Length)
            .ToListAsync(cancellationToken);

        // Compute retry time for the current actor only, using active history entries.
        // Fetch for the visible page so large customer sets do not inflate this query.
        var now = DateTime.Now;
        var pageCustomerIds = page.Select(row => row.CustomerId).ToArray();
        var recentAttempts = await db.QualityControlContactHistories.AsNoTracking()
            .Where(entry => pageCustomerIds.Contains(entry.CustomerId)
                && entry.UserCreatorID == access.UserId
                && entry.Status == QualityControlCustomerContactStatus.NoResponse
                && entry.AttemptCount > 0 && !entry.IsLegacy
                && entry.CreateDate > now.AddSeconds(-NoResponseCooldownSeconds))
            .GroupBy(entry => entry.CustomerId)
            .Select(group => new { CustomerId = group.Key, LastAttempt = group.Max(entry => entry.CreateDate) })
            .ToDictionaryAsync(entry => entry.CustomerId, entry => entry.LastAttempt, cancellationToken);

        // Count only active contact-history records for the visible customers.
        // Soft-deleted records are excluded by the history entity's query filter.
        var historyCounts = await db.QualityControlContactHistories.AsNoTracking()
            .Where(entry => pageCustomerIds.Contains(entry.CustomerId))
            .GroupBy(entry => entry.CustomerId)
            .Select(group => new { CustomerId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(entry => entry.CustomerId, entry => entry.Count, cancellationToken);

        var data = page.Select(row => new QualityControlCustomerRow(row.CustomerId, row.CustomerName,
            row.CustomerCode, row.Mobile, row.Orders, row.Invoices,
            row.TotalItems, row.ReviewedItems, row.DiscrepancyItems,
            contactStatuses.GetValueOrDefault(row.CustomerId),
            contacts.TryGetValue(row.CustomerId, out var contact) ? contact.NoResponseAttempts : 0,
            recentAttempts.TryGetValue(row.CustomerId, out var lastAttempt)
                ? CooldownRemaining(lastAttempt, now) : 0,
            historyCounts.GetValueOrDefault(row.CustomerId))).ToList();
        return new(total, filtered, data, summary);
    }

    public async Task<SetQualityControlCustomerContactResult> SetCustomerContactStatusAsync(
        SetQualityControlCustomerContactRequest request, QualityControlAccess access, string persianNow,
        CancellationToken cancellationToken)
    {
        if (request.CustomerId == Guid.Empty ||
            request.Status is not (QualityControlCustomerContactStatus.NoResponse
                or QualityControlCustomerContactStatus.InvalidNumber))
            return new(400, "مشتری یا وضعیت تماس نامعتبر است.", QualityControlCustomerContactStatus.None);

        // Use the same eligibility/role scope as the customer QC queue.
        var eligibleItems =
            from item in db.FactorItems.AsNoTracking()
            join factor in db.Factors.AsNoTracking() on item.factorId equals factor.Id
            join deal in VisibleDeals(access) on factor.DealId equals (Guid?)deal.Id
            join customer in db.Customers.AsNoTracking()
                on (factor.CustomerId ?? deal.CustomerId) equals (Guid?)customer.Id
            where customer.Id == request.CustomerId
                && !customer.IsDeleted && !item.IsDeleted && !factor.IsDeleted && !deal.IsDeleted
                && deal.FinancialStatus == DealFinancialStausEnum.تایید_شده
                && factor.status == FactorStatus.تایید_شده && !factor.Returned
            select item.Id;
        if (!await eligibleItems.AnyAsync(cancellationToken))
            return new(404, "مشتری یا فاکتور قابل بررسی یافت نشد.", QualityControlCustomerContactStatus.None);

        // Partial QC is allowed: only completely reviewed customers cannot be put on hold.
        var hasPendingItems = await eligibleItems.AnyAsync(itemId =>
            !db.QualityControlItemReviews.Any(review => review.FactorItemId == itemId
                && review.Status != QualityControlOutcome.NotReviewed), cancellationToken);
        if (!hasPendingItems)
            return new(409, "همه اقلام این مشتری بررسی شده‌اند و نیازی به پیگیری تماس نیست.",
                QualityControlCustomerContactStatus.None);

        var contact = await db.QualityControlCustomerContacts.IgnoreQueryFilters()
            .FirstOrDefaultAsync(item => item.CustomerId == request.CustomerId, cancellationToken);
        var mobile = await db.Customers.AsNoTracking().Where(customer => customer.Id == request.CustomerId)
            .Select(customer => customer.Mobile).FirstOrDefaultAsync(cancellationToken);
        var now = DateTime.Now;
        if (request.Status == QualityControlCustomerContactStatus.NoResponse)
        {
            // Limit only repeated attempts by the same staff member for this customer.
            // Deleted history does not count, so correcting an accidental entry resets the guard.
            var lastAttempt = await db.QualityControlContactHistories.AsNoTracking()
                .Where(entry => entry.CustomerId == request.CustomerId
                    && entry.UserCreatorID == access.UserId
                    && entry.Status == QualityControlCustomerContactStatus.NoResponse
                    && entry.AttemptCount > 0 && !entry.IsLegacy
                    && entry.CreateDate > now.AddSeconds(-NoResponseCooldownSeconds))
                .Select(entry => (DateTime?)entry.CreateDate)
                .MaxAsync(cancellationToken);
            var remaining = lastAttempt.HasValue ? CooldownRemaining(lastAttempt.Value, now) : 0;
            if (remaining > 0)
                return new(429, $"عدم پاسخ قبلاً ثبت شده است؛ {remaining} ثانیه دیگر دوباره تلاش کنید.",
                    QualityControlCustomerContactStatus.NoResponse, remaining);
        }
        var currentStatus = contact?.IsDeleted == false
            ? CurrentContactStatus(contact, mobile, now.Date, 1)
            : QualityControlCustomerContactStatus.None;
        // Every no-response click is a separate contact attempt, even on the same day.
        // Only the invalid-number flag can be toggled off by clicking it again.
        var nextStatus = request.Status == QualityControlCustomerContactStatus.NoResponse
            ? QualityControlCustomerContactStatus.NoResponse
            : currentStatus == request.Status ? QualityControlCustomerContactStatus.None : request.Status;
        if (contact == null)
        {
            contact = new QualityControlCustomerContact
            {
                Id = Guid.NewGuid(),
                CustomerId = request.CustomerId,
                Status = nextStatus
            };
            Stamp(contact, access, now, persianNow, true);
            db.QualityControlCustomerContacts.Add(contact);
        }
        else
        {
            contact.IsDeleted = false;
            contact.Status = nextStatus;
            Stamp(contact, access, now, persianNow, false);
        }

        if (nextStatus == QualityControlCustomerContactStatus.NoResponse)
        {
            contact.NoResponseAttempts++;
            contact.LastNoResponseAt = now;
        }
        if (nextStatus == QualityControlCustomerContactStatus.InvalidNumber)
            contact.InvalidMobileAtMark = (mobile ?? "").Trim();
        else
            contact.InvalidMobileAtMark = null;

        var history = new QualityControlContactHistory
        {
            Id = Guid.NewGuid(),
            CustomerId = request.CustomerId,
            Status = nextStatus,
            AttemptCount = nextStatus == QualityControlCustomerContactStatus.NoResponse ? 1 : 0,
            MobileAtAttempt = (mobile ?? "").Trim(),
            Description = nextStatus == QualityControlCustomerContactStatus.None
                ? "لغو وضعیت تماس ثبت‌شده" : null
        };
        Stamp(history, access, now, persianNow, true);
        db.QualityControlContactHistories.Add(history);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return new(409, "وضعیت تماس توسط کاربر دیگری تغییر کرده است. فهرست را تازه کنید.", nextStatus);
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            return new(409, "وضعیت تماس تغییر کرده است. فهرست را تازه کنید.", nextStatus);
        }

        return new(200, nextStatus switch
        {
            QualityControlCustomerContactStatus.NoResponse => "عدم پاسخ ثبت شد؛ مشتری فردا دوباره در صف بررسی قرار می‌گیرد.",
            QualityControlCustomerContactStatus.InvalidNumber => "شماره نامعتبر مشتری ثبت شد.",
            _ => "وضعیت تماس مشتری پاک شد."
        }, nextStatus);
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
                item.PfItemId, item.SupplierName,
                DealId = deal.Id, DealSerial = deal.SerialNumber, DealTitle = deal.Title,
                FactorId = factor.Id, FactorSerial = factor.SerialNumber, FactorDate = factor.Date,
                PreFactorSupplyExpert = db.PreFactors.Where(preFactor => preFactor.Id == factor.PfactorID)
                    .Select(preFactor => preFactor.UserTaminTxt).FirstOrDefault(),
                FactorSupplyExpert = factor.UserTaminTxt
            }).ToListAsync(cancellationToken);
        if (raw.Count == 0)
            return null;

        var ids = raw.Select(item => item.FactorItemId).ToArray();
        var reviews = await db.QualityControlItemReviews.AsNoTracking()
            .Include(review => review.Answers)
            .Where(review => ids.Contains(review.FactorItemId))
            .ToDictionaryAsync(review => review.FactorItemId, cancellationToken);

        // Match the actual sourcing of each invoice item. Warehouse-linked allocations may
        // have their source item only on WarehouseProduct, so resolve both allocation paths.
        var allocations = await (
            from allocation in db.SupplyFactorItemAllocations.AsNoTracking()
            where allocation.FactorItemId.HasValue
                && ids.Contains(allocation.FactorItemId.Value) && allocation.Weight > 0
            from sourceProduct in db.WarehouseProducts.IgnoreQueryFilters().AsNoTracking()
                .Where(product => product.Id == allocation.SourceWarehouseProductId).DefaultIfEmpty()
            from supplyItem in db.SupplyFactorItems.IgnoreQueryFilters().AsNoTracking()
                .Where(item => (Guid?)item.Id ==
                    (allocation.SupplyFactorItemId ?? sourceProduct.SupplyFactorItemId)).DefaultIfEmpty()
            from supplyFactor in db.SupplyFactors.IgnoreQueryFilters().AsNoTracking()
                .Where(factor => supplyItem != null && factor.Id == supplyItem.SupplyFactorId).DefaultIfEmpty()
            where supplyFactor != null && !string.IsNullOrEmpty(supplyFactor.SupplierTxt)
            select new { FactorItemId = allocation.FactorItemId!.Value, supplyFactor.SupplierTxt }
        ).ToListAsync(cancellationToken);
        var allocatedSuppliers = allocations.GroupBy(item => item.FactorItemId)
            .ToDictionary(group => group.Key, group => string.Join("، ",
                group.Select(item => item.SupplierTxt.Trim())
                    .Where(name => name.Length > 0)
                    .Distinct(StringComparer.OrdinalIgnoreCase)));

        // If an item has no invoice allocation, use only links for that exact item:
        // its saved supplier, its originating production order, or its pre-invoice supply.
        // Never take a supplier from another item merely because the deal is the same.
        var pfItemIds = raw.Where(item => item.PfItemId.HasValue)
            .Select(item => item.PfItemId!.Value).Distinct().ToArray();
        var productionLinks = pfItemIds.Length == 0
            ? new List<(Guid PfItemId, string? Expert, string? Supplier)>()
            : (await (
                from order in db.Orders.AsNoTracking()
                join head in db.OrderHeads.AsNoTracking() on order.OrderHeadId equals (Guid?)head.Id
                where order.PreFactorItemId.HasValue && pfItemIds.Contains(order.PreFactorItemId.Value)
                    && !order.IsDeleted && !head.IsDeleted
                select new { PfItemId = order.PreFactorItemId.Value, Expert = head.UserTaminTxt,
                    Supplier = head.SupplierTxt }
            ).ToListAsync(cancellationToken))
            .Select(item => (item.PfItemId, item.Expert, item.Supplier)).ToList();
        var productionByPfItem = productionLinks.GroupBy(item => item.PfItemId)
            .ToDictionary(group => group.Key, group => group.ToList());

        var pfSupplyLinks = pfItemIds.Length == 0
            ? new List<(Guid PfItemId, string? Supplier)>()
            : (await db.Supplies.AsNoTracking()
                .Where(item => item.PfItemId.HasValue && pfItemIds.Contains(item.PfItemId.Value)
                    && !item.IsDeleted && item.SupplierTxt != null)
                .Select(item => new { PfItemId = item.PfItemId!.Value, Supplier = item.SupplierTxt })
                .ToListAsync(cancellationToken))
                .Select(item => (item.PfItemId, item.Supplier)).ToList();
        var pfSuppliersByItem = pfSupplyLinks.GroupBy(item => item.PfItemId)
            .ToDictionary(group => group.Key, group => group.ToList());

        static string? JoinNames(IEnumerable<string?> names)
        {
            var distinct = names.Where(name => !string.IsNullOrWhiteSpace(name))
                .Select(name => name!.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            return distinct.Length == 0 ? null : string.Join("، ", distinct);
        }

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
                            allocatedSuppliers.TryGetValue(item.FactorItemId, out var supplierNames);
                            productionByPfItem.TryGetValue(item.PfItemId ?? Guid.Empty, out var production);
                            pfSuppliersByItem.TryGetValue(item.PfItemId ?? Guid.Empty, out var pfSupply);
                            supplierNames = !string.IsNullOrWhiteSpace(supplierNames) ? supplierNames
                                : JoinNames(new[] { item.SupplierName }) ??
                                  JoinNames(production?.Select(link => link.Supplier) ?? Enumerable.Empty<string?>()) ??
                                  JoinNames(pfSupply?.Select(link => link.Supplier) ?? Enumerable.Empty<string?>());
                            var supplyExpert = JoinNames(production?.Select(link => link.Expert)
                                ?? Enumerable.Empty<string?>()) ??
                                JoinNames(new[] { item.PreFactorSupplyExpert, item.FactorSupplyExpert });
                            var savedQuestions = review?.Answers.Select(answer =>
                                new QualityControlQuestionDto(answer.QuestionId, answer.QuestionTitleSnapshot,
                                    answer.QuestionTextSnapshot, answer.ZeroAnswerTextSnapshot,
                                    answer.OneAnswerTextSnapshot, answer.SortOrderSnapshot, answer.Answer))
                                .OrderBy(question => question.SortOrder).ToList()
                                ?? new List<QualityControlQuestionDto>();
                            var status = review == null ? QualityControlStatus.NotReviewed
                                : (QualityControlStatus)review.Status;
                            return new QualityControlInvoiceItem(item.FactorItemId, item.ProductSmallName,
                                item.Product, item.WeightCount, item.UnitTxt, supplierNames, supplyExpert,
                                status, review?.Description, review?.RowVersion, savedQuestions);
                        }).ToList())).ToList())).ToList();
        return new(customer.Id, !string.IsNullOrWhiteSpace(customer.CompanyName)
            ? customer.CompanyName : customer.AgentName, customer.CustomerCode, customer.Mobile,
            customer.AgentName, orders, questions);
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
        if (!selected.IsSubsetOf(allowed))
            return new(400, "اقلام ارسال‌شده با فاکتورهای تحویل‌شده این مشتری مطابقت ندارد. فرم را مجدداً باز کنید.");

        var definitions = await db.QualityControlQuestions.AsNoTracking()
            .Where(question => question.IsActive).ToDictionaryAsync(question => question.Id, cancellationToken);
        foreach (var input in inputs)
        {
            if (input.Status is not (QualityControlStatus.NotReviewed or QualityControlStatus.NoDiscrepancy or QualityControlStatus.HasDiscrepancy)
                || input.Description?.Length > 4000)
                return new(400, "وضعیت یا طول شرح مغایرت معتبر نیست.");
            if (input.Status != QualityControlStatus.HasDiscrepancy)
            {
                if (input.Answers?.Count > 0)
                    return new(400, "برای وضعیت بدون مغایرت یا بررسی‌نشده، پاسخ جدید ارسال نکنید.");
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

        // No record needs to be created if the client returns an untouched item to unchecked.
        if (inputs.Any(input => input.Status == QualityControlStatus.NotReviewed
            && !existing.ContainsKey(input.FactorItemId)))
            return new(409, "وضعیت یکی از اقلام تغییر کرده است؛ فرم را مجدداً باز کنید.");

        // A successful QC result supersedes a previous unreachable/invalid-number mark.
        // Save both changes in the same EF transaction so the queue never retains both.
        if (inputs.Any(input => input.Status != QualityControlStatus.NotReviewed))
        {
            var contact = await db.QualityControlCustomerContacts
                .FirstOrDefaultAsync(item => item.CustomerId == request.CustomerId, cancellationToken);
            if (contact is { Status: not QualityControlCustomerContactStatus.None })
            {
                contact.Status = QualityControlCustomerContactStatus.None;
                contact.InvalidMobileAtMark = null;
                var clearDate = DateTime.Now;
                Stamp(contact, access, clearDate, persianNow, false);
                var eventRow = new QualityControlContactHistory
                {
                    Id = Guid.NewGuid(), CustomerId = request.CustomerId,
                    Status = QualityControlCustomerContactStatus.None,
                    AttemptCount = 0,
                    Description = "پایان وضعیت تماس پس از ثبت کنترل کیفیت"
                };
                Stamp(eventRow, access, clearDate, persianNow, true);
                db.QualityControlContactHistories.Add(eventRow);
            }
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
            // Retain prior discrepancy details on uncheck, so an accidental second click is reversible.
            if (input.Status == QualityControlStatus.HasDiscrepancy)
                review.Description = string.IsNullOrWhiteSpace(input.Description) ? null : input.Description.Trim();
            else if (input.Status == QualityControlStatus.NoDiscrepancy)
                review.Description = null;

            var requestedAnswers = input.Status == QualityControlStatus.HasDiscrepancy
                ? input.Answers! : new List<QualityControlAnswerInput>();
            var keyed = review.Answers.ToDictionary(answer => answer.QuestionId);
            var answerIds = requestedAnswers.Select(answer => answer.QuestionId).ToHashSet();
            if (input.Status == QualityControlStatus.NoDiscrepancy)
                foreach (var stale in review.Answers.ToList())
                    db.QualityControlItemAnswers.Remove(stale);
            else if (input.Status == QualityControlStatus.HasDiscrepancy)
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
        return new(200, request.SaveAll ? "نتیجه کنترل کیفیت اقلام انتخاب‌شده ثبت شد." : "وضعیت کنترل کیفیت قلم ثبت شد.",
            updated.Select(review => new QualityControlItemVersion(review.FactorItemId, review.RowVersion)).ToList());
    }

    public async Task<QualityControlCustomerNotesPage?> GetCustomerNotesAsync(
        Guid customerId, int start, int length, QualityControlAccess access, CancellationToken cancellationToken)
    {
        if (customerId == Guid.Empty || start < 0 || length is < 1 or > 50
            || !await IsHistoryCustomerVisibleAsync(customerId, access, cancellationToken))
            return null;

        var notes = db.QualityControlCustomerNotes.AsNoTracking()
            .Where(note => note.CustomerId == customerId);
        var total = await notes.CountAsync(cancellationToken);
        var page = await notes.OrderByDescending(note => note.CreateDate)
            .ThenByDescending(note => note.Id)
            .Skip(start).Take(length).ToListAsync(cancellationToken);
        return new(customerId, total, page.Select(note => new QualityControlCustomerNoteRow(
            note.Id, note.Description, note.Source,
            PersianDateTime(note.CreateDate), note.UserCreator)).ToList());
    }

    public async Task<AddQualityControlCustomerNoteResult> AddCustomerNoteAsync(
        AddQualityControlCustomerNoteRequest request, QualityControlAccess access,
        string persianNow, CancellationToken cancellationToken)
    {
        var description = request.Description?.Trim();
        if (request.CustomerId == Guid.Empty || string.IsNullOrWhiteSpace(description)
            || description.Length > 4000)
            return new(400, "متن یادداشت باید بین ۱ تا ۴۰۰۰ کاراکتر باشد.");

        if (!await IsHistoryCustomerVisibleAsync(request.CustomerId, access, cancellationToken))
            return new(404, "مشتری واجد شرایط کنترل کیفیت یافت نشد.");

        var note = new QualityControlCustomerNote
        {
            Id = Guid.NewGuid(), CustomerId = request.CustomerId,
            Description = description, Source = QualityControlCustomerNoteSource.Manual
        };
        Stamp(note, access, DateTime.Now, persianNow, true);
        db.QualityControlCustomerNotes.Add(note);
        await db.SaveChangesAsync(cancellationToken);
        return new(200, "یادداشت مشتری ثبت شد.");
    }

    private async Task<bool> IsHistoryCustomerVisibleAsync(Guid customerId, QualityControlAccess access,
        CancellationToken ct)
    {
        return await (
            from item in db.FactorItems.AsNoTracking()
            join factor in db.Factors.AsNoTracking() on item.factorId equals factor.Id
            join deal in VisibleDeals(access) on factor.DealId equals (Guid?)deal.Id
            join customer in db.Customers.AsNoTracking()
                on (factor.CustomerId ?? deal.CustomerId) equals (Guid?)customer.Id
            where customer.Id == customerId
                && !customer.IsDeleted && !item.IsDeleted && !factor.IsDeleted && !deal.IsDeleted
                && deal.FinancialStatus == DealFinancialStausEnum.تایید_شده
                && factor.status == FactorStatus.تایید_شده && !factor.Returned
            select item.Id
        ).AnyAsync(ct);
    }

    private static string PersianDateTime(DateTime? value)
    {
        if (!value.HasValue || value.Value < new DateTime(622, 3, 22)) return "تاریخ نامشخص";
        var dt = value.Value;
        var calendar = new System.Globalization.PersianCalendar();
        return $"{calendar.GetYear(dt):0000}/{calendar.GetMonth(dt):00}/{calendar.GetDayOfMonth(dt):00} {dt:HH:mm}";
    }

    // Rebuild the contact snapshot from surviving events, not the other way round.
    // This keeps list filters, retry counters and due-date summaries consistent after
    // administrative soft deletion or restoration of an older contact attempt.
    private async Task RebuildContactSnapshotAsync(Guid customerId, QualityControlAccess access,
        string persianNow, CancellationToken ct)
    {
        var entries = await db.QualityControlContactHistories.AsNoTracking()
            .Where(item => item.CustomerId == customerId)
            .OrderByDescending(item => item.CreateDate).ThenByDescending(item => item.Id)
            .ToListAsync(ct);
        var contact = await db.QualityControlCustomerContacts.IgnoreQueryFilters()
            .FirstOrDefaultAsync(item => item.CustomerId == customerId, ct);
        if (contact == null)
        {
            contact = new QualityControlCustomerContact { Id = Guid.NewGuid(), CustomerId = customerId };
            Stamp(contact, access, DateTime.Now, persianNow, true);
            db.QualityControlCustomerContacts.Add(contact);
        }
        else
        {
            contact.IsDeleted = false;
            Stamp(contact, access, DateTime.Now, persianNow, false);
        }
        var latest = entries.FirstOrDefault();
        contact.NoResponseAttempts = entries.Sum(item => item.AttemptCount);
        contact.LastNoResponseAt = entries.FirstOrDefault(item =>
            item.Status == QualityControlCustomerContactStatus.NoResponse && item.AttemptCount > 0)?.CreateDate;
        contact.Status = latest?.Status ?? QualityControlCustomerContactStatus.None;
        contact.InvalidMobileAtMark = latest?.Status == QualityControlCustomerContactStatus.InvalidNumber
            ? latest.MobileAtAttempt : null;
    }

    public async Task<QualityControlContactHistoryPage?> GetContactHistoryAsync(
        Guid customerId, bool includeDeleted, int start, int length,
        QualityControlAccess access, bool canManage, CancellationToken ct)
    {
        if (customerId == Guid.Empty || start < 0 || length is < 1 or > 100
            || !await IsHistoryCustomerVisibleAsync(customerId, access, ct))
            return null;
        var customerName = await db.Customers.AsNoTracking().Where(item => item.Id == customerId)
            .Select(item => item.CompanyName != null && item.CompanyName != ""
                ? item.CompanyName : item.AgentName).FirstOrDefaultAsync(ct);

        var entries = db.QualityControlContactHistories.IgnoreQueryFilters().AsNoTracking()
            .Where(item => item.CustomerId == customerId && (canManage && includeDeleted || !item.IsDeleted));
        var count = await entries.CountAsync(ct);
        var page = await entries.OrderByDescending(item => item.CreateDate).ThenByDescending(item => item.Id)
            .Skip(start).Take(length).ToListAsync(ct);
        var rows = page.Select(item => new QualityControlContactHistoryRow(
            item.Id, item.Status,
            item.IsLegacy ? "قدیمی؛ زمان دقیق تک‌تک تماس‌ها ثبت نشده" : PersianDateTime(item.CreateDate),
            item.UserCreator, item.AttemptCount, item.IsLegacy, item.MobileAtAttempt,
            item.Description, item.IsDeleted, item.DeleteReason,
            item.DeletedByName, item.DeletedAt.HasValue ? PersianDateTime(item.DeletedAt) : null,
            item.RestoredByName, item.RestoredAt.HasValue ? PersianDateTime(item.RestoredAt) : null,
            item.RowVersion)).ToArray();
        return new QualityControlContactHistoryPage(customerId, customerName, canManage, count, rows);
    }

    public async Task<ChangeQualityControlContactHistoryResult> ChangeContactHistoryDeletionAsync(
        ChangeQualityControlContactHistoryRequest request, QualityControlAccess access,
        string persianNow, CancellationToken cancellationToken)
    {
        if (request.CustomerId == Guid.Empty || request.HistoryId == Guid.Empty || request.RowVersion == null
            || request.RowVersion.Length != 8)
            return new(400, "شناسه یا نسخه سابقه تماس معتبر نیست.");
        if (!await IsHistoryCustomerVisibleAsync(request.CustomerId, access, cancellationToken))
            return new(404, "مشتری واجد شرایط یافت نشد.");

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var entry = await db.QualityControlContactHistories.IgnoreQueryFilters()
            .FirstOrDefaultAsync(item => item.Id == request.HistoryId
                && item.CustomerId == request.CustomerId, cancellationToken);
        if (entry == null)
            return new(404, "سابقه تماس یافت نشد.");
        if (entry.IsDeleted != request.Restore)
            return new(409, "وضعیت حذف این سابقه قبلاً تغییر کرده است؛ تاریخچه را تازه کنید.");
        if (!entry.RowVersion.SequenceEqual(request.RowVersion))
            return new(409, "این سابقه توسط کاربر دیگری تغییر کرده است؛ تاریخچه را تازه کنید.");

        var now = DateTime.Now;
        if (request.Restore)
        {
            entry.IsDeleted = false;
            entry.RestoredAt = now;
            entry.RestoredById = access.UserId;
            entry.RestoredByName = access.UserName;
        }
        else
        {
            entry.IsDeleted = true;
            entry.DeletedAt = now;
            entry.DeletedById = access.UserId;
            entry.DeletedByName = access.UserName;
        }
        Stamp(entry, access, now, persianNow, false);
        try
        {
            db.Entry(entry).Property(item => item.RowVersion).OriginalValue = request.RowVersion;
            await db.SaveChangesAsync(cancellationToken);
            await RebuildContactSnapshotAsync(request.CustomerId, access, persianNow, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return new(409, "اطلاعات تماس توسط کاربر دیگری تغییر کرده است. دوباره بارگذاری کنید.");
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            return new(409, "اطلاعات تماس هم‌زمان تغییر کرده است؛ تاریخچه را دوباره دریافت کنید.");
        }

        return new(200, request.Restore ? "سابقه تماس بازگردانی شد." : "سابقه تماس با حفظ اطلاعات حذف ثبت شد.");
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
