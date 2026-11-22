using System;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.ExternalSoftwares.Enums;
using CRM.Infrastructures.Sql;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace CRM.EndPoint.FrontEnd.Services.SepidarServices;

public sealed class SepidarUserResolver(CRMContextDb crmContext, IHttpContextAccessor http)
{
    public async Task<int> GetCurrentExternalUserIdAsync(Guid destinationId, SepidarService destination, CancellationToken ct = default)
    {
        var principal = http.HttpContext?.User;
        if (principal?.Identity?.IsAuthenticated != true
            || !Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            throw new SepidarTransferValidationException("کاربر جاری برای ثبت در نرم‌افزار مالی شناسایی نشد.");

        var mappings = await crmContext.ExternalMappings.AsNoTracking()
            .Where(x => x.EntityId == userId && x.ExternalSoftwareId == destinationId
                && x.MapType == ExternalMappingTypeEnum.کاربران && x.EntityName == "AppUser")
            .Select(x => x.ExEntityId).Take(2).ToListAsync(ct);
        if (mappings.Count == 0)
            throw new SepidarTransferValidationException("کاربر شما در نرم‌افزار مالی مقصد معادل ندارد. ارتباط کاربر را در بخش ارتباط با نرم‌افزارهای مالی ثبت کنید.");
        if (mappings.Count != 1 || !int.TryParse(mappings[0], NumberStyles.None, CultureInfo.InvariantCulture, out var creator)
            || creator <= 0 || !await destination.IsActiveUserAsync(creator, ct))
            throw new SepidarTransferValidationException("ارتباط کاربر شما با نرم‌افزار مالی مقصد معتبر نیست یا کاربر مقصد غیرفعال است.");

        return creator;
    }
}
