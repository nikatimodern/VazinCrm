using System;
using System.Collections.Generic;
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
                return RedirectToAction(nameof(Index));

            var software = await db.ExternalSoftwares.AsNoTracking()
                .Where(x => x.Id == softwareId)
                .Select(x => new { x.Id, x.Title })
                .SingleAsync(ct);
            ViewData["SoftwareId"] = software.Id;
            ViewData["SoftwareTitle"] = software.Title;
            return View();
        }

        // Redirect old bookmarks to the canonical entry points, without duplicating actions.
        [HttpGet("/ASetting/ExternalSoftware")]
        public IActionResult LegacySoftwareList() => RedirectToAction(nameof(Index));

        [HttpGet("/ExternalMapping/Index")]
        public IActionResult LegacyMapping(Guid? softwareId)
            => softwareId.HasValue && softwareId.Value != Guid.Empty
                ? RedirectToAction(nameof(Mapping), new { softwareId = softwareId.Value })
                : RedirectToAction(nameof(Index));

        public sealed class AddExternalSoftwareRequest
        {
            public string Title { get; set; }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddExternalSoftware(
            [FromBody] AddExternalSoftwareRequest request, CancellationToken ct)
        {
            var title = request?.Title?.Trim();
            if (string.IsNullOrWhiteSpace(title) || title.Length > 120)
                return BadRequest("عنوان نرم‌افزار باید بین ۱ تا ۱۲۰ کاراکتر باشد.");

            if (await db.ExternalSoftwares.AsNoTracking()
                .AnyAsync(x => x.Title == title, ct))
                return Conflict("نرم‌افزاری با این عنوان قبلاً ثبت شده است.");

            var model = new ExternalSoftware { Title = title };
            db.ExternalSoftwares.Add(model);
            await db.SaveChangesAsync(ct);
            return Ok(new { id = model.Id, title = model.Title, canMap = mappings.SupportsMapping(model.Id) });
        }

        [HttpGet]
        public async Task<IActionResult> GetExternalSoftwares(CancellationToken ct)
        {
            var software = await db.ExternalSoftwares.AsNoTracking()
                .OrderBy(x => x.Title)
                .Select(x => new { x.Id, x.Title, x.Code })
                .ToListAsync(ct);
            // Never return database passwords, connection credentials or connection strings.
            return Ok(software.Select(x => new {
                id = x.Id, title = x.Title, code = x.Code,
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
