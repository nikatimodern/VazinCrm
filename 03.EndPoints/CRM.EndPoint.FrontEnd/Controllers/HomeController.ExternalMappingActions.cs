using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.ApplicationServices.Security;
using CRM.Core.Domain.ASettings.Enums;
using CRM.Core.Domain.Customers.Entities;
using CRM.Core.Domain.ExternalSoftwares.Entites;
using CRM.Core.Domain.Factors.Entities;
using CRM.Core.Domain.Factors.Enum;
using CRM.Core.Domain.PreFactors.Entities;
using Generics.Infrastructures.Sql;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM.EndPoint.FrontEnd.Controllers
{
    public partial class HomeController
    {
        public sealed class SelectedExternalMapping
        {
            public Guid EntityId { get; set; }
            public ExternalMappingTypeEnum MapType { get; set; }
            public string ExEntityId { get; set; }
        }

        public sealed class SaveExternalMappingsRequest
        {
            public Guid ExternalSoftwareId { get; set; }
            public List<SelectedExternalMapping> Items { get; set; } = new();
        }

        [Authorize(Roles = AppRoles.Admin)]
        [HttpGet]
        public async Task<IActionResult> GetExternalMappingWorkspaceOptions(CancellationToken ct)
        {
            var software = await db.ExternalSoftwares.AsNoTracking()
                .OrderBy(x => x.Title)
                .Select(x => new { id = x.Id, title = x.Title })
                .ToListAsync(ct);

            var counts = await db.ExternalMappings.AsNoTracking()
                .GroupBy(x => new { x.ExternalSoftwareId, x.MapType })
                .Select(x => new { x.Key.ExternalSoftwareId, x.Key.MapType, Count = x.Count() })
                .ToListAsync(ct);

            var mapTypes = Enum.GetValues<ExternalMappingTypeEnum>()
                .Select(x => new {
                    value = (int)x, title = x.ToString().Replace("_", " "),
                    saved = counts.Where(c => c.MapType == x).Sum(c => c.Count)
                }).ToArray();

            return Ok(new {
                software,
                mapTypes,
                counts = counts.Select(x => new {
                    softwareId = x.ExternalSoftwareId,
                    mapType = (int)x.MapType,
                    count = x.Count
                }),
                automaticMatchingSoftwareId = Sepidar1SoftwareId,
                supportedManualSoftwareIds = new[] { Sepidar1SoftwareId, Sepidar2SoftwareId }
            });
        }

        [Authorize(Roles = AppRoles.Admin)]
        [HttpGet]
        public async Task<IActionResult> GetExternalMappingTargetDetails(
            Guid softwareId, int mapType, int targetId, CancellationToken ct)
        {
            if (targetId <= 0 || !Enum.IsDefined(typeof(ExternalMappingTypeEnum), mapType)
                || softwareId != Sepidar1SoftwareId && softwareId != Sepidar2SoftwareId
                || !await db.ExternalSoftwares.AsNoTracking().AnyAsync(x => x.Id == softwareId, ct))
                return BadRequest("نرم‌افزار، نوع نگاشت یا شناسه معتبر نیست.");

            var sepidar = softwareId == Sepidar1SoftwareId
                ? (SepidarContextDbBase)sdb : s2Db;
            var entityName = mapType == (int)ExternalMappingTypeEnum.مشتری ? nameof(Customer)
                : mapType == (int)ExternalMappingTypeEnum.پیش_فاکتور ? nameof(PreFactor)
                : mapType == (int)ExternalMappingTypeEnum.فاکتور ? nameof(Factor) : null;
            if (entityName == null)
                return BadRequest("نمایش معادل برای این نوع نگاشت هنوز پشتیبانی نمی‌شود.");

            var externalName = entityName == nameof(Customer) ? "Party"
                : entityName == nameof(PreFactor) ? "Quotation" : "Invoice";
            var exists = entityName == nameof(Customer)
                ? await sepidar.Parties.AsNoTracking().AnyAsync(x => x.PartyId == targetId && x.IsCustomer, ct)
                : entityName == nameof(PreFactor)
                    ? await sepidar.Quotations.AsNoTracking().AnyAsync(x => x.QuotationId == targetId, ct)
                    : await sepidar.SalesInvoices.AsNoTracking().AnyAsync(x => x.InvoiceId == targetId, ct);
            if (!exists) return NotFound("معادل انتخاب‌شده در نرم‌افزار خارجی پیدا نشد.");

            var entry = new MappingReportEntry(
                "manual-preview", entityName, Guid.Empty, "pending", "ReadyForReview",
                "", externalName, targetId, Array.Empty<string>(), mapType,
                targetId.ToString(), new[] { targetId });
            var rows = await EnrichMappingEntriesAsync(new List<MappingReportEntry> { entry }, softwareId, ct);
            return Ok(new { target = rows[0].Sepidar });
        }

        // Only selected rows are submitted; no automatic migration runs from page visits.
        // On success each record becomes a persisted ExternalMapping, never an external INSERT.
        [Authorize(Roles = AppRoles.Admin)]
        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> SaveSelectedExternalMappings(
            [FromBody] SaveExternalMappingsRequest request, CancellationToken ct)
        {
            if (request == null || request.Items == null || request.Items.Count == 0 || request.Items.Count > 100)
                return BadRequest("بین ۱ تا ۱۰۰ نگاشت را انتخاب کنید.");

            if (request.ExternalSoftwareId != Sepidar1SoftwareId
                && request.ExternalSoftwareId != Sepidar2SoftwareId)
                return BadRequest("برای این نرم‌افزار، ثبت دستی هنوز پشتیبانی نمی‌شود.");

            if (!await db.ExternalSoftwares.AsNoTracking()
                    .AnyAsync(x => x.Id == request.ExternalSoftwareId, ct))
                return BadRequest("نرم‌افزار انتخاب‌شده ثبت نشده است.");

            var repeatedSources = request.Items.GroupBy(x => (x.MapType, x.EntityId))
                .Any(x => x.Count() > 1);
            if (repeatedSources)
                return BadRequest("برای هر رکورد CRM فقط یک معادل انتخاب کنید.");

            var allowed = new[] {
                ExternalMappingTypeEnum.مشتری,
                ExternalMappingTypeEnum.پیش_فاکتور,
                ExternalMappingTypeEnum.فاکتور
            };
            if (request.Items.Any(x => !allowed.Contains(x.MapType)
                || x.EntityId == Guid.Empty
                || !int.TryParse(x.ExEntityId, out var id) || id <= 0))
                return BadRequest("نوع نگاشت یا شناسه انتخاب‌شده معتبر نیست.");

            var hasCustomer = request.Items.Any(x => x.MapType == ExternalMappingTypeEnum.مشتری);
            var hasOther = request.Items.Any(x => x.MapType != ExternalMappingTypeEnum.مشتری);
            if (hasCustomer && hasOther)
                return BadRequest("ابتدا نگاشت مشتریان را ثبت کنید؛ سپس پیش‌فاکتور یا فاکتور را انتخاب کنید.");

            // Force users to resolve customer conflicts explicitly rather than allowing
            // a bulk click to select an arbitrary Party for several CRM customers.
            var crmUser = await userManager.GetUserAsync(User);
            if (crmUser == null) return Unauthorized();

            var targetDb = request.ExternalSoftwareId == Sepidar1SoftwareId
                ? (SepidarContextDbBase)sdb : s2Db;

            await using var transaction = await db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable, ct);

            try
            {
                var existing = await db.ExternalMappings.AsNoTracking()
                    .Where(x => x.ExternalSoftwareId == request.ExternalSoftwareId)
                    .ToListAsync(ct);

                var duplicatesInRequest = request.Items.GroupBy(x =>
                    (x.MapType, x.ExEntityId.Trim())).Any(x => x.Count() > 1);
                if (duplicatesInRequest)
                    return BadRequest("چند رکورد CRM نمی‌توانند به یک معادل خارجی متصل شوند.");

                var confirmedCustomerMappings = existing
                    .Where(x => x.MapType == ExternalMappingTypeEnum.مشتری
                        && x.EntityName == nameof(Customer))
                    .GroupBy(x => x.EntityId)
                    .ToDictionary(x => x.Key, x => x.ToList());

                var newMappings = new List<ExternalMapping>();
                foreach (var item in request.Items)
                {
                    var targetId = int.Parse(item.ExEntityId.Trim());
                    var entityName = item.MapType switch {
                        ExternalMappingTypeEnum.مشتری => nameof(Customer),
                        ExternalMappingTypeEnum.پیش_فاکتور => nameof(PreFactor),
                        _ => nameof(Factor)
                    };
                    var externalName = item.MapType switch {
                        ExternalMappingTypeEnum.مشتری => "Party",
                        ExternalMappingTypeEnum.پیش_فاکتور => "Quotation",
                        _ => "Invoice"
                    };

                    if (existing.Any(x => x.EntityName == entityName
                        && x.EntityId == item.EntityId && x.MapType == item.MapType))
                        return Conflict($"این {item.MapType} در نرم‌افزار انتخاب‌شده قبلاً نگاشت شده است.");

                    if (existing.Any(x => x.ExEntityName == externalName
                        && x.ExEntityId == targetId.ToString()
                        && x.EntityId != item.EntityId))
                        return Conflict($"معادل خارجی شماره {targetId} قبلاً به رکورد CRM دیگری وصل شده است.");

                    if (item.MapType == ExternalMappingTypeEnum.مشتری)
                    {
                        if (!await db.Customers.AsNoTracking().AnyAsync(x => x.Id == item.EntityId, ct))
                            return BadRequest("مشتری انتخاب‌شده در CRM یافت نشد.");
                        if (!await targetDb.Parties.AsNoTracking()
                                .AnyAsync(x => x.PartyId == targetId && x.IsCustomer, ct))
                            return BadRequest($"مشتری با شناسه {targetId} در نرم‌افزار مقصد معتبر نیست.");
                    }
                    else
                    {
                        Guid? customerId;
                        int partyId;
                        if (item.MapType == ExternalMappingTypeEnum.پیش_فاکتور)
                        {
                            var pf = await db.PreFactors.AsNoTracking()
                                .Where(x => x.Id == item.EntityId)
                                .Select(x => new { x.CustomerId })
                                .SingleOrDefaultAsync(ct);
                            if (pf == null || !pf.CustomerId.HasValue)
                                return BadRequest("پیش‌فاکتور CRM مشتری مشخصی ندارد.");
                            customerId = pf.CustomerId;
                            var external = await targetDb.Quotations.AsNoTracking()
                                .Where(x => x.QuotationId == targetId)
                                .Select(x => new { x.CustomerPartyRef })
                                .SingleOrDefaultAsync(ct);
                            if (external == null)
                                return BadRequest($"پیش‌فاکتور با شناسه {targetId} در نرم‌افزار مقصد پیدا نشد.");
                            partyId = external.CustomerPartyRef;
                        }
                        else
                        {
                            var factor = await db.Factors.AsNoTracking()
                                .Where(x => x.Id == item.EntityId)
                                .Select(x => new { x.CustomerId, x.PfactorID, x.status, x.ParentReturnedId })
                                .SingleOrDefaultAsync(ct);
                            if (factor == null || factor.status != FactorStatus.تایید_شده
                                || factor.ParentReturnedId.HasValue)
                                return BadRequest("فقط فاکتور تأییدشده و غیربرگشتی قابل نگاشت است.");

                            var pfCustomerId = await db.PreFactors.AsNoTracking()
                                .Where(x => x.Id == factor.PfactorID)
                                .Select(x => x.CustomerId)
                                .FirstOrDefaultAsync(ct);
                            if (factor.CustomerId.HasValue && pfCustomerId.HasValue
                                && factor.CustomerId != pfCustomerId)
                                return BadRequest("مشتری فاکتور و پیش‌فاکتور مرتبط متفاوت است.");
                            customerId = factor.CustomerId ?? pfCustomerId;
                            if (!customerId.HasValue)
                                return BadRequest("مشتری فاکتور مشخص نیست.");

                            var external = await targetDb.SalesInvoices.AsNoTracking()
                                .Where(x => x.InvoiceId == targetId)
                                .Select(x => new { x.CustomerPartyRef, x.QuotationRef })
                                .SingleOrDefaultAsync(ct);
                            if (external == null)
                                return BadRequest($"فاکتور با شناسه {targetId} در نرم‌افزار مقصد پیدا نشد.");
                            partyId = external.CustomerPartyRef;

                            var crmQuotationMapping = existing.FirstOrDefault(x =>
                                x.MapType == ExternalMappingTypeEnum.پیش_فاکتور
                                && x.EntityId == factor.PfactorID);
                            if (crmQuotationMapping != null
                                && crmQuotationMapping.ExEntityId != external.QuotationRef?.ToString())
                                return BadRequest("پیش‌فاکتور مرتبط با فاکتور سپیدار، با نگاشت CRM هم‌خوان نیست.");
                        }

                        if (!confirmedCustomerMappings.TryGetValue(customerId.Value, out var customerMaps)
                            || customerMaps.Count != 1
                            || customerMaps[0].ExEntityName != "Party"
                            || customerMaps[0].ExEntityId != partyId.ToString())
                            return BadRequest("ابتدا مشتری مربوط به فاکتور یا پیش‌فاکتور را در همین نرم‌افزار به‌درستی نگاشت کنید.");
                    }

                    newMappings.Add(new ExternalMapping {
                        Id = Guid.NewGuid(),
                        ExternalSoftwareId = request.ExternalSoftwareId,
                        EntityName = entityName,
                        EntityId = item.EntityId,
                        MapType = item.MapType,
                        MapTypeTxt = item.MapType.ToString(),
                        ExEntityName = externalName,
                        ExEntityId = targetId.ToString(),
                        UserCreatorID = crmUser.Id,
                        UserCreator = crmUser.FullName
                    });
                }

                db.ExternalMappings.AddRange(newMappings);
                await db.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
                return Ok(new { count = newMappings.Count });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Saving selected external mappings failed.");
                return Conflict("اطلاعات نگاشت در این فاصله تغییر کرده است؛ فهرست را دوباره دریافت کنید.");
            }
        }
    }
}
