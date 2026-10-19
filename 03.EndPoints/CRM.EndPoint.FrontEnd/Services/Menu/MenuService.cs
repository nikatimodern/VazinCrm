using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using CRM.Core.Domain.Deals.Enums;
using CRM.EndPoint.FrontEnd.IServices.Menu;
using CRM.EndPoint.FrontEnd.Models.Menu;
using CRM.Infrastructures.Sql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

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
                IconHtml =
                    """<svg class="menu-icon" viewBox="0 0 24 24" fill="currentColor"><path d="M11.47 3.841a.75.75 0 0 1 1.06 0l8.69 8.69a.75.75 0 1 0 1.06-1.061l-8.689-8.69a2.25 2.25 0 0 0-3.182 0l-8.69 8.69a.75.75 0 1 0 1.061 1.06l8.69-8.689Z"/><path d="m12 5.432 8.159 8.159c.03.03.06.058.091.086v6.198c0 1.035-.84 1.875-1.875 1.875H15a.75.75 0 0 1-.75-.75v-4.5a.75.75 0 0 0-.75-.75h-3a.75.75 0 0 0-.75.75V21a.75.75 0 0 1-.75.75H5.625a1.875 1.875 0 0 1-1.875-1.875v-6.198a2.29 2.29 0 0 0 .091-.086L12 5.432Z"/></svg>""",
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
                IconHtml =
                    """<svg class="menu-icon" viewBox="0 0 384 512"><path fill="currentColor" d="M224 136V0H24C10.7 0 0 10.7 0 24v464c0 13.3 10.7 24 24 24h336c13.3 0 24-10.7 24-24V160H248c-13.2 0-24-10.8-24-24zM64 72c0-4.42 3.58-8 8-8h80c4.42 0 8 3.58 8 8v16c0 4.42-3.58 8-8 8H72c-4.42 0-8-3.58-8-8V72zm0 64c0-4.42 3.58-8 8-8h80c4.42 0 8 3.58 8 8v16c0 4.42-3.58 8-8 8H72c-4.42 0-8-3.58-8-8v-16zm192.81 248H304c8.84 0 16 7.16 16 16s-7.16 16-16 16h-47.19c-16.45 0-31.27-9.14-38.64-23.86-2.95-5.92-8.09-6.52-10.17-6.52s-7.22.59-10.02 6.19l-7.67 15.34a15.986 15.986 0 0 1-14.31 8.84c-.38 0-.75-.02-1.14-.05-6.45-.45-12-4.75-14.03-10.89L144 354.59l-10.61 31.88c-5.89 17.66-22.38 29.53-41 29.53H80c-8.84 0-16-7.16-16-16s7.16-16 16-16h12.39c4.83 0 9.11-3.08 10.64-7.66l18.19-54.64c3.3-9.81 12.44-16.41 22.78-16.41s19.48 6.59 22.77 16.41l13.88 41.64c19.77-16.19 54.05-9.7 66 14.16 2.02 4.06 5.96 6.5 10.16 6.5zM377 105L279.1 7c-4.5-4.5-10.6-7-17-7H256v128h128v-6.1c0-6.3-2.5-12.4-7-16.9z"></path></svg>""",
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

            #region اشخاص

            new MenuItem {
                Id = "customer",
                Title = "اشخاص",
                Url = "/Customer/Index",
                IconHtml =
                    """<svg class="menu-icon" viewBox="0 0 640 512"><path fill="currentColor" d="M96 224c35.3 0 64-28.7 64-64s-28.7-64-64-64-64 28.7-64 64 28.7 64 64 64zm448 0c35.3 0 64-28.7 64-64s-28.7-64-64-64-64 28.7-64 64 28.7 64 64 64zm32 32h-64c-17.6 0-33.5 7.1-45.1 18.6 40.3 22.1 68.9 62 75.1 109.4h66c17.7 0 32-14.3 32-32v-32c0-35.3-28.7-64-64-64zm-256 0c61.9 0 112-50.1 112-112S381.9 32 320 32 208 82.1 208 144s50.1 112 112 112zm76.8 32h-8.3c-20.8 10-43.9 16-68.5 16s-47.6-6-68.5-16h-8.3C179.6 288 128 339.6 128 403.2V432c0 26.5 21.5 48 48 48h288c26.5 0 48-21.5 48-48v-28.8c0-63.6-51.6-115.2-115.2-115.2zm-223.7-13.4C161.5 263.1 145.6 256 128 256H64c-35.3 0-64 28.7-64 64v32c0 17.7 14.3 32 32 32h65.9c6.3-47.4 34.9-87.3 75.2-109.4z"></path></svg>""",
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
                IconHtml =
                    """<svg class="menu-icon" viewBox="0 0 448 512"><path fill="currentColor" d="M448 360V24c0-13.3-10.7-24-24-24H96C43 0 0 43 0 96v320c0 53 43 96 96 96h328c13.3 0 24-10.7 24-24v-16c0-7.5-3.5-14.3-8.9-18.7-4.2-15.4-4.2-59.3 0-74.7 5.4-4.3 8.9-11.1 8.9-18.6zM128 134c0-3.3 2.7-6 6-6h212c3.3 0 6 2.7 6 6v20c0 3.3-2.7 6-6 6H134c-3.3 0-6-2.7-6-6v-20zm0 64c0-3.3 2.7-6 6-6h212c3.3 0 6 2.7 6 6v20c0 3.3-2.7 6-6 6H134c-3.3 0-6-2.7-6-6v-20zm253.4 250H96c-17.7 0-32-14.3-32-32 0-17.6 14.4-32 32-32h285.4c-1.9 17.1-1.9 46.9 0 64z"></path></svg>""",
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
                IconHtml =
                    """<svg class="menu-icon" viewBox="0 0 512 512"><path fill="currentColor" d="M139.61 35.5a12 12 0 0 0-17 0L58.93 98.81l-22.7-22.12a12 12 0 0 0-17 0L3.53 92.41a12 12 0 0 0 0 17l47.59 47.4a12.78 12.78 0 0 0 17.61 0l15.59-15.62L156.52 69a12.09 12.09 0 0 0 .09-17zm0 159.19a12 12 0 0 0-17 0l-63.68 63.72-22.7-22.1a12 12 0 0 0-17 0L3.53 252a12 12 0 0 0 0 17L51 316.5a12.77 12.77 0 0 0 17.6 0l15.7-15.69 72.2-72.22a12 12 0 0 0 .09-16.9zM64 368c-26.49 0-48.59 21.5-48.59 48S37.53 464 64 464a48 48 0 0 0 0-96zm432 16H208a16 16 0 0 0-16 16v32a16 16 0 0 0 16 16h288a16 16 0 0 0 16-16v-32a16 16 0 0 0-16-16zm0-320H208a16 16 0 0 0-16 16v32a16 16 0 0 0 16 16h288a16 16 0 0 0 16-16V80a16 16 0 0 0-16-16zm0 160H208a16 16 0 0 0-16 16v32a16 16 0 0 0 16 16h288a16 16 0 0 0 16-16v-32a16 16 0 0 0-16-16z"></path></svg>""",
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
                IconHtml =
                    """<svg class="menu-icon" viewBox="0 0 448 512"><path fill="currentColor" d="M432 32a16 16 0 0 1 16 16v96a16 16 0 0 1-16 16h-32a16 16 0 0 1-16-16v-32H264v304h40a16 16 0 0 1 16 16v32a16 16 0 0 1-16 16H144a16 16 0 0 1-16-16v-32a16 16 0 0 1 16-16h40V112H64v32a16 16 0 0 1-16 16H16a16 16 0 0 1-16-16V48a16 16 0 0 1 16-16z"></path></svg>""",
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
                IconHtml =
                    """<svg class="menu-icon" viewBox="0 0 24 24" fill="#fff"><path fill-rule="evenodd" d="M3.75 3.375c0-1.036.84-1.875 1.875-1.875H9a3.75 3.75 0 0 1 3.75 3.75v1.875c0 1.036.84 1.875 1.875 1.875H16.5a3.75 3.75 0 0 1 3.75 3.75v7.875c0 1.035-.84 1.875-1.875 1.875H5.625a1.875 1.875 0 0 1-1.875-1.875V3.375Zm10.5 1.875a5.23 5.23 0 0 0-1.279-3.434 9.768 9.768 0 0 1 6.963 6.963A5.23 5.23 0 0 0 16.5 7.5h-1.875a.375.375 0 0 1-.375-.375V5.25ZM12 10.5a.75.75 0 0 1 .75.75v.028a9.727 9.727 0 0 1 1.687.28.75.75 0 1 1-.374 1.452 8.207 8.207 0 0 0-1.313-.226v1.68l.969.332c.67.23 1.281.85 1.281 1.704 0 .158-.007.314-.02.468-.083.931-.83 1.582-1.669 1.695a9.776 9.776 0 0 1-.561.059v.028a.75.75 0 0 1-1.5 0v-.029a9.724 9.724 0 0 1-1.687-.278.75.75 0 0 1 .374-1.453c.425.11.864.186 1.313.226v-1.68l-.968-.332C9.612 14.974 9 14.354 9 13.5c0-.158.007-.314.02-.468.083-.931.831-1.582 1.67-1.694.185-.025.372-.045.56-.06v-.028a.75.75 0 0 1 .75-.75Zm-1.11 2.324c.119-.016.239-.03.36-.04v1.166l-.482-.165c-.208-.072-.268-.211-.268-.285 0-.113.005-.225.015-.336.013-.146.14-.309.374-.34Zm1.86 4.392V16.05l.482.165c.208.072.268.211.268.285 0 .113-.005.225-.015.336-.012.146-.14.309-.374.34-.12.016-.24.03-.361.04Z" clip-rule="evenodd"/></svg>""",
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
                IconHtml =
                    """<svg class="menu-icon" fill="currentColor" viewBox="0 0 256 256"><path d="M216,40H40A16,16,0,0,0,24,56V208a8,8,0,0,0,11.58,7.15L64,200.94l28.42,14.21a8,8,0,0,0,7.16,0L128,200.94l28.42,14.21a8,8,0,0,0,7.16,0L192,200.94l28.42,14.21A8,8,0,0,0,232,208V56A16,16,0,0,0,216,40ZM176,144H80a8,8,0,0,1,0-16h96a8,8,0,0,1,0,16Zm0-32H80a8,8,0,0,1,0-16h96a8,8,0,0,1,0,16Z"></path></svg>""",
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
                IconHtml =
                    """<svg class="menu-icon" viewBox="0 0 24 24" fill="currentColor"><path stroke="none" d="M0 0h24v24H0z" fill="none"/><path d="M17.997 4.17a3 3 0 0 1 2.003 2.83v12a3 3 0 0 1 -3 3h-10a3 3 0 0 1 -3 -3v-12a3 3 0 0 1 2.003 -2.83a4 4 0 0 0 3.997 3.83h4a4 4 0 0 0 3.98 -3.597zm-8.997 7.83a1 1 0 0 0 -1 1v4a1 1 0 0 0 2 0v-4a1 1 0 0 0 -1 -1m3 3a1 1 0 0 0 -1 1v1a1 1 0 0 0 1 1l.117 -.007a1 1 0 0 0 .883 -.993v-1a1 1 0 0 0 -1 -1m3 -1a1 1 0 0 0 -1 1v2a1 1 0 0 0 2 0v-2a1 1 0 0 0 -1 -1m-1 -12a2 2 0 1 1 0 4h-4a2 2 0 1 1 0 -4z"/></svg>""",
                Roles = [
                    AppRoles.Admin,
                    AppRoles.ManagingDirector,
                    AppRoles.ExecutiveDirector,
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
                IconHtml =
                    """<svg class="menu-icon" viewBox="0 0 288 512"><path fill="currentColor" d="M209.2 233.4l-108-31.6C88.7 198.2 80 186.5 80 173.5c0-16.3 13.2-29.5 29.5-29.5h66.3c12.2 0 24.2 3.7 34.2 10.5 6.1 4.1 14.3 3.1 19.5-2l34.8-34c7.1-6.9 6.1-18.4-1.8-24.5C238 74.8 207.4 64.1 176 64V16c0-8.8-7.2-16-16-16h-32c-8.8 0-16 7.2-16 16v48h-2.5C45.8 64-5.4 118.7.5 183.6c4.2 46.1 39.4 83.6 83.8 96.6l102.5 30c12.5 3.7 21.2 15.3 21.2 28.3 0 16.3-13.2 29.5-29.5 29.5h-66.3C100 368 88 364.3 78 357.5c-6.1-4.1-14.3-3.1-19.5 2l-34.8 34c-7.1 6.9-6.1 18.4 1.8 24.5 24.5 19.2 55.1 29.9 86.5 30v48c0 8.8 7.2 16 16 16h32c8.8 0 16-7.2 16-16v-48.2c46.6-.9 90.3-28.6 105.7-72.7 21.5-61.6-14.6-124.8-72.5-141.7z"></path></svg>""",
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
                IconHtml =
                    """<svg class="menu-icon" viewBox="0 0 24 24" fill="currentColor"><path d="M12 7.5a2.25 2.25 0 1 0 0 4.5 2.25 2.25 0 0 0 0-4.5Z"/><path fill-rule="evenodd" d="M1.5 4.875C1.5 3.839 2.34 3 3.375 3h17.25c1.035 0 1.875.84 1.875 1.875v9.75c0 1.036-.84 1.875-1.875 1.875H3.375A1.875 1.875 0 0 1 1.5 14.625v-9.75ZM8.25 9.75a3.75 3.75 0 1 1 7.5 0 3.75 3.75 0 0 1-7.5 0ZM18.75 9a.75.75 0 0 0-.75.75v.008c0 .414.336.75.75.75h.008a.75.75 0 0 0 .75-.75V9.75a.75.75 0 0 0-.75-.75h-.008ZM4.5 9.75A.75.75 0 0 1 5.25 9h.008a.75.75 0 0 1 .75.75v.008a.75.75 0 0 1-.75.75H5.25a.75.75 0 0 1-.75-.75V9.75Z" clip-rule="evenodd"/><path d="M2.25 18a.75.75 0 0 0 0 1.5c5.4 0 10.63.722 15.6 2.075 1.19.324 2.4-.558 2.4-1.82V18.75a.75.75 0 0 0-.75-.75H2.25Z"/></svg>""",
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
                IconHtml =
                    """<svg class="menu-icon" viewBox="0 0 512 512"><path fill="currentColor" d="M487.4 315.7l-42.6-24.6c4.3-23.2 4.3-47 0-70.2l42.6-24.6c4.9-2.8 7.1-8.6 5.5-14-11.1-35.6-30-67.8-54.7-94.6-3.8-4.1-10-5.1-14.8-2.3L380.8 110c-17.9-15.4-38.5-27.3-60.8-35.1V25.8c0-5.6-3.9-10.5-9.4-11.7-36.7-8.2-74.3-7.8-109.2 0-5.5 1.2-9.4 6.1-9.4 11.7V75c-22.2 7.9-42.8 19.8-60.8 35.1L88.7 85.5c-4.9-2.8-11-1.9-14.8 2.3-24.7 26.7-43.6 58.9-54.7 94.6-1.7 5.4.6 11.2 5.5 14L67.3 221c-4.3 23.2-4.3 47 0 70.2l-42.6 24.6c-4.9 2.8-7.1 8.6-5.5 14 11.1 35.6 30 67.8 54.7 94.6 3.8 4.1 10 5.1 14.8 2.3l42.6-24.6c17.9 15.4 38.5 27.3 60.8 35.1v49.2c0 5.6 3.9 10.5 9.4 11.7 36.7 8.2 74.3 7.8 109.2 0 5.5-1.2 9.4-6.1 9.4-11.7v-49.2c22.2-7.9 42.8-19.8 60.8-35.1l42.6 24.6c4.9 2.8 11 1.9 14.8-2.3 24.7-26.7 43.6-58.9 54.7-94.6 1.5-5.5-.7-11.3-5.6-14.1zM256 336c-44.1 0-80-35.9-80-80s35.9-80 80-80 80 35.9 80 80-35.9 80-80 80z"></path></svg>""",
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
                IsStandalone = true,
                IconHtml =
                    """<svg class="menu-icon" viewBox="0 0 24 24" fill="currentColor"><path fill-rule="evenodd" d="M12 2.25a9.75 9.75 0 1 0 0 19.5 9.75 9.75 0 0 0 0-19.5ZM12 6a.75.75 0 0 1 .75.75v5.1l3.03 1.82a.75.75 0 1 1-.772 1.286l-3.375-2.025A.75.75 0 0 1 11.25 12V6.75A.75.75 0 0 1 12 6Z" clip-rule="evenodd"/></svg>""",
                Roles = [
                    AppRoles.Admin,
                    AppRoles.SalesAdmin,
                    AppRoles.ManagingDirector
                ],
                GetBadgeCountAsync = async sp => {
                    var db = sp.GetRequiredService<CRMContextDb>();
                    var overdueBefore = DateTime.Now.AddDays(-14);

                    return await db.Deals
                        .AsNoTracking()
                        .CountAsync(deal =>
                            deal.Status == DealStatusEnum.باز &&
                            deal.CreateDate <= overdueBefore);
                },
                HideWhenBadgeEmpty = true,
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
                IconHtml =
                    """<svg class="menu-icon" viewBox="0 0 24 24" fill="currentColor"><path fill-rule="evenodd" d="M9.401 3.003c1.155-2 4.043-2 5.197 0l7.355 12.748c1.154 2-.29 4.5-2.599 4.5H4.645c-2.309 0-3.752-2.5-2.598-4.5L9.4 3.003ZM12 8.25a.75.75 0 0 1 .75.75v3.75a.75.75 0 0 1-1.5 0V9a.75.75 0 0 1 .75-.75Zm0 8.25a.75.75 0 1 0 0-1.5.75.75 0 0 0 0 1.5Z" clip-rule="evenodd"/></svg>""",
                Roles = [
                    AppRoles.Admin,
                    AppRoles.FinancialAdmin,
                    AppRoles.FinancialExpert
                ],
                DisplayOrder = 999
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