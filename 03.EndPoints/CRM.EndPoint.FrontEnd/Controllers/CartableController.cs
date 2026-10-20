using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.ASettings.Entities;
using CRM.Core.Domain.Factors.Entities;
using CRM.Core.Domain.PreFactors.Entities;
using CRM.Core.Domain.PreFactors.Enum;
using CRM.EndPoint.FrontEnd.Models.Factor;
using CRM.EndPoint.FrontEnd.Services;
using CRM.Infrastructures.Sql;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CRM.Core.Domain.Accountancies.Enum;
using CRM.Core.Domain.Supplies.Enums;
using CRM.EndPoint.FrontEnd.IServices.ViewRender;
using CRM.EndPoint.FrontEnd.Models;
using CRM.EndPoint.FrontEnd.Models.Cartable;
using CRM.EndPoint.FrontEnd.Models.PreFactors;
using CRM.EndPoint.FrontEnd.Services.Extensions;
using CRM.Infrastructures.Sql.Accountancies.QueryExtensions;

// ReSharper disable EntityFramework.UnsupportedServerSideFunctionCall

namespace CRM.EndPoint.FrontEnd.Controllers {
    [Authorize]
    public class CartableController : Controller {
        private readonly Service genService;
        private readonly CRMContextDb db;
        private readonly PreFactorService preFactorService;
        private readonly IViewRenderService vrs;
        private readonly UserManager<AppUser> userManager;
        private readonly ASetting _setting;


        public CartableController(IViewRenderService vrs, UserManager<AppUser> userManager, Service GenService, CRMContextDb db,
            PreFactorService preFactorService)
        {
            this.vrs = vrs;
            this.userManager = userManager;
            genService = GenService;
            this.db = db;
            this.preFactorService = preFactorService;
            this._setting = db.ASettings.FirstOrDefault();
        }

        [Authorize(Policy = "NotCustomerOnly")]
        public IActionResult Index()
        {
            ViewBag.SubTitle = "کارتابل من";
            ViewBag.Head = "Cartable";
            return View();
        }


