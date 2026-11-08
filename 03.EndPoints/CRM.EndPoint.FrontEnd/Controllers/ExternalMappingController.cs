using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.ApplicationServices.Security;
using CRM.EndPoint.FrontEnd.Services.ExternalMappings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.EndPoint.FrontEnd.Controllers
{
    [Authorize(Roles = AppRoles.Admin)]
    public sealed class ExternalMappingController : Controller
    {
        private readonly ExternalMappingService mappings;

        public ExternalMappingController(ExternalMappingService mappings)
        {
            this.mappings = mappings;
        }

        [HttpGet]
        public IActionResult Index() => View();

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
        public async Task<IActionResult> GetExternalMappingTargetDetails(
            Guid softwareId, int mapType, int targetId, CancellationToken ct)
        {
            try
            {
                return Ok(await mappings.GetExternalMappingTargetDetails(
                    softwareId, mapType, targetId, ct));
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
                return BadRequest("نگاشت انتخاب نشده است.");

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
