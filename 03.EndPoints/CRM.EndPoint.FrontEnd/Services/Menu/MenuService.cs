using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using CRM.Core.ApplicationServices.Security;
using CRM.Core.Domain.AccessCatalog.Entities;
using CRM.Core.Domain.AccessCatalog.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using CRM.Core.ApplicationServices.ReleaseNotes.Services;
using CRM.Core.Domain.ReleaseNotes.Repositories;
using CRM.EndPoint.FrontEnd.IServices.Menu;
using CRM.EndPoint.FrontEnd.Models.Menu;

namespace CRM.EndPoint.FrontEnd.Services.Menu;

public class MenuService(
    IServiceProvider serviceProvider,
    MenuDefinitionService definitions,
    MenuBadgeService badges,
    MenuFavoritesService favorites,
    ReleaseNoteAccess releaseNoteAccess,
    IReleaseNoteRepository releaseNotes) : IMenuService
{
    public async Task<List<MenuItem>> GetMenusAsync(ClaimsPrincipal user)
    {
        var roles = user.Claims.Where(claim => claim.Type == ClaimTypes.Role)
            .Select(claim => claim.Value).ToHashSet();

        var catalogViews = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        // Catalog-controlled links are evaluated before legacy role filtering.
        // A granted child can appear even if its legacy parent group was hidden.
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrWhiteSpace(userId))
        {
            var settings = serviceProvider.GetRequiredService<IConfiguration>();
            var scopes = serviceProvider.GetRequiredService<IAccessScopeService>();
            foreach (var entry in new[] {
                (Entity: "Dashboard", Url: "/Dashboard/Index"),
                (Entity: "Customer", Url: "/Customer/Index"),
                (Entity: "Deal", Url: "/Deal/Index"),
                (Entity: "OrderHead", Url: "/Order/Index"),
                (Entity: "SupplyFactor", Url: "/Supply/SupplyFactor"),
                (Entity: "Accountancy", Url: "/Accountancy/Index"),
                (Entity: "RequestAmount", Url: "/RequestAmount/Index"),
                (Entity: "Payment", Url: "/Supply/Payment"),
                (Entity: "Checkbook", Url: "/Check/Index"),
                (Entity: "Check", Url: "/Check/Received"),
                (Entity: "Transfer", Url: "/Transfer/Index"),
                (Entity: "Cash", Url: "/Cash/Index"),
                (Entity: "Cashier", Url: "/Cash/Cashier"),
                (Entity: "BaseCash", Url: "/BaseItem/Financial"),
                (Entity: "Warehouse", Url: "/Warehouse/Index"),
                (Entity: "WarehouseVoucher", Url: "/Warehouse/Voucher"),
                (Entity: "QualityControl", Url: "/QualityControl/Index"),
                (Entity: "ProductAmount", Url: "/Amount/Index"),
                (Entity: "Product", Url: "/Amount/Index"),
                (Entity: "Carizo", Url: "/Carizo/Index"),
                (Entity: "Report", Url: "/Report/Index"),
                (Entity: "Contract", Url: "/Contract/Index")
            })
            {
                if (!settings.GetValue<bool>("AccessCatalog:Enforce" + entry.Entity + "Permissions")) continue;
                var scope = await scopes.GetEffectiveScopeAsync(userId, entry.Entity + ".View", default);
                // Employee-only endpoints keep their independent account-type boundary.
                var allowed = scope != AccessScopes.None &&
                    (entry.Entity is "Customer" or "QualityControl" || !user.IsInRole(AppRoles.Customer));
                catalogViews[entry.Url] = allowed && catalogViews.GetValueOrDefault(entry.Url, true);
            }

            // Settings is an action-only group; a delegated child must not be hidden by its role list.
            var canOpenSettings = catalogViews.GetValueOrDefault("/BaseItem/Financial");
            foreach (var entity in new[] { "SentSms", "ReceivedSms" })
                if (settings.GetValue<bool>("AccessCatalog:Enforce" + entity + "Permissions") &&
                    await scopes.GetEffectiveScopeAsync(userId, entity + ".View", default) == AccessScopes.All)
                    canOpenSettings = true;
            if (settings.GetValue<bool>("AccessCatalog:EnforceReleaseNotePermissions") &&
                await scopes.GetEffectiveScopeAsync(userId, "ReleaseNote.View", default) == AccessScopes.All &&
                !user.IsInRole(AppRoles.Customer)) canOpenSettings = true;
            if (settings.GetValue<bool>("AccessCatalog:EnforceSystemWarningPermissions"))
                catalogViews["#warnings"] = await scopes.GetEffectiveScopeAsync(userId, "SystemWarning.View", default) == AccessScopes.All;
            foreach (var entity in new[] { "BaseItem", "ASetting", "ExternalSoftware" })
                if (!user.IsInRole(AppRoles.Customer) && settings.GetValue<bool>("AccessCatalog:Enforce" + entity + "Permissions") &&
                    await scopes.GetEffectiveScopeAsync(userId, entity + ".View", default) == AccessScopes.All) canOpenSettings = true;
            if (canOpenSettings) catalogViews["#settings"] = true;
            // Settings children are defined centrally and must use catalog authorization.
            foreach (var entry in new[] {
                (Entity: "ReleaseNote", Url: "/ReleaseNotesAdmin/Index"),
                (Entity: "BaseItem", Url: "/BaseItem/Index"),
                (Entity: "ASetting", Url: "/Asetting/Index"),
                (Entity: "ExternalSoftware", Url: "/ExternalSoftware/Index")
            })
                if (settings.GetValue<bool>("AccessCatalog:Enforce" + entry.Entity + "Permissions"))
                    catalogViews[entry.Url] = !user.IsInRole(AppRoles.Customer) &&
                        await scopes.GetEffectiveScopeAsync(userId, entry.Entity + ".View", default) == AccessScopes.All;
            var smsEnforced = settings.GetValue<bool>("AccessCatalog:EnforceSentSmsPermissions") ||
                settings.GetValue<bool>("AccessCatalog:EnforceReceivedSmsPermissions");
            if (smsEnforced)
            {
                var canSent = settings.GetValue<bool>("AccessCatalog:EnforceSentSmsPermissions")
                    ? await scopes.GetEffectiveScopeAsync(userId, "SentSms.View", default) == AccessScopes.All
                    : user.IsInRole(AppRoles.Admin);
                var canReceived = settings.GetValue<bool>("AccessCatalog:EnforceReceivedSmsPermissions")
                    ? await scopes.GetEffectiveScopeAsync(userId, "ReceivedSms.View", default) == AccessScopes.All
                    : user.IsInRole(AppRoles.Admin);
                catalogViews["/Sms/Index"] = canSent || canReceived;
            }


            if (settings.GetValue<bool>("AccessCatalog:EnforcePreFactorPermissions") &&
                settings.GetValue<bool>("AccessCatalog:EnforceFactorPermissions") &&
                await scopes.GetEffectiveScopeAsync(userId, "PreFactor.View", default) == AccessScopes.None &&
                await scopes.GetEffectiveScopeAsync(userId, "Factor.View", default) == AccessScopes.None &&
                !user.IsInRole("مدیر ارشد") && !user.IsInRole("کارشناس مالی") && !user.IsInRole("مدیر تامین") &&
                !user.IsInRole("کارشناس تامین") && !user.IsInRole("مدیر عامل"))
                catalogViews["/Cartable/Index"] = false;
        }

        var menus = MenuAccessFilter.Filter(definitions.BuildMenus(user, roles), roles, catalogViews);
        await SetReleaseVersionAsync(menus);
        await badges.PopulateAsync(menus);
        await favorites.AttachAsync(menus, user);
        return menus;
    }

    private async Task SetReleaseVersionAsync(List<MenuItem> menus)
    {
        var version = menus.FirstOrDefault(item => item.Id == "releaseNotes");
        if (version is null) return;

        var reader = await releaseNoteAccess.ReaderAsync(default);
        var latest = await releaseNotes.GetLatestVisibleVersionInfoAsync(reader, DateTime.UtcNow, default);
        version.Title = latest is null ? "نسخه" : $"نسخه {latest.Version}";
        if (latest?.PublishDate is DateTime publishDate)
            version.Subtitle = CRM.Framework.BaseServices.GetDatePersian(publishDate);
    }
}