        [Authorize(Policy = "NotCustomerOnly")]
        [HttpGet]
        public async Task<IActionResult> GetCountEnvelope()
        {
            try
            {
                var user = await userManager.GetUserAsync(HttpContext.User);
                var countObj = await genService.GetCartableCountObj(user);
                var emailsCount = await genService.GetEmailsNotSeenCount(Guid.Parse(user.Id));

                var chatsCount = await db.MainChats
                    .Where(f => f.ToUserId == user.Id || f.FromUserId == user.Id)
                    .SumAsync(f => f.Chats.Count(f => !f.Read && f.UserCreatorID != user.Id));

                var data = new { countObj, emailsCount, chatsCount };
                return Ok(data);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }


        [HttpPost]
        [Authorize(Roles = "کارشناس فروش ,مدیر ارشد ,مدیر مالی, کارشناس مالی,مدیر فروش,کارشناس تامین")]
        public async Task<IActionResult> PreFactorAcceptedPartial([FromBody] FilterVModel model)
        {
            try
            {
                var pList = await GetPrefactors(model);
                if (pList == null)
                    throw new Exception();


                var vModel = new PrefactorViewModel {
                    data = pList,
                    _Setting = _setting,
                };

                var html = await vrs.RenderToStringAsync("PreFactorAcceptedPartial", vModel);

                return Ok(new { count = vModel.data?.Count ?? 0, html });
            }
            catch (Exception)
            {
                return BadRequest();
            }
        }


        [HttpPost]
        [Authorize]
        public async Task<IActionResult> PrefactorExportToExcel([FromBody] FilterVModel model)
        {
            try
            {
                var preFactors = await GetPrefactors(model);


                var columns = new List<ExcelColumnConfig<PreFactor>> {
                    new() { Header = "شماره سریال", ValueSelector = x => x.SerialNumber },
                    new() { Header = "نوع", ValueSelector = x => x.OfficialType == 1 ? "رسمی" : "غیر رسمی" },
                    new() { Header = "تاریخ صدور", ValueSelector = x => TextService.ToEnglishNumber(x.Date) },
                    new() { Header = "تاریخ اعتبار", ValueSelector = x => TextService.ToEnglishNumber(x.ValidityDade) },
                    new() { Header = "جمع کل", ValueSelector = x => x.SumTotalAmount, NumberFormat = "#,##0_-ريال" },
                    new() { Header = "جمع کل با ارزش افزوده", ValueSelector = x => x.PaymentAmount, NumberFormat = "#,##0_-ريال" },
                    new() { Header = "مشتری", ValueSelector = x => $"{x.Customer} {x.AgentName}" },
                    new() { Header = "کارشناس فروش", ValueSelector = x => x.UserCreator },
                    new() { Header = "وضعیت", ValueSelector = x => x.PftypeTxt },
                    new() { Header = "تایید", ValueSelector = x => x.Accepted == true ? "بله" : "خیر" },
                    new() { Header = "صادراتی", ValueSelector = x => x.Export == true ? "بله" : "خیر" }
                };

                return ExcelExportService.ExportToExcel(preFactors, columns, "PreFactors");
            }
            catch
            {
                return null;
            }
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> FactorExportToExcel([FromBody] FactorVModel model)
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            var roles = await userManager.GetRolesAsync(user);


            var factors = await GetFactors(model, user, roles);
            var columns = new List<ExcelColumnConfig<Factor>> {
                new() { Header = "شماره سریال", ValueSelector = x => x.SerialNumber },
                new() { Header = "نوع", ValueSelector = x => x.OfficialType == 1 ? "رسمی" : "غیر رسمی" },
                new() { Header = "تاریخ", ValueSelector = x => TextService.ToEnglishNumber(x.Date) },
                new() { Header = "شماره پیش فاکتور", ValueSelector = x => x.PfactorSerialNumber },
                new() { Header = "جمع کل", ValueSelector = x => x.SumTotalAmount, NumberFormat = "#,##0_-ريال" },
                new() { Header = "جمع کل با ارزش افزوده", ValueSelector = x => x.PaymentAmount, NumberFormat = "#,##0_-ريال" },
                new() { Header = "مشتری", ValueSelector = x => $"{x.Customer} {x.AgentName}" },
                new() { Header = "کارشناس فروش", ValueSelector = x => x.SelerFullName },
                new() { Header = "شماره فاکتور مالی", ValueSelector = x => x.SnFinancial },
                new() { Header = "وضعیت", ValueSelector = x => x.statusTxt },
                new() { Header = "صادراتی", ValueSelector = x => x.Export == true ? "بله" : "خیر" },
                new() { Header = "ثبت شده توسط", ValueSelector = x => x.UserCreator }
            };

            return ExcelExportService.ExportToExcel(factors, columns, "PreFactors");
        }


        private async Task<IList<PreFactor>> GetPrefactors(FilterVModel model)
        {
            try
            {
                var user = await userManager.GetUserAsync(HttpContext.User);
                var pListQ = preFactorService.BuildQuery();

                if (!string.IsNullOrWhiteSpace(model.serVal))
                {
                    var ser = model.serVal;
                    int? serNum = TextService.convertToInt(ser);

                    pListQ = pListQ.Where(s =>
                        EF.Functions.Like(s.AgentName, "%" + ser + "%") ||
                        EF.Functions.Like(s.Customer, "%" + ser + "%") ||
                        s.SerialNumber == serNum ||
                        s.CustomerEntity.CustomerCode == serNum
                    );
                }

                if (model.filt != null)
                {
                    pListQ = pListQ.ApplyFilter(model, f => f.CustomerId, f => f.DateMiladi, f => f.UserCreatorID);
                }

                if (!User.IsAdmin() && !User.IsSalesAdmin() && !User.IsFinancial())
                {
                    pListQ = pListQ.Where(f => f.UserCreatorID == user.Id);
                }

                pListQ = model._type switch {
                    5 => pListQ.Where(f => !f.Factors.Any()), // بدون فاکتور
                    6 => pListQ.Where(f => f.Factors.Any()), // دارای فاکتور
                    _ => pListQ
                };

                if (model.filt == null)
                {
                    pListQ = pListQ.Take(50);
                }

                var pList = await pListQ.AsNoTracking().ToListAsync();

                await preFactorService.FillDetailsAsync(pList);

                return pList;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        private async Task<IList<Factor>> GetFactors(FactorVModel fModel, AppUser user, IList<string> roles)
        {
            var model = fModel.FModel;
            try
            {
                var fListQ = db.Factors.AsQueryable();

                if (!string.IsNullOrWhiteSpace(model.serVal))
                {
                    var ser = model.serVal;
                    int? serNum = TextService.convertToInt(ser);

                    fListQ = fListQ.Where(s =>
                        EF.Functions.Like(s.AgentName, "%" + ser + "%") ||
                        EF.Functions.Like(s.Customer, "%" + ser + "%") ||
                        s.SerialNumber == serNum ||
                        s.CustomerCode == serNum ||
                        s.SnFinancial == ser
                    );
                }

                if (!roles.Any(new[] { "مدیر ارشد", "مدیر فروش", "کارشناس مالی", "مدیر مالی", "مشتری" }.Contains))
                {
                    fListQ = fListQ.Where(f => f.SelerId == user.Id);
                }

                if (model.filt != null)
                {
                    fListQ = fListQ.ApplyFilter(model, f => f.CustomerId, f => f.DateMiladi, f => f.SelerId);
                }

                fListQ = model._type switch {
                    1 => fListQ.Where(f => f.OfficialType == 1),
                    2 => fListQ.Where(f => f.OfficialType == 0),
                    3 => fListQ.Where(f => f.ParentReturnedId != null),
                    _ => fListQ
                };

                if (!string.IsNullOrEmpty(model.EntityId))
                {
                    fListQ = fListQ.Where(f => f.PfactorID == Guid.Parse(model.EntityId)).AsQueryable();
                } else if (fModel.CustomerId != null)
                {
                    fListQ = fListQ.Where(f => f.CustomerId == fModel.CustomerId).AsQueryable();
                }


                fListQ = fListQ
                    .OrderByDescending(f => f.DateMiladi)
                    .ThenByDescending(f => f.CreateDate);

                if (model.filt == null)
                {
                    fListQ = fListQ.Take(50);
                }

                var fList = await fListQ.AsNoTracking().ToListAsync();
                return fList;
            }
            catch
            {
                return null;
            }
        }


        [HttpGet]
        [Authorize(Roles = "مدیر ارشد , کارشناس تامین , مدیر تامین")]
        public async Task<IActionResult> GetPreFactorTaminExpert(string serVal, int type)
        {
            try
            {
                var user = await userManager.GetUserAsync(HttpContext.User);
                var rols = await userManager.GetRolesAsync(user);

                var model = new TaminViewModel {
                    UserId = user.Id,
                    Setting = _setting,
                    IsAdmin = User.IsAdmin() || User.IsTaminAdmin()
                };


                var queryBase = db.PreFactors.AsQueryable();

                if (!string.IsNullOrWhiteSpace(serVal))
                {
                    var serialVal = TextService.convertToInt(serVal);
                    queryBase = queryBase.Where(s =>
                        EF.Functions.Like(s.AgentName, $"%{serVal}%") ||
                        EF.Functions.Like(s.Customer, $"%{serVal}%") ||
                        s.SerialNumber == serialVal
                    );
                } else if (!rols.Contains("مدیر ارشد") && !rols.Contains("مدیر تامین"))
                {
                    queryBase = queryBase.Where(f => f.UserTaminId == Guid.Parse(user.Id));
                }

                var filterCounts = new Dictionary<int, int> {
                    [-1] = await queryBase.CountAsync(s => s.Pftype >= (int)PreFactorEnum.در_انتظار_تایید_مسئول_تامین),
                    [1] = await queryBase.CountAsync(s => s.Pftype == (int)PreFactorEnum.در_انتظار_تایید_مسئول_تامین),
                    [2] = await queryBase.CountAsync(s => s.Pftype > (int)PreFactorEnum.در_انتظار_تایید_مسئول_تامین),
                };


                var query = type switch {
                    -1 => queryBase.Where(s => s.Pftype >= (int)PreFactorEnum.در_انتظار_تایید_مسئول_تامین),
                    1 => queryBase.Where(s => s.Pftype == (int)PreFactorEnum.در_انتظار_تایید_مسئول_تامین),
                    2 => queryBase.Where(s => s.Pftype > (int)PreFactorEnum.در_انتظار_تایید_مسئول_تامین),
                    _ => queryBase
                };


                var preFactors = await query.OrderByDescending(f => f.ModifyDate).Take(100).Include(f => f.PreFactorItems).ToListAsync();

                var pfIds = preFactors.Select(f => f.Id).ToList();
                var supplies = await db.Supplies
                    .Where(f => pfIds.Contains(f.PfId))
                    .GroupBy(f => f.PfId)
                    .Select(f => new {
                        pfId = f.Key,
                        totalAmount = f.Sum(g => g.TotalAmount),
                        //totalWeight = f.Where(g => g.SupplyType == SupplyTypeEnum.هزینه_تولید).Sum(g => Convert.ToDecimal(g.Weight)),
                        totalItems = f.Where(g => g.SupplyType == SupplyTypeEnum.هزینه_تولید).GroupBy(g => g.PfItemId).Count()
                    })
                    .ToListAsync();


                foreach (var pf in preFactors)
                {
                    var s = supplies.FirstOrDefault(f => f.pfId == pf.Id);
                    if (s == null)
                    {
                        pf.NoSupply = true;
                        continue;
                    }

                    //var pfItemsWeight = pf.PreFactorItems.Sum(x => Convert.ToDecimal(x?.WeightCount));
                    var pfItemsCount = pf.PreFactorItems.Count;

                    pf.TotalSupplyAmount = s.totalAmount;

                    pf.SupplyMismatch = s.totalItems != pfItemsCount; //|| s.totalWeight != pfItemsWeight;
                }

                model.PreFactors = preFactors;


                var html = await vrs.RenderToStringAsync("GetPreFactorTaminExpert", model);
                return Ok(new { count = model.PreFactors.Count, html, filterCounts });
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }


        [HttpGet]
        [Authorize(Roles = "مدیر ارشد , مدیر مالی, کارشناس مالی")]
        public async Task<IActionResult> GetPreFactorFinancialExpert(string serVal, int type = 1)
        {
            try
            {
                var baseQuery = db.PreFactors.OrderByDescending(g => g.ModifyDate).AsQueryable();

                if (!string.IsNullOrWhiteSpace(serVal))
                {
                    baseQuery = baseQuery.Where(f =>
                        f.AgentName.Contains(serVal) ||
                        f.Customer.Contains(serVal) ||
                        f.SerialNumber == TextService.convertToInt(serVal)
                    );
                }

                const int pending = (int)PreFactorEnum.در_انتظار_تایید_مالی;
                int[] approved = [(int)PreFactorEnum.در_انتظار_تولید, (int)PreFactorEnum.جنس_به_مشتری_تحویل_داده_شد];

                var filterCounts = new Dictionary<int, int> {
                    [-1] = await baseQuery.Where(s => approved.Contains(s.Pftype) || s.Pftype == pending || s.AcceptedFinancial != null).CountAsync(),
                    [1] = await baseQuery.Where(s => s.Pftype == pending).CountAsync(),
                    [2] = await baseQuery.Where(s => approved.Contains(s.Pftype) || s.AcceptedFinancial == true).CountAsync(),
                    [3] = await baseQuery.Where(s => s.AcceptedFinancial == false).CountAsync(),
                };

                var query = type switch {
                    -1 => baseQuery.Where(f => approved.Contains(f.Pftype) || f.Pftype == pending || f.AcceptedFinancial != null),
                    1 => baseQuery.Where(f => f.Pftype == pending),
                    2 => baseQuery.Where(f => approved.Contains(f.Pftype) || f.AcceptedFinancial == true),
                    3 => baseQuery.Where(f => f.AcceptedFinancial == false),
                    _ => baseQuery
                };

                query = string.IsNullOrEmpty(serVal) ? query.Take(100) : query;

                var result = await query
                    .Select(pf => new PreFactorFinancialExpertVModel {
                        PreFactor = pf,
                        ReceiveAmount =
                            db.Accountancies.AsNoTracking().ForPreFactor()
                                .NotVosoled()
                                .Where(a => a.pfid == pf.Id)
                                .Sum(f => f.AccType == AcEnum.واریزی ? f.credit : -f.BackWard) ?? 0,

                        NotAcceptedFiAccCount =
                            db.Accountancies.AsNoTracking().ForPreFactor().OnlyFinancial()
                                .Count(a => a.pfid == pf.Id && a.Status != AccStatusEnum.تایید_شده),

                        NotAcceptedMdAccCount =
                            db.Accountancies.AsNoTracking().ForPreFactor().OnlyTamin()
                                .Count(a => a.pfid == pf.Id && a.Status != AccStatusEnum.تایید_شده),

                        TotalAccCount =
                            db.Accountancies.AsNoTracking().ForPreFactor()
                                .Count(a => a.pfid == pf.Id),
                    })
                    .ToListAsync();


                var html = await vrs.RenderToStringAsync("GetPreFactorFinancialExpert", result);
                return Ok(new { count = result.Count, html, filterCounts });
            }
            catch (Exception)
            {
                return BadRequest();
            }
        }


        [HttpGet]
        public async Task<IActionResult> GetSelesExpertPartial()
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            var data = db.PreFactors.Where(f => f.UserCreatorID == user.Id).ToList();
            return PartialView(data);
        }

        [HttpGet]
        [Authorize(Roles = "مدیر ارشد , مدیر فروش")]
        public async Task<IActionResult> GetPreFactorSalesAdmin(string serVal, int type)
        {
            try
            {
                var baseQuery = db.PreFactors.AsQueryable();

                if (!string.IsNullOrWhiteSpace(serVal))
                {
                    var serialVal = TextService.convertToInt(serVal);
                    baseQuery = baseQuery.Where(s =>
                        EF.Functions.Like(s.AgentName, $"%{serVal}%") ||
                        EF.Functions.Like(s.Customer, $"%{serVal}%") ||
                        s.SerialNumber == serialVal);
                }

                const int pending = (int)PreFactorEnum.در_انتظار_تایید_مدیر_فروش;
                var approved = new[] {
                    (int)PreFactorEnum.در_انتظار_تولید,
                    (int)PreFactorEnum.جنس_به_مشتری_تحویل_داده_شد,
                    (int)PreFactorEnum.در_انتظار_تایید_مالی
                };

                var filterCounts = new Dictionary<int, int> {
                    [-1] = await baseQuery.Where(s => approved.Contains(s.Pftype) || s.Pftype == pending).CountAsync(),
                    [1] = await baseQuery.Where(s => s.Pftype == pending).CountAsync(),
                    [2] = await baseQuery.Where(s => approved.Contains(s.Pftype)).CountAsync(),
                };

                var query = type switch {
                    -1 => baseQuery.Where(s => approved.Contains(s.Pftype) || s.Pftype == pending),
                    1 => baseQuery.Where(s => s.Pftype == pending),
                    2 => baseQuery.Where(s => approved.Contains(s.Pftype)),
                    _ => baseQuery
                };

                var pList = await (
                        from pf in query
                        let receiveAmount = db.Accountancies.ForPreFactor().NotVosoled()
                            .Where(a => a.pfid == pf.Id)
                            .Sum(f => f.AccType == AcEnum.واریزی ? (f.credit ?? 0) : -(f.BackWard ?? 0))
                        select new PreFactorSalesAdminViewModel {
                            PreFactor = pf,
                            ReceiveAmount = receiveAmount,
                            SumTotalAmountTax = pf.SumTotalAmount + pf.TotalTax - pf.Discount,
                            PaymentAmount = pf.SumTotalAmount + pf.TotalTax - pf.Discount - receiveAmount
                        })
                    .OrderByDescending(f => f.PreFactor.AcceptedFinancial == false)
                    .ThenByDescending(f => f.ReceiveAmount != 0)
                    .ThenByDescending(f => f.PreFactor.ModifyDate)
                    .Take(100)
                    .ToListAsync();


                var html = await vrs.RenderToStringAsync("GetPreFactorSalesAdmin", pList);
                return Ok(new { count = pList.Count, html, filterCounts });
            }
            catch (Exception)
            {
                return BadRequest();
            }
        }

        [HttpPost]
        public async Task<IActionResult> FactorPartial([FromBody] FactorVModel model)
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            var rols = await userManager.GetRolesAsync(user);

            var fList = await GetFactors(model, user, rols);

            var vModel = new FactorsViewModel {
                Factors = fList,
                _Setting = _setting,
            };

            var html = await vrs.RenderToStringAsync("FactorPartial", vModel,
                new Dictionary<string, object> {
                    { "PassType", model.PassType } //passType: (0:PreFactor,Factor | 1:CustomerProfile | 5:Supply | 4:Deal | 6:Email)
                });

            return Ok(new { count = fList?.Count ?? 0, html });
        }

        [HttpGet]
        public async Task<IActionResult> GetAllFactorsPartialModal(Guid id, int type)
        {
            try
            {
                var user = await userManager.GetUserAsync(HttpContext.User);
                ViewBag.PassType = type; //passType: (0:PreFactor,Factor | 1:CustomerProfile | 5:Supply | 4:Deal | 6:Email)

                var fList = type switch {
                    4 => await db.Factors.Where(f => f.DealId == id).ToListAsync(),
                    5 => await db.Factors.Where(f => f.PfactorID == id).ToListAsync(),
                    6 => await db.Factors.Where(f => f.Id == id).ToListAsync(),
                    _ => null
                };

                var model = new FactorsViewModel {
                    Factors = fList,
                    _Setting = _setting
                };

                var html = await vrs.RenderToStringAsync("FactorPartial", model,
                    new Dictionary<string, object> {
                        { "PassType", ViewBag.PassType }
                    }
                );

                return Ok(new { count = fList?.Count ?? 0, html });
            }
            catch (Exception)
            {
                return BadRequest();
            }
        }
    }
}