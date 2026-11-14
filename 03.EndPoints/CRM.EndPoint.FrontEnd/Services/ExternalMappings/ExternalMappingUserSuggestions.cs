using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.ASettings.Enums;
using CRM.Core.Domain.Sepidar.Parties.Entities;
using Generics.Infrastructures.Sql;
using Microsoft.EntityFrameworkCore;

namespace CRM.EndPoint.FrontEnd.Services.ExternalMappings
{
    public sealed partial class ExternalMappingService
    {
        private static string NormalizeBrokerName(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "";
            return Regex.Replace(value.Trim().Replace('ي', 'ی').Replace('ك', 'ک'), @"\s+", " ")
                .ToUpperInvariant();
        }

        // Only exact, unique normalized names can be proposed as ready-to-save.
        // No mapping is inserted or updated by suggestion calculation.
        private async Task<List<MappingReportEntry>> BuildUserBrokerSuggestionsAsync(
            Guid softwareId, CancellationToken ct)
        {
            var sepidar = softwareId == Sepidar1SoftwareId
                ? (SepidarContextDbBase)sdb : (SepidarContextDbBase)s2Db;

            var users = await userManager.Users.AsNoTracking()
                .Where(x => x.IsEmployee)
                .Select(x => new { x.Id, x.FirstName, x.LastName, x.UserName })
                .ToListAsync(ct);
            var brokers = await sepidar.Parties.AsNoTracking()
                .Where(x => x.IsBroker && x.Dl.IsActive)
                .Select(x => new { x.PartyId, x.Name, x.LastName })
                .ToListAsync(ct);
            var saved = await db.ExternalMappings.AsNoTracking()
                .Where(x => x.ExternalSoftwareId == softwareId
                    && x.MapType == ExternalMappingTypeEnum.واسط)
                .Select(x => new { x.EntityId, x.ExEntityId })
                .ToListAsync(ct);

            var byName = brokers
                .GroupBy(x => NormalizeBrokerName(x.Name + " " + x.LastName))
                .Where(x => !string.IsNullOrEmpty(x.Key))
                .ToDictionary(x => x.Key, x => x.Select(b => b.PartyId).Distinct().ToArray());
            var savedSources = saved.Select(x => x.EntityId).ToHashSet();
            var usedTargets = saved.Select(x => x.ExEntityId).ToHashSet(StringComparer.Ordinal);
            var result = new List<MappingReportEntry>();
            var userNameCounts = users
                .GroupBy(x => NormalizeBrokerName(x.FirstName + " " + x.LastName))
                .ToDictionary(x => x.Key, x => x.Count());

            foreach (var user in users)
            {
                if (!Guid.TryParse(user.Id, out var id) || id == Guid.Empty || savedSources.Contains(id))
                    continue;
                var name = NormalizeBrokerName(user.FirstName + " " + user.LastName);
                if (string.IsNullOrEmpty(name))
                    continue;

                var candidates = byName.GetValueOrDefault(name) ?? Array.Empty<int>();
                var free = candidates.Where(x => !usedTargets.Contains(x.ToString())).ToArray();
                var duplicateUserNames = userNameCounts.GetValueOrDefault(name) > 1;
                var status = duplicateUserNames && free.Length > 0 ? "ambiguous"
                    : free.Length == 1 && candidates.Length == 1 ? "pending"
                    : candidates.Length > 0 && free.Length == 0 ? "blocked"
                    : free.Length > 1 ? "ambiguous" : "notMatched";
                var reason = status switch {
                    "pending" => "UserBrokerExactMatch",
                    "blocked" => "UserBrokerAlreadyMapped",
                    "ambiguous" when duplicateUserNames => "UserBrokerMultipleCrmUsers",
                    "ambiguous" => "UserBrokerMultipleMatches",
                    _ => "UserBrokerNoMatch"
                };
                var target = status == "pending" ? free[0] : (int?)null;
                result.Add(new MappingReportEntry(
                    $"user-broker-{id}", "AppUser", id, status, reason,
                    status == "pending" ? "نام کاربر CRM و واسط سپیدار یکسان است."
                        : status == "blocked" ? "واسط‌های هم‌نام قبلاً نگاشت شده‌اند."
                        : status == "ambiguous" ? "چند واسط هم‌نام وجود دارد؛ معادل درست را انتخاب کنید."
                        : "واسط هم‌نام قطعی پیدا نشد؛ می‌توانید نگاشت را دستی ثبت کنید.",
                    nameof(Party), target, Array.Empty<string>(),
                    (int)ExternalMappingTypeEnum.واسط, target?.ToString(),
                    free));
            }
            return result;
        }
    }
}
