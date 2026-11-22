using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using CRM.Core.ApplicationServices.QualityControls.Services;
using CRM.Core.Domain.Deals.Enums;
using CRM.Core.Domain.Deals;
using CRM.EndPoint.FrontEnd.IServices.Menu;
using CRM.EndPoint.FrontEnd.Models.Menu;
using CRM.Infrastructures.Sql;
using Generics.Infrastructures.Sql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using CRM.Core.ApplicationServices.Security;
using CRM.Core.Domain.ReleaseNotes.Repositories;
using CRM.Core.ApplicationServices.ReleaseNotes.Services;

namespace CRM.EndPoint.FrontEnd.Services.Menu;

public class MenuService(IServiceProvider serviceProvider) : IMenuService {
    public async Task<List<MenuItem>> GetMenusAsync(ClaimsPrincipal user)
    {
        // Read current roles from Identity, not the potentially stale cookie.
        // This makes the menu snapshot authoritative immediately after role edits.
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        var identityDb = serviceProvider.GetRequiredService<UserContextDb>();
        var roles = string.IsNullOrWhiteSpace(userId)
            ? user.Claims.Where(x => x.Type == ClaimTypes.Role).Select(x => x.Value).ToHashSet()
            : (await (from assignment in identityDb.UserRoles.AsNoTracking()
                join role in identityDb.Roles.AsNoTracking() on assignment.RoleId equals role.Id
                where assignment.UserId == userId
                select role.Name).ToListAsync())
                .Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!).ToHashSet();

        var menus = OrganizeMenus(BuildMenus(user));

        var filteredMenus = Filter(menus, roles);
        var versionMenu = filteredMenus.FirstOrDefault(x => x.Id == "releaseNotes");
        if (versionMenu is not null) {
            // Match the exact audience/role resolution used by the release history.
            var reader = await serviceProvider.GetRequiredService<ReleaseNoteAccess>().ReaderAsync(default);
            var latest = await serviceProvider.GetRequiredService<IReleaseNoteRepository>()
                .GetLatestVisibleVersionInfoAsync(reader, DateTime.UtcNow, default);
            versionMenu.Title = latest is null ? "نسخه" : $"نسخه {latest.Version}";
            if (latest?.PublishDate is DateTime publishDate)
                versionMenu.Subtitle = CRM.Framework.BaseServices.GetDatePersian(publishDate);
        }

        await SetBadgeCountsAsync(filteredMenus);
        await AttachFavoritesAsync(filteredMenus, user);

