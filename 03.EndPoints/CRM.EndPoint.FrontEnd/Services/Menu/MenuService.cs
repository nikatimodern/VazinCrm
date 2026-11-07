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
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using CRM.Core.ApplicationServices.Security;
using CRM.Core.Domain.ReleaseNotes.Repositories;

namespace CRM.EndPoint.FrontEnd.Services.Menu;

public class MenuService(IServiceProvider serviceProvider) : IMenuService {
    public async Task<List<MenuItem>> GetMenusAsync(ClaimsPrincipal user)
    {
        var roles = user.Claims
            .Where(x => x.Type == ClaimTypes.Role)
            .Select(x => x.Value)
            .ToHashSet();

        var menus = BuildMenus(user);

        var filteredMenus = Filter(menus, roles);
        var versionMenu = filteredMenus.FirstOrDefault(x => x.Id == "releaseNotes");
        if (versionMenu is not null) {
            var reader = new ReleaseNoteReader(user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "", [.. roles],
                roles.Contains(AppRoles.Customer), roles.Any(role => role != AppRoles.Customer));
            var latest = await serviceProvider.GetRequiredService<IReleaseNoteRepository>()
                .GetLatestVisibleVersionInfoAsync(reader, DateTime.UtcNow, default);
            versionMenu.Title = latest is null ? "نسخه" : $"نسخه {latest.Version}";
            if (latest?.PublishDate is DateTime publishDate)
                versionMenu.Subtitle = CRM.Framework.BaseServices.GetDatePersian(publishDate);
        }

        await SetBadgeCountsAsync(filteredMenus);

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
                        Id = "orders",
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
