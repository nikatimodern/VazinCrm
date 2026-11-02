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
using System.Globalization;

namespace CRM.Infrastructures.Sql.QualityControls.Repositories;

public sealed class QualityControlRepository(CRMContextDb db) : IQualityControlRepository
{
    public async Task<QualityControlListResult> GetDealsAsync(QualityControlListRequest request, QualityControlAccess access, CancellationToken cancellationToken)
    {
        var deals = VisibleDeals(access);
        var recordsTotal = await deals.CountAsync(cancellationToken);
        if (request.FinancialStatus.HasValue)
            deals = deals.Where(deal => deal.FinancialStatus == (DealFinancialStausEnum)request.FinancialStatus.Value);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            deals = deals.Where(deal => deal.SerialNumber.ToString() == search || deal.CustomerCode.ToString() == search
                || (deal.Title != null && deal.Title.Contains(search))
                || (deal.CustomerName != null && deal.CustomerName.Contains(search))
                || (deal.CusCompanyName != null && deal.CusCompanyName.Contains(search)));
        }
        deals = request.Status switch {
            QualityControlStatus.NotReviewed => deals.Where(deal => !db.QualityControls.Any(control => control.DealId == deal.Id)),
            QualityControlStatus.NoDiscrepancy => deals.Where(deal => db.QualityControls.Any(control => control.DealId == deal.Id)
                && !db.QualityControlAnswers.Any(answer => answer.QualityControl.DealId == deal.Id && answer.Answer == 1)),
            QualityControlStatus.HasDiscrepancy => deals.Where(deal => db.QualityControlAnswers
                .Any(answer => answer.QualityControl.DealId == deal.Id && answer.Answer == 1)),
            _ => deals
        };
        var recordsFiltered = await deals.CountAsync(cancellationToken);
        var data = await deals.OrderByDescending(deal => deal.CreateDate).ThenByDescending(deal => deal.Id)
            .Skip(Math.Max(0, request.Start)).Take(Math.Clamp(request.Length, 1, 100))
            .Select(deal => new QualityControlRow {
                DealId = deal.Id, SerialNumber = deal.SerialNumber, Title = deal.Title,
                CustomerName = deal.CustomerName, CustomerCode = deal.CustomerCode,
                CreateDate = deal.CreateDateTxt, UserCreator = deal.UserCreator,
                FinancialApproved = deal.FinancialStatus == DealFinancialStausEnum.تایید_شده,
                Status = !db.QualityControls.Any(control => control.DealId == deal.Id)
                    ? QualityControlStatus.NotReviewed
                    : db.QualityControlAnswers.Any(answer => answer.QualityControl.DealId == deal.Id && answer.Answer == 1)
                        ? QualityControlStatus.HasDiscrepancy : QualityControlStatus.NoDiscrepancy
            }).ToListAsync(cancellationToken);
        return new QualityControlListResult(request.Draw, recordsTotal, recordsFiltered, data);
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
                    db.SupplyFactors.Count(x => x.DealId == item.Id))))
            .FirstOrDefaultAsync(cancellationToken);
        if (deal == null)
            return null;

        if (deal.CustomerId.HasValue)
        {
            var payments = await db.Accountancies.AsNoTracking().ForCustomer(deal.CustomerId).NotVosoled()
                .Where(payment => payment.AccType == AcEnum.واریزی)
                .GroupBy(payment => payment.CurrencyType)
                .Select(group => new { Currency = group.Key, Count = group.Count(), Amount = group.Sum(payment => payment.credit) ?? 0 })
                .ToListAsync(cancellationToken);
            deal = deal with {
                CustomerPaymentCount = payments.Sum(payment => payment.Count),
                CustomerPaymentTotals = payments.Select(payment => new QualityControlPaymentTotal(
                    payment.Currency.ToString(), payment.Amount.ToString("#,0.############################", CultureInfo.InvariantCulture))).ToList()
            };
        }

        var factorTotals = await db.Factors.AsNoTracking().Where(factor => factor.DealId == dealId)
            .GroupBy(factor => factor.CurrencyType)
            .Select(group => new { Currency = group.Key, Amount = group.Sum(factor => factor.SumTotalAmountTax) })
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
            questions.Values.OrderBy(question => question.SortOrder).ThenBy(question => question.Id).ToList());
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
