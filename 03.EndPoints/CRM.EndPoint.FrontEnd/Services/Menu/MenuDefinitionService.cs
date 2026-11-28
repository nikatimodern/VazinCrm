using System;
using System.Collections.Generic;
using System.Security.Claims;
using CRM.Core.ApplicationServices.QualityControls.Services;
using CRM.Core.ApplicationServices.Security;
using CRM.EndPoint.FrontEnd.Composition;
using CRM.EndPoint.FrontEnd.Models.Menu;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace CRM.EndPoint.FrontEnd.Services.Menu;

public sealed class MenuDefinitionService(
    IWebHostEnvironment environment,
    IConfiguration configuration,
    MenuBadgeService badges)
{
    // These audiences also preserve the inherited access of relocated links.
    private static readonly string[] DashboardRoles = [
        AppRoles.Admin, AppRoles.ManagingDirector, AppRoles.ExecutiveDirector,
        AppRoles.SalesExpert, AppRoles.SalesAdmin, AppRoles.TaminExpert, AppRoles.TaminAdmin,
        AppRoles.FinancialExpert, AppRoles.FinancialAdmin
    ];
    private static readonly string[] SalesAndSupplyRoles = [
        AppRoles.Admin, AppRoles.ManagingDirector, AppRoles.ExecutiveDirector,
        AppRoles.SalesExpert, AppRoles.SalesAdmin, AppRoles.TaminExpert, AppRoles.TaminAdmin
    ];
    private static readonly string[] WorkReportRoles = [
        AppRoles.Admin, AppRoles.ManagingDirector, AppRoles.ExecutiveDirector,
        AppRoles.SalesExpert, AppRoles.TaminExpert
    ];
    private static readonly string[] FinanceRoles = [
        AppRoles.Admin, AppRoles.ManagingDirector, AppRoles.ExecutiveDirector,
        AppRoles.FinancialExpert, AppRoles.FinancialAdmin
    ];
    private static readonly string[] TreasuryRoles = [
        AppRoles.Admin, AppRoles.ManagingDirector, AppRoles.ExecutiveDirector,
        AppRoles.TaminExpert, AppRoles.TaminAdmin, AppRoles.FinancialExpert,
        AppRoles.FinancialAdmin, AppRoles.TanKhah
    ];
    private static readonly string[] SupplyFactorRoles = [
        AppRoles.Admin, AppRoles.ManagingDirector, AppRoles.ExecutiveDirector,
        AppRoles.TaminExpert, AppRoles.TaminAdmin, AppRoles.FinancialExpert, AppRoles.FinancialAdmin
    ];
    private static readonly string[] CashRoles = [
        AppRoles.Admin, AppRoles.ManagingDirector, AppRoles.FinancialAdmin,
        AppRoles.FinancialExpert, AppRoles.TanKhah
    ];

    public List<MenuItem> BuildMenus(ClaimsPrincipal user, HashSet<string> roles)
    {
        var showTaxation = roles.Contains(AppRoles.Admin) &&
            TaxationAvailability.IsLocalEnabled(environment, configuration);

        return [
            #region Dashboard and mobile shortcuts
            new MenuItem {
                Id = "dashboard", Title = "داشبورد", Url = "/Dashboard/Index",
                IconHtml = MenuIcons.Dashboard, Roles = [.. DashboardRoles],
                DisplayOrder = 1, ShowInMobileBottomMenu = true
            },
            MobileShortcut("dealMobile", "سفارشات", "/Deal/Index", MenuIcons.Deals, 2, SalesAndSupplyRoles),
            MobileShortcut("peopleMobile", "اشخاص", "/Customer/Index", MenuIcons.Customers, 3, SalesAndSupplyRoles),
            new MenuItem {
                Id = "cartable", Title = "کارتابل", Url = "/Cartable/Index",
                IconHtml = MenuIcons.Cartable, DisplayOrder = 4, ShowInMobileBottomMenu = true,
                Roles = [AppRoles.Admin, AppRoles.ManagingDirector, AppRoles.ExecutiveDirector,
                    AppRoles.SalesExpert, AppRoles.SalesAdmin, AppRoles.TaminExpert,
                    AppRoles.FinancialExpert, AppRoles.FinancialAdmin]
            },
            MobileShortcut("reportMobile", "گزارش کار", "/Report/Index", MenuIcons.WorkReports, 5, WorkReportRoles),
            #endregion

            #region Daily activities
            new MenuItem {
                Id = "categoryDaily", Title = "فعالیت‌های روزانه", IconHtml = MenuIcons.Tasks,
                DisplayOrder = 10, CompactChildOrder = true,
                SubItems = [
                    new MenuItem {
                        Id = "carizo", Title = "کاربان", Url = "/Carizo/Index",
                        IconHtml = MenuIcons.Tasks, Roles = [.. DashboardRoles], DisplayOrder = 1
                    },
                    new MenuItem {
                        Id = "report", Title = "گزارش کار", Url = "/Report/Index",
                        IconHtml = MenuIcons.WorkReports, Roles = [.. WorkReportRoles], DisplayOrder = 2
                    }
                ]
            },
            #endregion

            #region Sales
            new MenuItem {
                Id = "categorySales", Title = "فروش", IconHtml = MenuIcons.Deals,
                DisplayOrder = 20, CompactChildOrder = true,
                SubItems = [
                    new MenuItem {
                        Id = "customer", Title = "مشتریان", Url = "/Customer/Index?roleType=1",
                        IconHtml = MenuIcons.Customers, Roles = [.. SalesAndSupplyRoles], DisplayOrder = 1
                    },
                    new MenuItem {
                        Id = "deal", Title = "سفارشات", Url = "/Deal/Index",
                        IconHtml = MenuIcons.Deals, Roles = [.. SalesAndSupplyRoles], DisplayOrder = 2
                    }
                ]
            },
            #endregion

            #region Supply
            new MenuItem {
                Id = "categorySupply", Title = "تأمین", IconHtml = MenuIcons.Treasury,
                DisplayOrder = 30, CompactChildOrder = true,
                SubItems = [
                    new MenuItem {
                        Id = "suppliers", Title = "تأمین‌کنندگان", Url = "/Customer/Index?roleType=2",
                        Roles = [.. SalesAndSupplyRoles], DisplayOrder = 1
                    },
                    new MenuItem {
                        Id = "amount", Title = "قیمت ها", Url = "/Amount/Index", IconHtml = MenuIcons.Prices,
                        Roles = [AppRoles.Admin, AppRoles.TaminAdmin, AppRoles.TaminExpert, AppRoles.SalesAdmin],
                        DisplayOrder = 2
                    },
                    new MenuItem {
                        Id = "supplyFactor", Title = "ثبت فاکتور تامین", Url = "/Supply/SupplyFactor",
                        Roles = [.. SupplyFactorRoles], RequiredParentRoles = TreasuryRoles, DisplayOrder = 3
                    },
                    new MenuItem {
                        Id = "requestAmount", Title = "ثبت درخواست وجه", Url = "/RequestAmount/Index",
                        RequiredParentRoles = TreasuryRoles, DisplayOrder = 4
                    }
                ]
            },
            #endregion

            #region Production, warehouse and quality
            new MenuItem {
                Id = "categoryProduction", Title = "تولید، انبار و کیفیت", IconHtml = MenuIcons.Vouchers,
                DisplayOrder = 40, CompactChildOrder = true,
                SubItems = [
                    new MenuItem {
                        Id = "productionVoucher", Title = "حواله تولید", Url = "/Order/Index",
                        RequiredParentRoles = SalesAndSupplyRoles, DisplayOrder = 1
                    },
                    new MenuItem {
                        Id = "warehouseVoucher", Title = "حواله انبار", Url = "/Warehouse/Voucher",
                        RequiredParentRoles = SalesAndSupplyRoles, DisplayOrder = 2
                    },
                    new MenuItem {
                        Id = "warehouse", Title = "انبار", Url = "/Warehouse/Index",
                        Roles = [.. SupplyFactorRoles], RequiredParentRoles = TreasuryRoles, DisplayOrder = 3
                    },
                    new MenuItem {
                        Id = "qualityControl", Title = "کنترل کیفیت", Url = "/QualityControl/Index",
                        IconHtml = MenuIcons.QualityControl,
                        Roles = [.. QualityControlAccessPolicy.Roles.Split(',')], DisplayOrder = 4
                    }
                ]
            },
            #endregion

            #region Finance and treasury
            new MenuItem {
                Id = "categoryFinance", Title = "مالی و خزانه‌داری", IconHtml = MenuIcons.Accounting,
                DisplayOrder = 50, CompactChildOrder = true,
                SubItems = [
                    new MenuItem {
                        Id = "accountancy", Title = "حسابداری", Url = "/Accountancy/Index",
                        IconHtml = MenuIcons.Accounting, DisplayOrder = 1,
                        Roles = [AppRoles.Admin, AppRoles.ManagingDirector, AppRoles.ExecutiveDirector,
                            AppRoles.SalesExpert, AppRoles.TaminExpert, AppRoles.FinancialExpert, AppRoles.FinancialAdmin]
                    },
                    new MenuItem {
                        Id = "receivedChecks", Title = "چک‌های دریافتی", Url = "/Check/Received",
                        RequiredParentRoles = FinanceRoles, DisplayOrder = 2
                    },
                    new MenuItem {
                        Id = "issuedChecks", Title = "صدور چک", Url = "/Check/Index",
                        RequiredParentRoles = FinanceRoles, DisplayOrder = 3
                    },
                    new MenuItem {
                        Id = "transfer", Title = "نقل و انتقال", Url = "/Transfer/Index",
                        Roles = [.. FinanceRoles], RequiredParentRoles = TreasuryRoles, DisplayOrder = 4
                    },
                    new MenuItem {
                        Id = "cashier", Title = "ثبت تنخواه دار", Url = "/Cash/Cashier",
                        Roles = [AppRoles.Admin, AppRoles.FinancialAdmin, AppRoles.FinancialExpert],
                        RequiredParentRoles = CashRoles, DisplayOrder = 5
                    },
                    new MenuItem {
                        Id = "cashIndex", Title = "ثبت تنخواه", Url = "/Cash/Index",
                        RequiredParentRoles = CashRoles, DisplayOrder = 6
                    },
                    .. (showTaxation ? new[] {
                        new MenuItem {
                            Id = "taxation", Title = "سامانه مؤدیان", Url = "/Taxation",
                            IconHtml = MenuIcons.Accounting, Roles = [AppRoles.Admin],
                            DisplayOrder = 21, KeepDisplayOrder = true
                        }
                    } : Array.Empty<MenuItem>())
                ]
            },
            #endregion

            #region Management reports
            new MenuItem {
                Id = "categoryReports", Title = "گزارش‌های مدیریتی", IconHtml = MenuIcons.Reports,
                DisplayOrder = 60, CompactChildOrder = true,
                SubItems = [
                    new MenuItem {
                        Id = "profit", Title = "بهای تمام شده", Url = "/Supply/Profit",
                        RequiredParentRoles = FinanceRoles, DisplayOrder = 1
                    },
                    new MenuItem {
                        Id = "demand", Title = "مطالبات", Url = "/Supply/Demand",
                        RequiredParentRoles = FinanceRoles, DisplayOrder = 2
                    }
                ]
            },
            #endregion

            #region Settings
            new MenuItem {
                Id = "settings", Title = "تنظیمات", IconHtml = MenuIcons.Settings,
                OnClick = "showModalSetting()", DisplayOrder = 70,
                Roles = [AppRoles.Admin, AppRoles.ManagingDirector, AppRoles.ExecutiveDirector,
                    AppRoles.TaminAdmin, AppRoles.SalesAdmin, AppRoles.FinancialAdmin, AppRoles.FinancialExpert]
            },
            new MenuItem {
                Id = "extraSettings", Title = "تنظیمات", IconHtml = MenuIcons.Settings,
                DisplayOrder = 80, ShowInDesktop = false, ShowInMobile = false,
                SubItems = [
                    SettingLink("settingsVersions", "مدیریت تغییرات نسخه‌ها", "/ReleaseNotesAdmin/Index", AppRoles.Admin),
                    SettingLink("settingsUsers", "کاربران", "/Admin/Index", AppRoles.Admin),
                    SettingLink("settingsCompany", "اطلاعات پایه شرکت", "/BaseItem/Index",
                        AppRoles.Admin, AppRoles.TaminAdmin, AppRoles.SalesAdmin, AppRoles.FinancialAdmin),
                    SettingLink("settingsFinance", "اطلاعات پایه مالی", "/BaseItem/Financial",
                        AppRoles.Admin, AppRoles.FinancialExpert, AppRoles.FinancialAdmin),
                    SettingLink("settingsMain", "تنظیمات پایه", "/Asetting/Index", AppRoles.Admin),
                    SettingLink("settingsPrefactorTerms", "شرایط قرارداد در پیش فاکتور", "/PrefactorTerms/Index", AppRoles.Admin),
                    SettingLink("settingsBin", "سطل زباله", "/Asetting/RecycleBin", AppRoles.Admin),
                    SettingLink("settingsSms", "مدیریت پیامک", "/Sms/Index", AppRoles.Admin),
                    SettingLink("settingsExternal", "مدیریت ارتباطات", "/ExternalSoftware/Index", AppRoles.Admin)
                ]
            },
            #endregion

            #region Fixed footer actions
            new MenuItem {
                Id = "overdueOpenDeals", Title = "سفارشات باز معوقه", Url = "/OverdueDeal/Index",
                ClassName = "blink-button", IsStandalone = false, IconHtml = MenuIcons.OverdueDeals,
                Roles = [AppRoles.Admin, AppRoles.SalesAdmin, AppRoles.ManagingDirector, AppRoles.SalesExpert],
                GetBadgeCountAsync = _ => badges.GetOverdueCountAsync(user),
                HideWhenBadgeEmpty = false, DisplayOrder = 998
            },
            new MenuItem {
                Id = "warnings", Title = "هشدارها", ElementId = "warningsMenuId",
                OnClick = "showModalWarnings()", ClassName = "warning", IsStandalone = true,
                HideWhenBadgeEmpty = true, GetBadgeCountAsync = _ => badges.GetWarningCountAsync(user),
                IconHtml = MenuIcons.Warnings,
                Roles = [AppRoles.Admin, AppRoles.FinancialAdmin, AppRoles.FinancialExpert], DisplayOrder = 999
            },
            new MenuItem {
                Id = "releaseNotes", Title = "نسخه", OnClick = "showReleaseNotesHistory()",
                ClassName = "menu-release-notes", IconHtml = "", IsStandalone = true, DisplayOrder = 1000
            }
            #endregion
        ];
    }

    private static MenuItem MobileShortcut(string id, string title, string url, string icon,
        int order, string[] roles) => new() {
        Id = id, Title = title, Url = url, IconHtml = icon, Roles = [.. roles], DisplayOrder = order,
        ShowInMobileBottomMenu = true, ShowInDesktop = false
    };

    private static MenuItem SettingLink(string id, string title, string url, params string[] roles) => new() {
        Id = id, Title = title, Url = url, Roles = [.. roles]
    };
}
