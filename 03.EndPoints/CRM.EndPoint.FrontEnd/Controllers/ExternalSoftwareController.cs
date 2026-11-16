using System;
using System.Collections.Generic;
using System.Data;
using CRM.Core.Domain.BaseItems.Enums;
using CRM.Core.Domain.ExternalSoftwares.Enums;
using System.Threading;
using System.Linq;
using CRM.Core.Domain.ExternalSoftwares.Entites;
using CRM.Infrastructures.Sql;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using CRM.Core.ApplicationServices.Security;
using CRM.EndPoint.FrontEnd.Services.ExternalMappings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.EndPoint.FrontEnd.Controllers
{
    [Authorize(Roles = AppRoles.Admin)]
    public sealed class ExternalSoftwareController : Controller
    {
        private readonly ExternalMappingService mappings;
        private readonly CRMContextDb db;

        public ExternalSoftwareController(ExternalMappingService mappings, CRMContextDb db)
        {
            this.mappings = mappings;
            this.db = db;
        }

        [HttpGet]
        public IActionResult Index() => View();

        // The URL identifies the destination; no client-side software switch is allowed.
        [HttpGet]
        public async Task<IActionResult> Mapping(Guid softwareId, CancellationToken ct)
        {
            if (softwareId == Guid.Empty || !mappings.SupportsMapping(softwareId)
                || !await db.ExternalSoftwares.AsNoTracking().AnyAsync(x => x.Id == softwareId, ct))
                return NotFound();

            var software = await db.ExternalSoftwares.AsNoTracking()
                .Where(x => x.Id == softwareId)
                .Select(x => new { x.Id, x.Title })
                .SingleAsync(ct);
            ViewData["SoftwareId"] = software.Id;
            ViewData["SoftwareTitle"] = software.Title;
            return View();
        }


        public sealed class SaveExternalSoftwareRequest
        {
            public Guid? Id { get; set; }
            public string Title { get; set; }
            public int? SoftwareType { get; set; }
            public Guid? SoftwareBaseItemId { get; set; }
            public string DatabaseIp { get; set; }
            public string DatabaseName { get; set; }
            public string DatabaseUserName { get; set; }
            public string DatabasePassword { get; set; }
            public bool ClearDatabaseIp { get; set; }
            public bool ClearDatabaseName { get; set; }
            public bool ClearDatabaseUserName { get; set; }
            public bool ClearDatabasePassword { get; set; }
        }

        public sealed class DeleteExternalSoftwareRequest
        {
            public Guid Id { get; set; }
            public string ConfirmTitle { get; set; }
            public string Confirmation { get; set; }
        }

        [HttpGet]
        public async Task<IActionResult> GetExternalSoftwareFormOptions(CancellationToken ct)
        {
            var softwareTypes = Enum.GetValues<SoftwareTypeEnum>()
                .Select(x => new { value = (int)x, text = x.ToString().Replace("_", " ") });
            var baseItems = await db.BaseItems.AsNoTracking()
                .Where(x => x.TypeCode == BaseItemEnum.نرم_افزار_حسابداری)
                .OrderBy(x => x.Title)
                .Select(x => new { value = x.Id, text = x.Title })
                .ToListAsync(ct);
            return Ok(new { softwareTypes, baseItems });
        }

        private static string NormalizeOptional(string value, int maxLength, string field)
        {
            var normalized = value?.Trim();
            if (string.IsNullOrEmpty(normalized)) return null;
            if (normalized.Length > maxLength)
                throw new ArgumentException($"طول {field} نباید بیشتر از {maxLength} کاراکتر باشد.");
            return normalized;
        }

        private static string NormalizePassword(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            if (value.Length > 256)
                throw new ArgumentException("طول رمز عبور نباید بیشتر از ۲۵۶ کاراکتر باشد.");
            return value;
        }

        private async Task<(string Title, SoftwareTypeEnum Type, Guid? BaseId,
            string BaseTitle, string Ip, string Name, string User, string Password)>
            ValidateSoftwareRequestAsync(SaveExternalSoftwareRequest request, CancellationToken ct)
        {
            var title = request?.Title?.Trim();
            if (string.IsNullOrWhiteSpace(title) || title.Length > 120)
                throw new ArgumentException("نام نرم‌افزار باید بین ۱ تا ۱۲۰ کاراکتر باشد.");
            if (request.SoftwareType is not int typeValue
                || !Enum.IsDefined(typeof(SoftwareTypeEnum), typeValue))
                throw new ArgumentException("نوع نرم‌افزار معتبر نیست.");

            var type = (SoftwareTypeEnum)typeValue;
            string baseTitle = null;
            if (request.SoftwareBaseItemId.HasValue)
            {
                if (request.SoftwareBaseItemId == Guid.Empty)
                    throw new ArgumentException("نرم‌افزار پایه معتبر نیست.");
                baseTitle = await db.BaseItems.AsNoTracking()
                    .Where(x => x.Id == request.SoftwareBaseItemId
                        && x.TypeCode == BaseItemEnum.نرم_افزار_حسابداری)
                    .Select(x => x.Title)
                    .FirstOrDefaultAsync(ct);
                if (baseTitle == null)
                    throw new ArgumentException("نرم‌افزار پایه در فهرست نرم‌افزارهای حسابداری وجود ندارد.");
            }
            if (type == SoftwareTypeEnum.حسابداری && !request.SoftwareBaseItemId.HasValue)
                throw new ArgumentException("برای نرم‌افزار حسابداری، نرم‌افزار پایه را انتخاب کنید.");

            return (title, type, request.SoftwareBaseItemId, baseTitle,
                NormalizeOptional(request.DatabaseIp, 255, "آدرس سرور"),
                NormalizeOptional(request.DatabaseName, 128, "نام دیتابیس"),
                NormalizeOptional(request.DatabaseUserName, 128, "نام کاربری دیتابیس"),
                NormalizePassword(request.DatabasePassword));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddExternalSoftware(
            [FromBody] SaveExternalSoftwareRequest request, CancellationToken ct)
        {
            if (request == null || request.Id.HasValue)
                return BadRequest("اطلاعات ثبت نرم‌افزار معتبر نیست.");

            (string Title, SoftwareTypeEnum Type, Guid? BaseId, string BaseTitle,
                string Ip, string Name, string User, string Password) input;
            try { input = await ValidateSoftwareRequestAsync(request, ct); }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }

            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            if (await db.ExternalSoftwares.AsNoTracking()
                .AnyAsync(x => x.Title == input.Title, ct))
                return Conflict("نرم‌افزاری با این نام قبلاً ثبت شده است.");

            var model = new ExternalSoftware
            {
                Title = input.Title,
                SoftwareType = input.Type,
                SoftwareTypeTxt = input.Type.ToString().Replace("_", " "),
                SoftwareBaseItemId = input.BaseId,
                SoftwareBaseItemTxt = input.BaseTitle,
                DatabaseIp = input.Ip,
                DatabaseName = input.Name,
                DatabaseUserName = input.User,
                DatabasePassword = input.Password
                // Code is an IDENTITY column and is generated by SQL Server.
            };
            db.ExternalSoftwares.Add(model);
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return Ok(new { id = model.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateExternalSoftware(
            [FromBody] SaveExternalSoftwareRequest request, CancellationToken ct)
        {
            if (request?.Id is not Guid id || id == Guid.Empty)
                return BadRequest("شناسه نرم‌افزار معتبر نیست.");

            (string Title, SoftwareTypeEnum Type, Guid? BaseId, string BaseTitle,
                string Ip, string Name, string User, string Password) input;
            try { input = await ValidateSoftwareRequestAsync(request, ct); }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }

            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            var model = await db.ExternalSoftwares.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (model == null) return NotFound("نرم‌افزار موردنظر پیدا نشد.");
            if (await db.ExternalSoftwares.AsNoTracking()
                .AnyAsync(x => x.Id != id && x.Title == input.Title, ct))
                return Conflict("نرم‌افزاری با این نام قبلاً ثبت شده است.");

            model.Title = input.Title;
            model.SoftwareType = input.Type;
            model.SoftwareTypeTxt = input.Type.ToString().Replace("_", " ");
            model.SoftwareBaseItemId = input.BaseId;
            model.SoftwareBaseItemTxt = input.BaseTitle;
            // Empty edit fields mean "keep existing value"; the admin must opt in to clear.
            model.DatabaseIp = request.ClearDatabaseIp ? null : input.Ip ?? model.DatabaseIp;
            model.DatabaseName = request.ClearDatabaseName ? null : input.Name ?? model.DatabaseName;
            model.DatabaseUserName = request.ClearDatabaseUserName ? null : input.User ?? model.DatabaseUserName;
            model.DatabasePassword = request.ClearDatabasePassword ? null : input.Password ?? model.DatabasePassword;

            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return Ok(new { id = model.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteExternalSoftware(
            [FromBody] DeleteExternalSoftwareRequest request, CancellationToken ct)
        {
            if (request?.Id is not Guid id || id == Guid.Empty
                || request.Confirmation != "حذف دائمی"
                || string.IsNullOrWhiteSpace(request.ConfirmTitle))
                return BadRequest("برای حذف، تأیید کامل لازم است.");

            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            var model = await db.ExternalSoftwares.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (model == null) return NotFound("نرم‌افزار پیدا نشد.");
            if (!string.Equals(request.ConfirmTitle.Trim(), model.Title, StringComparison.Ordinal))
                return BadRequest("نام واردشده با نام نرم‌افزار مطابقت ندارد.");

            if (await db.ExternalMappings.AsNoTracking().AnyAsync(x => x.ExternalSoftwareId == id, ct))
                return Conflict("برای این نرم‌افزار ارتباط ثبت‌شده وجود دارد؛ ابتدا ارتباط‌ها را بررسی کنید.");

            if (await db.CashExternalSoftwares.AsNoTracking().AnyAsync(x => x.ExternalSoftwareId == id, ct)
                || await db.CashierExternalSoftwares.AsNoTracking().AnyAsync(x => x.ExternalSoftwareId == id, ct)
                || await db.ExternalSoftwareSettings.AsNoTracking().AnyAsync(x => x.ExternalSoftwareId == id, ct))
                return Conflict("این نرم‌افزار اطلاعات وابسته دارد و قابل حذف نیست.");

            db.ExternalSoftwares.Remove(model);
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return Ok(new { deleted = true });
        }

        // Only expose masked connection metadata to the admin UI; never serialize secrets.
        private static string MaskConnectionField(string value, int visibleChars = 2)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            var text = value.Trim();
            return text.Length <= visibleChars ? "••••" : text[..visibleChars] + "••••";
        }

        private static string MaskDatabaseAddress(string address)
        {
            if (string.IsNullOrWhiteSpace(address)) return null;
            var parts = address.Trim().Split('.');
            if (parts.Length == 4 && parts.All(x => byte.TryParse(x, out _)))
                return $"{parts[0]}.{parts[1]}.*.*";
            return MaskConnectionField(address);
        }

        [HttpGet]
        public async Task<IActionResult> GetExternalSoftwares(CancellationToken ct)
        {
            var software = await db.ExternalSoftwares.AsNoTracking()
                .OrderBy(x => x.Title)
                .Select(x => new {
                    x.Id, x.Title, x.Code, x.SoftwareType, x.SoftwareBaseItemId,
                    x.SoftwareTypeTxt, x.SoftwareBaseItemTxt, x.CreateDateTxt,
                    x.DatabaseIp, x.DatabaseName, x.DatabaseUserName, x.DatabasePassword
                })
                .ToListAsync(ct);

            return Ok(software.Select(x => new {
                id = x.Id,
                title = x.Title,
                code = x.Code,
                softwareType = (int)x.SoftwareType,
                softwareBaseItemId = x.SoftwareBaseItemId,
                softwareTypeTxt = x.SoftwareTypeTxt,
                softwareBaseItemTxt = x.SoftwareBaseItemTxt,
                createDateTxt = x.CreateDateTxt,
                databaseIpMasked = MaskDatabaseAddress(x.DatabaseIp),
                databaseNameMasked = MaskConnectionField(x.DatabaseName),
                databaseUserNameMasked = MaskConnectionField(x.DatabaseUserName, 1),
                hasDatabasePassword = !string.IsNullOrWhiteSpace(x.DatabasePassword),
                canMap = mappings.SupportsMapping(x.Id)
            }));
        }

        [HttpGet]
        public async Task<IActionResult> GetExternalMappingWorkspaceOptions(CancellationToken ct)
            => Ok(await mappings.GetExternalMappingWorkspaceOptions(ct));

        [HttpGet]
        public async Task<IActionResult> GetExternalMappingReview(Guid softwareId, CancellationToken ct)
        {
            try
            {
                return Ok(await mappings.GetExternalMappingReview(softwareId, ct));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetSavedExternalMappings(
            Guid softwareId, int? mapType = null, string search = "",
            int page = 1, int pageSize = 12, CancellationToken ct = default)
        {
            try
            {
                return Ok(await mappings.GetSavedExternalMappings(
                    softwareId, mapType, search, page, pageSize, ct));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetExternalMappingForEdit(
            Guid softwareId, Guid mappingId, CancellationToken ct)
        {
            try
            {
                return Ok(await mappings.GetExternalMappingForEditAsync(softwareId, mappingId, ct));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetManualMappingOptions(
            Guid softwareId, int mapType, string side, string search = "",
            string sourceEntity = null, string targetEntity = null,
            CancellationToken ct = default)
        {
            try
            {
                return Ok(await mappings.GetManualMappingOptionsAsync(
                    softwareId, mapType, side, search, sourceEntity, targetEntity, ct));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetExternalMappingTargetDetails(
            Guid softwareId, int mapType, int targetId, string targetEntity = null,
            CancellationToken ct = default)
        {
            try
            {
                return Ok(await mappings.GetExternalMappingTargetDetails(
                    softwareId, mapType, targetId, ct, targetEntity));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        public sealed class DeleteExternalMappingRequest
        {
            public Guid SoftwareId { get; set; }
            public Guid MappingId { get; set; }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteExternalMapping(
            [FromBody] DeleteExternalMappingRequest request, CancellationToken ct)
        {
            if (request == null)
                return BadRequest("ارتباط انتخاب نشده است.");

            try
            {
                await mappings.DeleteExternalMappingAsync(
                    request.SoftwareId, request.MappingId, ct);
                return Ok(new { deleted = true });
            }
            catch (ExternalMappingService.MappingConflictException ex)
            {
                return Conflict(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateExternalMapping(
            [FromBody] ExternalMappingService.UpdateExternalMappingRequest request,
            CancellationToken ct)
        {
            try
            {
                await mappings.UpdateExternalMappingAsync(request, User, ct);
                return Ok(new { updated = true });
            }
            catch (ExternalMappingService.MappingConflictException ex)
            {
                return Conflict(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveSelectedExternalMappings(
            [FromBody] ExternalMappingService.SaveExternalMappingsRequest request,
            CancellationToken ct)
        {
            try
            {
                var count = await mappings.SaveSelectedExternalMappings(request, User, ct);
                return Ok(new { count });
            }
            catch (ExternalMappingService.MappingConflictException ex)
            {
                return Conflict(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
        }
    }
}
