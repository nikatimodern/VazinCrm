using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using CRM.EndPoint.FrontEnd.Models;

namespace CRM.EndPoint.FrontEnd.Services;

public class MenuService {
    public List<MenuItem> GetMenus(ClaimsPrincipal user)
    {
        var roles = user.Claims
            .Where(x => x.Type == ClaimTypes.Role)
            .Select(x => x.Value)
            .ToHashSet();

        var menus = BuildMenus();

        return Filter(menus, roles);
    }

    private static List<MenuItem> BuildMenus()
    {
        return [
            new MenuItem {
                Id = "dashboard",
                Title = "داشبورد",
                Url = "/Dashboard/Index",
                IconHtml =
                    """<svg class="menuIcon" viewBox="0 0 24 24" fill="currentColor"><path d="M11.47 3.841a.75.75 0 0 1 1.06 0l8.69 8.69a.75.75 0 1 0 1.06-1.061l-8.689-8.69a2.25 2.25 0 0 0-3.182 0l-8.69 8.69a.75.75 0 1 0 1.061 1.06l8.69-8.689Z"/><path d="m12 5.432 8.159 8.159c.03.03.06.058.091.086v6.198c0 1.035-.84 1.875-1.875 1.875H15a.75.75 0 0 1-.75-.75v-4.5a.75.75 0 0 0-.75-.75h-3a.75.75 0 0 0-.75.75V21a.75.75 0 0 1-.75.75H5.625a1.875 1.875 0 0 1-1.875-1.875v-6.198a2.29 2.29 0 0 0 .091-.086L12 5.432Z"/></svg>""",
                Roles = [
                    AppRoles.Admin,
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
                Id = "deal",
                Title = "سفارشات",
                Url = "/Deal/Index",
                IconHtml = """<i class="fas fa-file-contract menuIcon"></i>""",
                Roles = [
                    AppRoles.Admin,
                    AppRoles.SalesExpert,
                    AppRoles.SalesAdmin,
                    AppRoles.TaminExpert,
                    AppRoles.TaminAdmin,
                    AppRoles.FinancialExpert,
                    AppRoles.FinancialAdmin
                ],
                DisplayOrder = 2,
                ShowInMobileBottomMenu = true
            },

            new MenuItem {
                Id = "customer",
                Title = "اشخاص",
                Url = "/Customer/Index",
                IconHtml = """<i class="fas fa-users menuIcon"></i>""",
                Roles = [
                    AppRoles.Admin,
                    AppRoles.SalesExpert,
                    AppRoles.SalesAdmin,
                    AppRoles.TaminExpert,
                    AppRoles.TaminAdmin
                ],
                DisplayOrder = 3,
                ShowInMobileBottomMenu = true
            },

            new MenuItem {
                Id = "cartable",
                Title = "کارتابل",
                Url = "/Cartable/Index",
                IconHtml = """<i class="fa fa-book menuIcon"></i>""",
                Roles = [
                    AppRoles.Admin,
                    AppRoles.SalesExpert,
                    AppRoles.SalesAdmin,
                    AppRoles.TaminExpert,
                    AppRoles.FinancialExpert,
                    AppRoles.FinancialAdmin
                ],
                DisplayOrder = 4,
                ShowInMobileBottomMenu = true
            },

            new MenuItem {
                Id = "carizo",
                Title = "کاریزو",
                Url = "/Carizo/Index",
                IconHtml = """<i class="fa fa-tasks menuIcon"></i>""",
                Roles = [
                    AppRoles.Admin,
                    AppRoles.SalesExpert,
                    AppRoles.SalesAdmin,
                    AppRoles.TaminExpert,
                    AppRoles.TaminAdmin,
                    AppRoles.FinancialExpert,
                    AppRoles.FinancialAdmin
                ],
                DisplayOrder = 5
            },

            new MenuItem {
                Id = "report",
                Title = "گزارش کار",
                Url = "/Report/Index",
                IconHtml = """<i class="fa fa-text menuIcon"></i>""",
                Roles = [
                    AppRoles.Admin,
                    AppRoles.SalesExpert,
                    AppRoles.TaminExpert
                ],
                DisplayOrder = 6,
                ShowInMobileBottomMenu = true
            },

            new MenuItem {
                Id = "accountancy",
                Title = "حسابداری",
                Url = "/Accountancy/Index",
                IconHtml =
                    """<svg style="width: 22px;height: 22px" viewBox="0 0 24 24" fill="#fff"><path fill-rule="evenodd" d="M3.75 3.375c0-1.036.84-1.875 1.875-1.875H9a3.75 3.75 0 0 1 3.75 3.75v1.875c0 1.036.84 1.875 1.875 1.875H16.5a3.75 3.75 0 0 1 3.75 3.75v7.875c0 1.035-.84 1.875-1.875 1.875H5.625a1.875 1.875 0 0 1-1.875-1.875V3.375Zm10.5 1.875a5.23 5.23 0 0 0-1.279-3.434 9.768 9.768 0 0 1 6.963 6.963A5.23 5.23 0 0 0 16.5 7.5h-1.875a.375.375 0 0 1-.375-.375V5.25ZM12 10.5a.75.75 0 0 1 .75.75v.028a9.727 9.727 0 0 1 1.687.28.75.75 0 1 1-.374 1.452 8.207 8.207 0 0 0-1.313-.226v1.68l.969.332c.67.23 1.281.85 1.281 1.704 0 .158-.007.314-.02.468-.083.931-.83 1.582-1.669 1.695a9.776 9.776 0 0 1-.561.059v.028a.75.75 0 0 1-1.5 0v-.029a9.724 9.724 0 0 1-1.687-.278.75.75 0 0 1 .374-1.453c.425.11.864.186 1.313.226v-1.68l-.968-.332C9.612 14.974 9 14.354 9 13.5c0-.158.007-.314.02-.468.083-.931.831-1.582 1.67-1.694.185-.025.372-.045.56-.06v-.028a.75.75 0 0 1 .75-.75Zm-1.11 2.324c.119-.016.239-.03.36-.04v1.166l-.482-.165c-.208-.072-.268-.211-.268-.285 0-.113.005-.225.015-.336.013-.146.14-.309.374-.34Zm1.86 4.392V16.05l.482.165c.208.072.268.211.268.285 0 .113-.005.225-.015.336-.012.146-.14.309-.374.34-.12.016-.24.03-.361.04Z" clip-rule="evenodd"/></svg>""",
                Roles = [
                    AppRoles.Admin,
                    AppRoles.SalesExpert,
                    AppRoles.TaminExpert,
                    AppRoles.FinancialExpert,
                    AppRoles.FinancialAdmin
                ],
                DisplayOrder = 7
            },

            new MenuItem {
                Id = "orders",
                Title = "حواله ها",
                Url = "/Order/Index",
                IconHtml =
                    """<svg style="width: 22px" fill="currentColor" viewBox="0 0 256 256"><path d="M216,40H40A16,16,0,0,0,24,56V208a8,8,0,0,0,11.58,7.15L64,200.94l28.42,14.21a8,8,0,0,0,7.16,0L128,200.94l28.42,14.21a8,8,0,0,0,7.16,0L192,200.94l28.42,14.21A8,8,0,0,0,232,208V56A16,16,0,0,0,216,40ZM176,144H80a8,8,0,0,1,0-16h96a8,8,0,0,1,0,16Zm0-32H80a8,8,0,0,1,0-16h96a8,8,0,0,1,0,16Z"></path></svg>""",
                Roles = [
                    AppRoles.Admin,
                    AppRoles.SalesExpert,
                    AppRoles.SalesAdmin,
                    AppRoles.TaminExpert,
                    AppRoles.TaminAdmin
                ],
                DisplayOrder = 8
            },

            new MenuItem {
                Id = "reports",
                Title = "گزارشات",
                IconHtml =
                    """<svg style="width: 22px" viewBox="0 0 24 24" fill="currentColor"><path stroke="none" d="M0 0h24v24H0z" fill="none"/><path d="M17.997 4.17a3 3 0 0 1 2.003 2.83v12a3 3 0 0 1 -3 3h-10a3 3 0 0 1 -3 -3v-12a3 3 0 0 1 2.003 -2.83a4 4 0 0 0 3.997 3.83h4a4 4 0 0 0 3.98 -3.597zm-8.997 7.83a1 1 0 0 0 -1 1v4a1 1 0 0 0 2 0v-4a1 1 0 0 0 -1 -1m3 3a1 1 0 0 0 -1 1v1a1 1 0 0 0 1 1l.117 -.007a1 1 0 0 0 .883 -.993v-1a1 1 0 0 0 -1 -1m3 -1a1 1 0 0 0 -1 1v2a1 1 0 0 0 2 0v-2a1 1 0 0 0 -1 -1m-1 -12a2 2 0 1 1 0 4h-4a2 2 0 1 1 0 -4z"/></svg>""",
                Roles = [
                    AppRoles.Admin,
                    AppRoles.ExecutiveDirector
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

            new MenuItem {
                Id = "treasury",
                Title = "خزانه",
                IconHtml =
                    """<svg style="width: 18px" fill="currentColor" viewBox="0 0 16 16"><path d="M9.778 9.414A2 2 0 1 1 6.95 6.586a2 2 0 0 1 2.828 2.828"/><path d="M2.5 0A1.5 1.5 0 0 0 1 1.5V3H.5a.5.5 0 0 0 0 1H1v3.5H.5a.5.5 0 0 0 0 1H1V12H.5a.5.5 0 0 0 0 1H1v1.5A1.5 1.5 0 0 0 2.5 16h12a1.5 1.5 0 0 0 1.5-1.5v-13A1.5 1.5 0 0 0 14.5 0zm3.036 4.464 1.09 1.09a3 3 0 0 1 3.476 0l1.09-1.09a.5.5 0 1 1 .707.708l-1.09 1.09c.74 1.037.74 2.44 0 3.476l1.09 1.09a.5.5 0 1 1-.707.708l-1.09-1.09a3 3 0 0 1-3.476 0l-1.09 1.09a.5.5 0 1 1-.708-.708l1.09-1.09a3 3 0 0 1 0-3.476l-1.09-1.09a.5.5 0 1 1 .708-.708M14 6.5v3a.5.5 0 0 1-1 0v-3a.5.5 0 0 1 1 0"/></svg>""",
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
                    new MenuItem {
                        Id = "requestAmount",
                        Title = "ثبت درخواست وجه",
                        Url = "/Amount/RequestAmount",
                        DisplayOrder = 2
                    },
                    new MenuItem {
                        Id = "check",
                        Title = "چک ها",
                        Url = "/Check/Index",
                        DisplayOrder = 3,
                        Roles = [
                            AppRoles.Admin,
                            AppRoles.ManagingDirector,
                            AppRoles.ExecutiveDirector,
                            AppRoles.FinancialExpert,
                            AppRoles.FinancialAdmin
                        ]
                    },
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
                ]
            },

            new MenuItem {
                Id = "amount",
                Title = "قیمت ها",
                Url = "/Amount/Index",
                IconHtml = """<i class="fas fa-dollar-sign menuIcon"></i>""",
                Roles = [
                    AppRoles.Admin,
                    AppRoles.TaminAdmin,
                    AppRoles.TaminExpert,
                    AppRoles.SalesAdmin
                ],
                DisplayOrder = 11
            },

            new MenuItem {
                Id = "cash",
                Title = "تنخواه",
                IconHtml = """<svg style="width: 22px" viewBox="0 0 24 24" fill="currentColor"><path d="M12 7.5a2.25 2.25 0 1 0 0 4.5 2.25 2.25 0 0 0 0-4.5Z"/><path fill-rule="evenodd" d="M1.5 4.875C1.5 3.839 2.34 3 3.375 3h17.25c1.035 0 1.875.84 1.875 1.875v9.75c0 1.036-.84 1.875-1.875 1.875H3.375A1.875 1.875 0 0 1 1.5 14.625v-9.75ZM8.25 9.75a3.75 3.75 0 1 1 7.5 0 3.75 3.75 0 0 1-7.5 0ZM18.75 9a.75.75 0 0 0-.75.75v.008c0 .414.336.75.75.75h.008a.75.75 0 0 0 .75-.75V9.75a.75.75 0 0 0-.75-.75h-.008ZM4.5 9.75A.75.75 0 0 1 5.25 9h.008a.75.75 0 0 1 .75.75v.008a.75.75 0 0 1-.75.75H5.25a.75.75 0 0 1-.75-.75V9.75Z" clip-rule="evenodd"/><path d="M2.25 18a.75.75 0 0 0 0 1.5c5.4 0 10.63.722 15.6 2.075 1.19.324 2.4-.558 2.4-1.82V18.75a.75.75 0 0 0-.75-.75H2.25Z"/></svg>""",
                Roles = [
                    AppRoles.Admin,
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

            new MenuItem {
                Id = "settings",
                Title = "تنظیمات",
                IconHtml = """<i class="fa fa-cog menuIcon"></i>""",
                OnClick = "showModalSetting()",
                Roles = [
                    AppRoles.Admin,
                    AppRoles.TaminAdmin,
                    AppRoles.SalesAdmin,
                    AppRoles.FinancialAdmin,
                    AppRoles.FinancialExpert
                ],
                DisplayOrder = 13
            },

            new MenuItem {
                Id = "warnings",
                Title = "هشدارها",
                ElementId = "warningsMenuId",
                OnClick = "showModalWarnings()",
                className = "warning",
                IsStandalone = true,
                HasBadge = true,
                IconHtml =
                    """<svg style="width: 22px;height: 22px" viewBox="0 0 24 24" fill="currentColor"><path fill-rule="evenodd" d="M9.401 3.003c1.155-2 4.043-2 5.197 0l7.355 12.748c1.154 2-.29 4.5-2.599 4.5H4.645c-2.309 0-3.752-2.5-2.598-4.5L9.4 3.003ZM12 8.25a.75.75 0 0 1 .75.75v3.75a.75.75 0 0 1-1.5 0V9a.75.75 0 0 1 .75-.75Zm0 8.25a.75.75 0 1 0 0-1.5.75.75 0 0 0 0 1.5Z" clip-rule="evenodd"/></svg>""",
                Roles = [
                    AppRoles.Admin,
                    AppRoles.FinancialAdmin,
                    AppRoles.FinancialExpert
                ],
                DisplayOrder = 999
            }
        ];
    }

    private static List<MenuItem> Filter(List<MenuItem> menus, HashSet<string> userRoles)
    {
        foreach (var menu in menus ?? [])
        {
            foreach (var sub in menu?.SubItems ?? [])
            {
                if (sub.Roles.Count == 0)
                    sub.Roles = [..menu.Roles];
            }
        }

        return menus
            .Where(menu => HasAccess(menu, userRoles))
            .Select(menu => {
                menu.SubItems = menu.SubItems
                    .Where(sub => HasAccess(sub, userRoles))
                    .OrderBy(sub => sub.DisplayOrder)
                    .ToList();

                return menu;
            })
            .OrderBy(menu => menu.DisplayOrder)
            .ToList();
    }

    private static bool HasAccess(MenuItem item, HashSet<string> userRoles)
    {
        return userRoles != null && (item.Roles.Count == 0 || item.Roles.Any(userRoles.Contains));
    }

    public static class AppRoles {
        public const string Admin = "مدیر ارشد";

        public const string SalesExpert = "کارشناس فروش";
        public const string SalesAdmin = "مدیر فروش";

        public const string TaminExpert = "کارشناس تامین";
        public const string TaminAdmin = "مدیر تامین";

        public const string FinancialExpert = "کارشناس مالی";
        public const string FinancialAdmin = "مدیر مالی";

        public const string ManagingDirector = "مدیر عامل";

        public const string ExecutiveDirector = "مدیر اجرایی";

        public const string TanKhah = "تنخواه دار";
    }
}