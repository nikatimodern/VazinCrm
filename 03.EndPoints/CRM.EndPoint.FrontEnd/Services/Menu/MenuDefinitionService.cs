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

public sealed class MenuDefinitionService(IWebHostEnvironment environment, IConfiguration configuration, MenuBadgeService badges) {
    public List<MenuItem> BuildMenus(ClaimsPrincipal user, HashSet<string> roles)
    {
        var showTaxation = roles.Contains(AppRoles.Admin) && TaxationAvailability.IsLocalEnabled(environment, configuration);

        return [
            #region Dashboard and mobile shortcuts

            new MenuItem {
                Id = "dashboard",
                DisplayType = MenuDisplayType.Link,
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
            new MenuItem {
                Id = "dealMobile",
                DisplayType = MenuDisplayType.Link,
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
                    AppRoles.TaminAdmin
                ],
                DisplayOrder = 2,
                ShowInMobileBottomMenu = true,
                ShowInDesktop = false
            },
            new MenuItem {
                Id = "peopleMobile",
                DisplayType = MenuDisplayType.Link,
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
                ShowInMobileBottomMenu = true,
                ShowInDesktop = false
            },
            new MenuItem {
                Id = "cartable",
                DisplayType = MenuDisplayType.Link,
                Title = "کارتابل",
                Url = "/Cartable/Index",
                IconHtml = MenuIcons.Cartable,
                DisplayOrder = 4,
                ShowInMobileBottomMenu = true,

                Roles = [
                    AppRoles.Admin,
                    AppRoles.ManagingDirector,
                    AppRoles.ExecutiveDirector,
                    AppRoles.SalesExpert,
                    AppRoles.SalesAdmin,
                    AppRoles.TaminExpert,
                    AppRoles.FinancialExpert,
                    AppRoles.FinancialAdmin
                ]
            },

            new MenuItem {
                Id = "reportMobile",
                DisplayType = MenuDisplayType.Link,
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

                DisplayOrder = 5,
                ShowInMobileBottomMenu = true,
                ShowInDesktop = false
            },

            #endregion

            #region Daily activities

            new MenuItem {
                Id = "categoryDaily",
                DisplayType = MenuDisplayType.Group,
                Title = "فعالیت‌های روزانه",
                IconHtml = MenuIcons.Tasks,
                DisplayOrder = 10,
                SubItems = [
                    new MenuItem {
                        Id = "carizo",
                        DisplayType = MenuDisplayType.Link,
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
                        DisplayOrder = 1
                    },

                    new MenuItem {
                        Id = "report",
                        DisplayType = MenuDisplayType.Link,
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
                        DisplayOrder = 2
                    }
                ]
            },

            #endregion

            #region Sales

            new MenuItem {
                Id = "categorySales",
                DisplayType = MenuDisplayType.Group,
                Title = "فروش",
                IconHtml = MenuIcons.Deals,
                DisplayOrder = 20,
                SubItems = [
                    new MenuItem {
                        Id = "customer",
                        DisplayType = MenuDisplayType.Link,
                        Title = "مشتریان",
                        Url = "/Customer/Index?roleType=1",
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
                        DisplayOrder = 1
                    },
                    new MenuItem {
                        Id = "deal",
                        DisplayType = MenuDisplayType.Link,
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
                            AppRoles.TaminAdmin
                        ],
                        DisplayOrder = 2
                    }
                ]
            },

            #endregion

            #region Supply

            new MenuItem {
                Id = "categorySupply",
                DisplayType = MenuDisplayType.Group,
                Title = "تأمین",
                IconHtml = MenuIcons.Treasury,
                DisplayOrder = 30,
                SubItems = [
                    new MenuItem {
                        Id = "suppliers",
                        DisplayType = MenuDisplayType.Link,
                        Title = "تأمین‌کنندگان",
                        Url = "/Customer/Index?roleType=2",
                        Roles = [
                            AppRoles.Admin,
                            AppRoles.ManagingDirector,
                            AppRoles.ExecutiveDirector,
                            AppRoles.SalesExpert,
                            AppRoles.SalesAdmin,
                            AppRoles.TaminExpert,
                            AppRoles.TaminAdmin
                        ],
                        DisplayOrder = 1
                    },

                    new MenuItem {
                        Id = "amount",
                        DisplayType = MenuDisplayType.Link,
                        Title = "قیمت ها",
                        Url = "/Amount/Index",
                        IconHtml = MenuIcons.Prices,
                        Roles = [
                            AppRoles.Admin,
                            AppRoles.TaminAdmin,
                            AppRoles.TaminExpert,
                            AppRoles.SalesAdmin
                        ],
                        DisplayOrder = 2
                    },

                    new MenuItem {
                        Id = "supplyFactor",
                        DisplayType = MenuDisplayType.Link,
                        Title = "ثبت فاکتور تامین",
                        Url = "/Supply/SupplyFactor",
                        Roles = [
                            AppRoles.Admin,
                            AppRoles.ManagingDirector,
                            AppRoles.ExecutiveDirector,
                            AppRoles.TaminExpert,
                            AppRoles.TaminAdmin,
                            AppRoles.FinancialExpert,
                            AppRoles.FinancialAdmin
                        ],
                        DisplayOrder = 3
                    },

                    new MenuItem {
                        Id = "requestAmount",
                        DisplayType = MenuDisplayType.Link,
                        Title = "ثبت درخواست وجه",
                        Url = "/RequestAmount/Index",
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
                        DisplayOrder = 4
                    }
                ]
            },

            #endregion

            #region Production, warehouse and quality

            new MenuItem {
                Id = "categoryProduction",
                DisplayType = MenuDisplayType.Group,
                Title = "تولید، انبار و کیفیت",
                IconHtml = MenuIcons.Vouchers,
                DisplayOrder = 40,
                SubItems = [
                    new MenuItem {
                        Id = "productionVoucher",
                        DisplayType = MenuDisplayType.Link,
                        Title = "حواله تولید",
                        Url = "/Order/Index",
                        Roles = [
                            AppRoles.Admin,
                            AppRoles.ManagingDirector,
                            AppRoles.ExecutiveDirector,
                            AppRoles.SalesExpert,
                            AppRoles.SalesAdmin,
                            AppRoles.TaminExpert,
                            AppRoles.TaminAdmin
                        ],
                        DisplayOrder = 1
                    },
                    new MenuItem {
                        Id = "warehouseVoucher",
                        DisplayType = MenuDisplayType.Link,
                        Title = "حواله انبار",
                        Url = "/Warehouse/Voucher",
                        Roles = [
                            AppRoles.Admin,
                            AppRoles.ManagingDirector,
                            AppRoles.ExecutiveDirector,
                            AppRoles.SalesExpert,
                            AppRoles.SalesAdmin,
                            AppRoles.TaminExpert,
                            AppRoles.TaminAdmin
                        ],
                        DisplayOrder = 2
                    },
                    new MenuItem {
                        Id = "warehouse",
                        DisplayType = MenuDisplayType.Link,
                        Title = "انبار",
                        Url = "/Warehouse/Index",
                        Roles = [
                            AppRoles.Admin,
                            AppRoles.ManagingDirector,
                            AppRoles.ExecutiveDirector,
                            AppRoles.TaminExpert,
                            AppRoles.TaminAdmin,
                            AppRoles.FinancialExpert,
                            AppRoles.FinancialAdmin
                        ],
                        DisplayOrder = 3
                    },
                    new MenuItem {
                        Id = "qualityControl",
                        DisplayType = MenuDisplayType.Link,
                        Title = "کنترل کیفیت",
                        Url = "/QualityControl/Index",
                        IconHtml = MenuIcons.QualityControl,
                        Roles = [.. QualityControlAccessPolicy.Roles.Split(',')],
                        DisplayOrder = 4
                    }
                ]
            },

            #endregion

            #region Finance and treasury

            new MenuItem {
                Id = "categoryFinance",
                DisplayType = MenuDisplayType.Group,
                Title = "مالی و خزانه‌داری",
                IconHtml = MenuIcons.Accounting,
                DisplayOrder = 50,
                SubItems = [
                    new MenuItem {
                        Id = "accountancy",
                        DisplayType = MenuDisplayType.Link,
                        Title = "حسابداری",
                        Url = "/Accountancy/Index",
                        IconHtml = MenuIcons.Accounting,
                        DisplayOrder = 1,
                        Roles = [
                            AppRoles.Admin,
                            AppRoles.ManagingDirector,
                            AppRoles.ExecutiveDirector,
                            AppRoles.SalesExpert,
                            AppRoles.TaminExpert,
                            AppRoles.FinancialExpert,
                            AppRoles.FinancialAdmin
                        ]
                    },
                    new MenuItem {
                        Id = "receivedChecks",
                        DisplayType = MenuDisplayType.Link,
                        Title = "چک‌های دریافتی",
                        Url = "/Check/Received",
                        Roles = [
                            AppRoles.Admin,
                            AppRoles.ManagingDirector,
                            AppRoles.ExecutiveDirector,
                            AppRoles.FinancialExpert,
                            AppRoles.FinancialAdmin
                        ],
                        DisplayOrder = 2
                    },
                    new MenuItem {
                        Id = "issuedChecks",
                        DisplayType = MenuDisplayType.Link,
                        Title = "صدور چک",
                        Url = "/Check/Index",
                        Roles = [
                            AppRoles.Admin,
                            AppRoles.ManagingDirector,
                            AppRoles.ExecutiveDirector,
                            AppRoles.FinancialExpert,
                            AppRoles.FinancialAdmin
                        ],
                        DisplayOrder = 3
                    },
                    new MenuItem {
                        Id = "transfer",
                        DisplayType = MenuDisplayType.Link,
                        Title = "نقل و انتقال",
                        Url = "/Transfer/Index",
                        Roles = [
                            AppRoles.Admin,
                            AppRoles.ManagingDirector,
                            AppRoles.ExecutiveDirector,
                            AppRoles.FinancialExpert,
                            AppRoles.FinancialAdmin
                        ],
                        DisplayOrder = 4
                    },
                    new MenuItem {
                        Id = "cashier",
                        DisplayType = MenuDisplayType.Link,
                        Title = "ثبت تنخواه دار",
                        Url = "/Cash/Cashier",
                        Roles = [
                            AppRoles.Admin,
                            AppRoles.FinancialAdmin,
                            AppRoles.FinancialExpert
                        ],
                        DisplayOrder = 5
                    },
                    new MenuItem {
                        Id = "cashIndex",
                        DisplayType = MenuDisplayType.Link,
                        Title = "ثبت تنخواه",
                        Url = "/Cash/Index",
                        Roles = [
                            AppRoles.Admin,
                            AppRoles.ManagingDirector,
                            AppRoles.FinancialAdmin,
                            AppRoles.FinancialExpert,
                            AppRoles.TanKhah
                        ],
                        DisplayOrder = 6
                    },
                    .. (showTaxation
                        ? new[] {
                            new MenuItem {
                                Id = "taxation",
                                DisplayType = MenuDisplayType.Link,
                                Title = "سامانه مؤدیان",
                                Url = "/Taxation",
                                IconHtml = MenuIcons.Accounting,
                                Roles = [AppRoles.Admin],
                                DisplayOrder = 21
                            }
                        }
                        : Array.Empty<MenuItem>())
                ]
            },

            #endregion

            #region Management reports

            new MenuItem {
                Id = "categoryReports",
                DisplayType = MenuDisplayType.Group,
                Title = "گزارش‌های مدیریتی",
                IconHtml = MenuIcons.Reports,
                DisplayOrder = 60,
                SubItems = [
                    new MenuItem {
                        Id = "profit",
                        DisplayType = MenuDisplayType.Link,
                        Title = "بهای تمام شده",
                        Url = "/Supply/Profit",
                        Roles = [
                            AppRoles.Admin,
                            AppRoles.ManagingDirector,
                            AppRoles.ExecutiveDirector,
                            AppRoles.FinancialExpert,
                            AppRoles.FinancialAdmin
                        ],
                        DisplayOrder = 1
                    },
                    new MenuItem {
                        Id = "demand",
                        DisplayType = MenuDisplayType.Link,
                        Title = "مطالبات",
                        Url = "/Supply/Demand",
                        Roles = [
                            AppRoles.Admin,
                            AppRoles.ManagingDirector,
                            AppRoles.ExecutiveDirector,
                            AppRoles.FinancialExpert,
                            AppRoles.FinancialAdmin
                        ],
                        DisplayOrder = 2
                    }
                ]
            },

            #endregion

            #region Settings

            new MenuItem {
                Id = "settings",
                DisplayType = MenuDisplayType.Modal,
                Title = "تنظیمات",
                IconHtml = MenuIcons.Settings,
                OnClick = "showModalSetting()", DisplayOrder = 70,
                Roles = [
                    AppRoles.Admin,
                    AppRoles.ManagingDirector,
                    AppRoles.ExecutiveDirector,
                    AppRoles.TaminAdmin,
                    AppRoles.SalesAdmin,
                    AppRoles.FinancialAdmin,
                    AppRoles.FinancialExpert
                ],
                SubItems = [
                    new MenuItem {
                        Id = "settingsGroupUsers",
                        DisplayType = MenuDisplayType.Group,
                        Title = "کاربران و نسخه‌ها",
                        DisplayOrder = 1,
                        SubItems = [
                            new MenuItem {
                                Id = "settingsVersions",
                                DisplayType = MenuDisplayType.Link,
                                Title = "مدیریت تغییرات نسخه‌ها",
                                Url = "/ReleaseNotesAdmin/Index",
                                IconHtml = MenuIcons.SettingsModalVersions,
                                Roles = [AppRoles.Admin],
                                DisplayOrder = 1
                            },
                            new MenuItem {
                                Id = "settingsUsers",
                                DisplayType = MenuDisplayType.Link,
                                Title = "کاربران",
                                Url = "/Admin/Index",
                                IconHtml = MenuIcons.SettingsModalUsers,
                                Roles = [AppRoles.Admin],
                                DisplayOrder = 2
                            }
                        ]
                    },
                    new MenuItem {
                        Id = "settingsGroupBase",
                        DisplayType = MenuDisplayType.Group,
                        Title = "اطلاعات پایه",
                        DisplayOrder = 2,
                        SubItems = [
                            new MenuItem {
                                Id = "settingsCompany",
                                DisplayType = MenuDisplayType.Link,
                                Title = "اطلاعات پایه شرکت",
                                Url = "/BaseItem/Index",
                                IconHtml = MenuIcons.SettingsModalCompany,
                                Roles = [AppRoles.Admin, AppRoles.TaminAdmin, AppRoles.SalesAdmin, AppRoles.FinancialAdmin],
                                DisplayOrder = 1
                            },
                            new MenuItem {
                                Id = "settingsFinance",
                                DisplayType = MenuDisplayType.Link,
                                Title = "اطلاعات پایه مالی",
                                Url = "/BaseItem/Financial",
                                IconHtml = MenuIcons.SettingsModalFinance,
                                Roles = [AppRoles.Admin, AppRoles.FinancialExpert, AppRoles.FinancialAdmin],
                                DisplayOrder = 2
                            }
                        ]
                    },
                    new MenuItem {
                        Id = "settingsGroupSystem",
                        DisplayType = MenuDisplayType.Group,
                        Title = "تنظیمات سیستم",
                        DisplayOrder = 3,
                        SubItems = [
                            new MenuItem {
                                Id = "settingsMain",
                                DisplayType = MenuDisplayType.Link,
                                Title = "تنظیمات پایه",
                                Url = "/Asetting/Index",
                                IconHtml = MenuIcons.SettingsModalMain,
                                Roles = [AppRoles.Admin],
                                DisplayOrder = 1
                            },
                            new MenuItem {
                                Id = "settingsPrefactorTerms",
                                DisplayType = MenuDisplayType.Link,
                                Title = "شرایط قرارداد در پیش فاکتور",
                                Url = "/PrefactorTerms/Index",
                                IconHtml = MenuIcons.SettingsModalPrefactorTerms,
                                Roles = [AppRoles.Admin],
                                DisplayOrder = 2
                            },
                            new MenuItem {
                                Id = "settingsBin",
                                DisplayType = MenuDisplayType.Link,
                                Title = "سطل زباله",
                                Url = "/Asetting/RecycleBin",
                                IconHtml = MenuIcons.SettingsModalBin,
                                Roles = [AppRoles.Admin],
                                DisplayOrder = 3
                            }
                        ]
                    },
                    new MenuItem {
                        Id = "settingsGroupCommunications",
                        DisplayType = MenuDisplayType.Group,
                        Title = "ارتباطات",
                        DisplayOrder = 4,
                        SubItems = [
                            new MenuItem {
                                Id = "settingsPhonebook",
                                DisplayType = MenuDisplayType.Modal,
                                Title = "دفترچه تلفن",
                                OnClick = "showModalPhonebook()",
                                IconHtml = MenuIcons.SettingsModalPhonebook,
                                Roles = [AppRoles.Admin, AppRoles.SalesAdmin],
                                DisplayOrder = 1
                            },
                            new MenuItem {
                                Id = "settingsSms",
                                DisplayType = MenuDisplayType.Link,
                                Title = "مدیریت پیامک",
                                Url = "/Sms/Index",
                                IconHtml = MenuIcons.SettingsModalSms,
                                Roles = [AppRoles.Admin],
                                DisplayOrder = 2
                            },
                            new MenuItem {
                                Id = "settingsExternal",
                                DisplayType = MenuDisplayType.Link,
                                Title = "مدیریت ارتباطات",
                                Url = "/ExternalSoftware/Index",
                                IconHtml = MenuIcons.SettingsModalExternal,
                                Roles = [AppRoles.Admin],
                                DisplayOrder = 3
                            }
                        ]
                    }
                ]
            },

            #endregion

            #region Fixed footer actions

            new MenuItem {
                Id = "overdueOpenDeals",
                DisplayType = MenuDisplayType.Link,
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
                GetBadgeCountAsync = _ => badges.GetOverdueCountAsync(user),
                HideWhenBadgeEmpty = false,
                DisplayOrder = 998
            },
            new MenuItem {
                Id = "warnings",
                DisplayType = MenuDisplayType.Modal,
                Title = "هشدارها",
                ElementId = "warningsMenuId",
                OnClick = "showModalWarnings()",
                ClassName = "warning",
                IsStandalone = true,
                HideWhenBadgeEmpty = true,
                GetBadgeCountAsync = _ => badges.GetWarningCountAsync(user),
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
                DisplayType = MenuDisplayType.Modal,
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
}