        return filteredMenus;
    }


    private static List<MenuItem> BuildMenus(ClaimsPrincipal user)
    {
        return [
            #region داشبورد

            new MenuItem {
                Id = "dashboard",
                Title = "داشبورد",
                Url = "/Dashboard/Index",
                IconHtml = MenuIcons.Dashboard,
                Roles = [
                    AppRoles.Admin,
                    AppRoles.ManagingDirector,
                    AppRoles.ExecutiveDirector,
                    AppRoles.SalesExpert,
                    AppRoles.SalesAdmin,
                    AppRoles.TaminExpert,
                    AppRoles.TaminAdmin,
                    AppRoles.FinancialExpert,
                    AppRoles.FinancialAdmin
                ],
                DisplayOrder = 1,
                ShowInMobileBottomMenu = true
            },

            #endregion

            #region سفارشات

            new MenuItem {
                Id = "deal",
                Title = "سفارشات",
                Url = "/Deal/Index",
                IconHtml = MenuIcons.Deals,
                Roles = [
                    AppRoles.Admin,
                    AppRoles.ManagingDirector,
                    AppRoles.ExecutiveDirector,
                    AppRoles.SalesExpert,
                    AppRoles.SalesAdmin,
                    AppRoles.TaminExpert,
                    AppRoles.TaminAdmin,
                ],
                DisplayOrder = 2,
                ShowInMobileBottomMenu = true
            },

            #endregion

            #region Quality control

            new MenuItem {
                Id = "qualityControl",
                Title = "کنترل کیفیت",
                Url = "/QualityControl/Index",
                IconHtml = MenuIcons.QualityControl,
                Roles = [
                    .. QualityControlAccessPolicy.Roles.Split(',')
                ],
                DisplayOrder = 20
            },

            #endregion

            #region اشخاص

            new MenuItem {
                Id = "customer",
                Title = "اشخاص",
                Url = "/Customer/Index",
                IconHtml = MenuIcons.Customers,
                Roles = [
                    AppRoles.Admin,
                    AppRoles.ManagingDirector,
                    AppRoles.ExecutiveDirector,
                    AppRoles.SalesExpert,
                    AppRoles.SalesAdmin,
                    AppRoles.TaminExpert,
                    AppRoles.TaminAdmin
                ],
                DisplayOrder = 3,
                ShowInMobileBottomMenu = true
            },

            #endregion

            #region کارتابل

            new MenuItem {
                Id = "cartable",
                Title = "کارتابل",
                Url = "/Cartable/Index",
                IconHtml = MenuIcons.Cartable,
                Roles = [
                    AppRoles.Admin,
                    AppRoles.ManagingDirector,
                    AppRoles.ExecutiveDirector,
                    AppRoles.SalesExpert,
                    AppRoles.SalesAdmin,
                    AppRoles.TaminExpert,
                    AppRoles.FinancialExpert,
                    AppRoles.FinancialAdmin
                ],
                DisplayOrder = 4,
                ShowInMobileBottomMenu = true
            },

            #endregion

            #region کاربان

            new MenuItem {
                Id = "carizo",
                Title = "کاربان",
                Url = "/Carizo/Index",
                IconHtml = MenuIcons.Tasks,
                Roles = [
                    AppRoles.Admin,
                    AppRoles.ManagingDirector,
                    AppRoles.ExecutiveDirector,
                    AppRoles.SalesExpert,
                    AppRoles.SalesAdmin,
                    AppRoles.TaminExpert,
                    AppRoles.TaminAdmin,
                    AppRoles.FinancialExpert,
                    AppRoles.FinancialAdmin
                ],
                DisplayOrder = 5
            },

            #endregion

            #region گزارش کار

            new MenuItem {
                Id = "report",
                Title = "گزارش کار",
                Url = "/Report/Index",
                IconHtml = MenuIcons.WorkReports,
                Roles = [
                    AppRoles.Admin,
                    AppRoles.ManagingDirector,
                    AppRoles.ExecutiveDirector,
                    AppRoles.SalesExpert,
                    AppRoles.TaminExpert
                ],
                DisplayOrder = 6,
                ShowInMobileBottomMenu = true
            },

            #endregion

            #region حسابداری

            new MenuItem {
                Id = "accountancy",
                Title = "حسابداری",
                Url = "/Accountancy/Index",
                IconHtml = MenuIcons.Accounting,
                Roles = [
                    AppRoles.Admin,
                    AppRoles.ManagingDirector,
                    AppRoles.ExecutiveDirector,
                    AppRoles.SalesExpert,
                    AppRoles.TaminExpert,
                    AppRoles.FinancialExpert,
                    AppRoles.FinancialAdmin
                ],
                DisplayOrder = 7
            },

            #endregion

            #region حواله ها

            new MenuItem {
                Id = "orders",
                Title = "حواله ها",
                IconHtml = MenuIcons.Vouchers,
                Roles = [
                    AppRoles.Admin,
                    AppRoles.ManagingDirector,
                    AppRoles.ExecutiveDirector,
                    AppRoles.SalesExpert,
                    AppRoles.SalesAdmin,
                    AppRoles.TaminExpert,
                    AppRoles.TaminAdmin
                ],
                DisplayOrder = 8,
                SubItems = [
                    new MenuItem {
                        Id = "productionVoucher",
                        Title = "حواله تولید",
                        Url = "/Order/Index",
                    },
                    new MenuItem {
                        Id = "warehouseVoucher",
                        Title = "حواله انبار",
                        Url = "/Warehouse/Voucher",
                    }
                ]
            },

            #endregion

            #region گزارشات

            new MenuItem {
                Id = "reports",
                Title = "گزارشات",
                IconHtml = MenuIcons.Reports,
                Roles = [
                    AppRoles.Admin,
                    AppRoles.ManagingDirector,
                    AppRoles.ExecutiveDirector,
                    AppRoles.FinancialExpert,
                    AppRoles.FinancialAdmin
                ],
                DisplayOrder = 9,
                SubItems = [
                    new MenuItem {
                        Id = "profit",
                        Title = "بهای تمام شده",
                        Url = "/Supply/Profit",
                        DisplayOrder = 1
                    },
                    new MenuItem {
                        Id = "demand",
                        Title = "مطالبات",
                        Url = "/Supply/Demand",
                        DisplayOrder = 2
                    }
                ]
            },

            #endregion

            #region خزانه

            new MenuItem {
                Id = "treasury",
                Title = "خزانه",
                IconHtml = MenuIcons.Treasury,
                Roles = [
                    AppRoles.Admin,
                    AppRoles.ManagingDirector,
                    AppRoles.ExecutiveDirector,
                    AppRoles.TaminExpert,
                    AppRoles.TaminAdmin,
                    AppRoles.FinancialExpert,
                    AppRoles.FinancialAdmin,
                    AppRoles.TanKhah
                ],
                DisplayOrder = 10,
                SubItems = [
                    #region ثبت فاکتور تامین

                    new MenuItem {
                        Id = "supplyFactor",
                        Title = "ثبت فاکتور تامین",
                        Url = "/Supply/SupplyFactor",
                        DisplayOrder = 1,
                        Roles = [
                            AppRoles.Admin,
                            AppRoles.ManagingDirector,
                            AppRoles.ExecutiveDirector,
                            AppRoles.TaminExpert,
                            AppRoles.TaminAdmin,
                            AppRoles.FinancialExpert,
                            AppRoles.FinancialAdmin
                        ]
                    },

                    #endregion

                    #region ثبت درخواست وجه

                    new MenuItem {
                        Id = "requestAmount",
                        Title = "ثبت درخواست وجه",
                        Url = "/RequestAmount/Index",
                        DisplayOrder = 2
                    },

                    #endregion

                    #region چک ها

                    new MenuItem {
                        Id = "check",
                        Title = "چک ها",
                        DisplayOrder = 3,
                        Roles = [
                            AppRoles.Admin,
                            AppRoles.ManagingDirector,
                            AppRoles.ExecutiveDirector,
                            AppRoles.FinancialExpert,
                            AppRoles.FinancialAdmin
                        ],
                        SubItems = [
                            new MenuItem {
                                Id = "receivedChecks",
                                Title = "چک‌های دریافتی",
                                Url = "/Check/Received"
                            },

                            new MenuItem {
                                Id = "issuedChecks",
                                Title = "صدور چک",
                                Url = "/Check/Index"
                            }
                        ],
                    },

                    #endregion

                    #region نقل و انتقال

                    new MenuItem {
                        Id = "transfer",
                        Title = "نقل و انتقال",
                        Url = "/Transfer/Index",
                        DisplayOrder = 4,
                        Roles = [
                            AppRoles.Admin,
                            AppRoles.ManagingDirector,
                            AppRoles.ExecutiveDirector,
                            AppRoles.FinancialExpert,
                            AppRoles.FinancialAdmin
                        ]
                    },

                    #endregion

                    #region انبار

                    new MenuItem {
                        Id = "warehouse",
                        Title = "انبار",
                        Url = "/Warehouse/Index",
                        DisplayOrder = 5,
                        Roles = [
                            AppRoles.Admin,
                            AppRoles.ManagingDirector,
                            AppRoles.ExecutiveDirector,
                            AppRoles.TaminExpert,
                            AppRoles.TaminAdmin,
                            AppRoles.FinancialExpert,
                            AppRoles.FinancialAdmin,
                        ],
                    },

                    #endregion

                    #region ثبت پرداختی

                    /*new MenuItem {
                        Id = "payment",
                        Title = "ثبت پرداختی",
                        Url = "/Supply/Payment",
                        DisplayOrder = 5,
                        Roles = [
                            AppRoles.Admin,
                            AppRoles.ManagingDirector,
                            AppRoles.ExecutiveDirector,
                            AppRoles.FinancialExpert,
                            AppRoles.FinancialAdmin
                        ]
                    }*/

                    #endregion
                ]
            },

            #endregion

            #region قیمت ها

            new MenuItem {
                Id = "amount",
                Title = "قیمت ها",
                Url = "/Amount/Index",
                IconHtml = MenuIcons.Prices,
                Roles = [
                    AppRoles.Admin,
                    AppRoles.TaminAdmin,
                    AppRoles.TaminExpert,
                    AppRoles.SalesAdmin
                ],
                DisplayOrder = 11
            },

            #endregion

            #region تنخواه

            new MenuItem {
                Id = "cash",
                Title = "تنخواه",
                IconHtml = MenuIcons.Cash,
                Roles = [
                    AppRoles.Admin,
                    AppRoles.ManagingDirector,
                    AppRoles.FinancialAdmin,
                    AppRoles.FinancialExpert,
                    AppRoles.TanKhah
                ],
                DisplayOrder = 12,
                SubItems = [
                    new MenuItem {
                        Id = "cashier",
                        Title = "ثبت تنخواه دار",
                        Url = "/Cash/Cashier",
                        DisplayOrder = 1,
                        Roles = [
                            AppRoles.Admin,
                            AppRoles.FinancialAdmin,
                            AppRoles.FinancialExpert
                        ]
                    },
                    new MenuItem {
                        Id = "cashIndex",
                        Title = "ثبت تنخواه",
                        Url = "/Cash/Index",
                        DisplayOrder = 2
                    }
                ]
            },

            #endregion

            #region تنظیمات

            new MenuItem {
                Id = "settings",
                Title = "تنظیمات",
                IconHtml = MenuIcons.Settings,
                OnClick = "showModalSetting()",
                Roles = [
                    AppRoles.Admin,
                    AppRoles.ManagingDirector,
                    AppRoles.ExecutiveDirector,
                    AppRoles.TaminAdmin,
                    AppRoles.SalesAdmin,
                    AppRoles.FinancialAdmin,
                    AppRoles.FinancialExpert
                ],
                DisplayOrder = 13
            },

            #endregion

            #region سفارشات باز معوقه

            new MenuItem {
                Id = "overdueOpenDeals",
                Title = "سفارشات باز معوقه",
                Url = "/OverdueDeal/Index",
                ClassName = "blink-button",
                IsStandalone = false,
                IconHtml = MenuIcons.OverdueDeals,
                Roles = [
                    AppRoles.Admin,
                    AppRoles.SalesAdmin,
                    AppRoles.ManagingDirector,
                    AppRoles.SalesExpert
                ],
                GetBadgeCountAsync = async sp => {
                    var db = sp.GetRequiredService<CRMContextDb>();
                    var now = DateTime.Now;
                    var overdueBefore = now.AddDays(-OverdueDealRules.OverdueAfterDays);
                    var restrictToOwnDeals = user.IsInRole(AppRoles.SalesExpert) &&
                        !user.IsInRole(AppRoles.SalesAdmin) && !user.IsInRole(AppRoles.Admin) &&
                        !user.IsInRole(AppRoles.ManagingDirector);
                    var creatorId = user.FindFirstValue(ClaimTypes.NameIdentifier);

                    return await db.Deals
                        .AsNoTracking()
                        .CountAsync(deal =>
                            deal.Status == DealStatusEnum.باز &&
                            deal.CreateDate >= OverdueDealRules.StartDate &&
                            deal.CreateDate <= overdueBefore &&
                            (!deal.OverdueFollowUpAt.HasValue || deal.OverdueFollowUpAt <= now) &&
                            (!restrictToOwnDeals || deal.UserCreatorID == creatorId));
                },
                HideWhenBadgeEmpty = false,
                DisplayOrder = 998
            },

            #endregion

            #region هشدارها

            new MenuItem {
                Id = "warnings",
                Title = "هشدارها",
                ElementId = "warningsMenuId",
                OnClick = "showModalWarnings()",
                ClassName = "warning",
                IsStandalone = true,
                HideWhenBadgeEmpty = true,
                GetBadgeCountAsync = async sp => {
                    var service = sp.GetRequiredService<Service>();

                    var warnings = await service!.GetAllWarnings(user);
                    return warnings?.Count ?? 0;
                },
                IconHtml = MenuIcons.Warnings,
                Roles = [
                    AppRoles.Admin,
                    AppRoles.FinancialAdmin,
                    AppRoles.FinancialExpert
                ],
                DisplayOrder = 999
            },
            new MenuItem {
                Id = "releaseNotes",
                Title = "نسخه",
                OnClick = "showReleaseNotesHistory()",
                ClassName = "menu-release-notes",
                IconHtml = "",
                IsStandalone = true,
                DisplayOrder = 1000
            }

            #endregion
        ];
    }


    // Keep page definitions and role assignments in BuildMenus. Only their
    // navigation hierarchy changes here; no endpoint or permission is expanded.
    private async Task AttachFavoritesAsync(List<MenuItem> menus, ClaimsPrincipal user)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) return;

        var favorites = await serviceProvider.GetRequiredService<UserContextDb>().MenuFavorites
            .AsNoTracking()
            .Where(f => f.UserId == userId)
            .OrderBy(f => f.SortOrder).ThenBy(f => f.MenuId)
            .ToListAsync();

        var links = new Dictionary<string, (MenuItem Item, string Icon)>(StringComparer.Ordinal);
        void Collect(IEnumerable<MenuItem> nodes, string parentIcon = "")
        {
            foreach (var item in nodes)
            {
                if (!item.ShowInDesktop) continue;
                if (!string.IsNullOrEmpty(item.Url) && !links.ContainsKey(item.Id))
                {
                    item.FavoriteTargetId = item.Id;
                    links[item.Id] = (item, string.IsNullOrEmpty(item.IconHtml) ? parentIcon : item.IconHtml);
                }
                Collect(item.SubItems, string.IsNullOrEmpty(item.IconHtml) ? parentIcon : item.IconHtml);
            }
        }
        Collect(menus);
        var favoriteIds = favorites.Select(f => f.MenuId).ToHashSet(StringComparer.Ordinal);
        foreach (var entry in links.Values) entry.Item.IsFavorite = favoriteIds.Contains(entry.Item.Id);

        var visible = favorites.Where(f => links.ContainsKey(f.MenuId))
            .Select(f =>
            {
                var (source, icon) = links[f.MenuId];
                return new MenuItem
                {
                    Id = "favorite-" + source.Id,
                    FavoriteTargetId = source.Id,
                    Title = source.Title,
                    Url = source.Url,
                    IconHtml = icon,
                    IsFavorite = true,
                    DisplayOrder = f.SortOrder
                };
            }).ToList();
        if (visible.Count == 0) return;

        menus.Add(new MenuItem
        {
            Id = "favorites", Title = "علاقه‌مندی‌ها",
            IconHtml = MenuIcons.Tasks, DisplayOrder = 6,
            IsFavoritesGroup = true, SubItems = visible
        });
        menus.Sort((a, b) => a.DisplayOrder.CompareTo(b.DisplayOrder));
    }

    private static List<MenuItem> OrganizeMenus(List<MenuItem> definitions)
    {
        var source = definitions.ToList();

        MenuItem Take(string id)
        {
            var item = source.Single(x => x.Id == id);
            source.Remove(item);
            return item;
        }

        MenuItem Child(MenuItem parent, string id)
        {
            var child = parent.SubItems.Single(x => x.Id == id);
            // Old children without Roles inherited their previous parent.
            // Flattening the menu must not silently grant additional access.
            child.Roles = child.Roles.Count == 0
                ? parent.Roles.ToList()
                : child.Roles.Intersect(parent.Roles).ToList();
            return child;
        }

        MenuItem Category(string id, string title, string icon, int order, params MenuItem[] children)
        {
            for (var i = 0; i < children.Length; i++)
                children[i].DisplayOrder = i + 1;
            return new MenuItem
            {
                Id = id, Title = title, IconHtml = icon, DisplayOrder = order,
                SubItems = children.ToList()
            };
        }

        MenuItem MobileShortcut(string id, MenuItem original, string title, string url, int order) =>
            new MenuItem
            {
                Id = id, Title = title, Url = url, IconHtml = original.IconHtml,
                Roles = original.Roles.ToList(), DisplayOrder = order,
                ShowInMobileBottomMenu = true, ShowInDesktop = false
            };

        var dashboard = Take("dashboard");
        dashboard.DisplayOrder = 1;
        var cartable = Take("cartable");
        cartable.DisplayOrder = 4;
        var deals = Take("deal");
        var people = Take("customer");
        var tasks = Take("carizo");
        var workReport = Take("report");
        var accounting = Take("accountancy");
        var vouchers = Take("orders");
        var reports = Take("reports");
        var treasury = Take("treasury");
        var prices = Take("amount");
        var cash = Take("cash");
        var quality = Take("qualityControl");
        var settings = Take("settings");
        var overdue = Take("overdueOpenDeals");
        var warnings = Take("warnings");
        var version = Take("releaseNotes");

        people.Title = "مشتریان";
        people.Url = "/Customer/Index?roleType=1";
        people.ShowInMobileBottomMenu = false;
        deals.ShowInMobileBottomMenu = false;
        workReport.ShowInMobileBottomMenu = false;

        var suppliers = new MenuItem
        {
            Id = "suppliers", Title = "تأمین‌کنندگان",
            Url = "/Customer/Index?roleType=2",
            Roles = people.Roles.ToList()
        };

        var categoryDaily = Category("categoryDaily", "فعالیت‌های روزانه", MenuIcons.Tasks, 10,
            tasks, workReport);
        var categorySales = Category("categorySales", "فروش", MenuIcons.Deals, 20,
            people, deals);
        var categorySupply = Category("categorySupply", "تأمین", MenuIcons.Treasury, 30,
            suppliers, prices, Child(treasury, "supplyFactor"), Child(treasury, "requestAmount"));
        var categoryProduction = Category("categoryProduction", "تولید، انبار و کیفیت", MenuIcons.Vouchers, 40,
            Child(vouchers, "productionVoucher"), Child(vouchers, "warehouseVoucher"),
            Child(treasury, "warehouse"), quality);
        var checks = Child(treasury, "check");
        var receivedChecks = Child(checks, "receivedChecks");
        var issuedChecks = Child(checks, "issuedChecks");
        var categoryFinance = Category("categoryFinance", "مالی و خزانه‌داری", MenuIcons.Accounting, 50,
            accounting, receivedChecks, issuedChecks, Child(treasury, "transfer"),
            Child(cash, "cashier"), Child(cash, "cashIndex"));
        var categoryReports = Category("categoryReports", "گزارش‌های مدیریتی", MenuIcons.Reports, 60,
            Child(reports, "profit"), Child(reports, "demand"));

        // Only roles that have at least one action in _SettingModal should see it.
        settings.Roles = [
            AppRoles.Admin, AppRoles.TaminAdmin, AppRoles.SalesAdmin,
            AppRoles.FinancialAdmin, AppRoles.FinancialExpert
        ];
        settings.DisplayOrder = 70;
        // Keep the existing mobile bottom bar shortcuts. The new categorized
        // pages remain reachable from the mobile "More" drawer as well.
        return
        [
            dashboard,
            MobileShortcut("dealMobile", deals, "سفارشات", "/Deal/Index", 2),
            MobileShortcut("peopleMobile", people, "اشخاص", "/Customer/Index", 3),
            cartable,
            MobileShortcut("reportMobile", workReport, "گزارش کار", "/Report/Index", 5),
            categoryDaily, categorySales, categorySupply, categoryProduction,
            categoryFinance, categoryReports, settings, overdue, warnings, version
        ];
    }

    private static List<MenuItem> Filter(List<MenuItem> menus, HashSet<string> userRoles)
    {
        return FilterItems(menus, userRoles, []);
    }

    private static List<MenuItem> FilterItems(List<MenuItem> items, HashSet<string> userRoles, List<string> parentRoles)
    {
        var result = new List<MenuItem>();

        foreach (var item in items ?? [])
        {
            // اگر خودش Role داشته باشد، همان ملاک است
            // اگر نداشته باشد، Role پرنت را به ارث می‌برد
            var effectiveRoles = item.Roles.Count > 0 ? item.Roles : parentRoles;

            var hasAccess = effectiveRoles?.Count == 0 || effectiveRoles.Any(userRoles.Contains);

            if (!hasAccess) continue;

            // Recursive
            var filteredSubItems = FilterItems(item.SubItems, userRoles, effectiveRoles);

            item.SubItems = filteredSubItems.OrderBy(x => x.DisplayOrder).ToList();


            if (string.IsNullOrEmpty(item.Url) && string.IsNullOrEmpty(item.OnClick) && item.SubItems.Count == 0)
            {
                continue;
            }

            result.Add(item);
        }

        return result.OrderBy(x => x.DisplayOrder).ToList();
    }

    private async Task SetBadgeCountsAsync(List<MenuItem> items)
    {
        foreach (var item in items ?? [])
        {
            if (item.GetBadgeCountAsync != null)
            {
                item.BadgeCount = await item.GetBadgeCountAsync(serviceProvider)!;
            }

            if (item.SubItems?.Count > 0)
            {
                await SetBadgeCountsAsync(item.SubItems);
            }
        }

        items?.RemoveAll(x => x.HideWhenBadgeEmpty && x.BadgeCount <= 0);
    }
}
