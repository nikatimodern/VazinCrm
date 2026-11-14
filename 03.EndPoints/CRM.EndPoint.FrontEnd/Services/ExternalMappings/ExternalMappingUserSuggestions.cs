using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.ASettings.Enums;
using CRM.Core.Domain.Sepidar.Parties.Entities;
using CRM.Infrastructures.Sql;
using Generics.Infrastructures.Sql;
using Microsoft.EntityFrameworkCore;

namespace CRM.EndPoint.FrontEnd.Services.ExternalMappings
{
    public sealed partial class ExternalMappingService
    {
        private static string NormalizeExpertName(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "";
            return Regex.Replace(value.Trim().Replace('ي', 'ی').Replace('ك', 'ک'), @"\s+", " ")
                .ToUpperInvariant();
        }

        // Only exact, unique normalized names can be proposed as ready-to-save.
        // No mapping is inserted or updated by suggestion calculation.
        private async Task<List<MappingReportEntry>> BuildUserExpertSuggestionsAsync(
            Guid softwareId, CancellationToken ct)
        {
            var sepidar = softwareId == Sepidar1SoftwareId
                ? (SepidarContextDbBase)sdb : (SepidarContextDbBase)s2Db;

            var users = await userManager.Users.AsNoTracking()
                .Where(x => x.IsEmployee)
                .Select(x => new { x.Id, x.FirstName, x.LastName, x.UserName })
                .ToListAsync(ct);
            var externalPeople = await sepidar.Parties.AsNoTracking()
                .Select(x => new { x.PartyId, x.Name, x.LastName })
                .ToListAsync(ct);
            var saved = await db.ExternalMappings.AsNoTracking()
                .Where(x => x.ExternalSoftwareId == softwareId
                    && x.MapType == ExternalMappingTypeEnum.واسط)
                .Select(x => new { x.EntityId, x.ExEntityId })
                .ToListAsync(ct);

            var byName = externalPeople
                .GroupBy(x => NormalizeExpertName(x.Name + " " + x.LastName))
                .Where(x => !string.IsNullOrEmpty(x.Key))
                .ToDictionary(x => x.Key, x => x.Select(b => b.PartyId).Distinct().ToArray());
            var savedSources = saved.Select(x => x.EntityId).ToHashSet();
            var usedTargets = saved.Select(x => x.ExEntityId).ToHashSet(StringComparer.Ordinal);
            var result = new List<MappingReportEntry>();
            var userNameCounts = users
                .GroupBy(x => NormalizeExpertName(x.FirstName + " " + x.LastName))
                .ToDictionary(x => x.Key, x => x.Count());

            foreach (var user in users)
            {
                if (!Guid.TryParse(user.Id, out var id) || id == Guid.Empty || savedSources.Contains(id))
                    continue;
                var name = NormalizeExpertName(user.FirstName + " " + user.LastName);
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
                    "pending" => "UserExpertExactMatch",
                    "blocked" => "UserExpertAlreadyMapped",
                    "ambiguous" when duplicateUserNames => "UserExpertMultipleCrmUsers",
                    "ambiguous" => "UserExpertMultipleMatches",
                    _ => "UserExpertNoMatch"
                };
                var target = status == "pending" ? free[0] : (int?)null;
                result.Add(new MappingReportEntry(
                    $"user-expert-{id}", "AppUser", id, status, reason,
                    status == "pending" ? "نام کارشناس CRM و شخص متناظر در نرم‌افزار مالی یکسان است."
                        : status == "blocked" ? "اشخاص هم‌نام قبلاً به کاربران دیگری نگاشت شده‌اند."
                        : status == "ambiguous" ? "چند شخص هم‌نام وجود دارد؛ معادل درست کارشناس را انتخاب کنید."
                        : "شخص هم‌نام قطعی پیدا نشد؛ می‌توانید نگاشت کارشناس را دستی ثبت کنید.",
                    nameof(Party), target, Array.Empty<string>(),
                    (int)ExternalMappingTypeEnum.واسط, target?.ToString(),
                    free));
            }
            return result;
        }
    }
}
