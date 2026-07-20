using DigitalBookLibrary.Application.Common;
using DigitalBookLibrary.Application.DTOs.Dashboard;
using DigitalBookLibrary.Application.Services;
using DigitalBookLibrary.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DigitalBookLibrary.WebAPI.Controllers
{
    /// <summary>Admin user management and audit trail (docs/05 §8c). Every endpoint is Admin-only.</summary>
    [ApiController]
    [Route("api/admin/users")]
    [Authorize(Roles = Roles.Admin)]
    public class AdminUsersController : ControllerBase
    {
        private readonly DashboardService _dashboard;

        public AdminUsersController(DashboardService dashboard) => _dashboard = dashboard;

        /// <summary>Paged user list with search, active filter and role filter.</summary>
        [HttpGet]
        public async Task<IActionResult> GetUsers(
            [FromQuery] AdminUserQueryOptions options, CancellationToken cancellationToken)
            => Ok(ApiResponse.Ok(await _dashboard.GetUsersAsync(options, cancellationToken)));

        /// <summary>
        /// Audit trail, newest first, filterable by entity and action.
        /// Declared before the <c>{id:int}</c> routes so "audit" is never read as an id.
        /// </summary>
        [HttpGet("audit")]
        public async Task<IActionResult> GetAudit(
            [FromQuery] AuditQueryOptions options, CancellationToken cancellationToken)
            => Ok(ApiResponse.Ok(await _dashboard.GetAuditAsync(options, cancellationToken)));

        /// <summary>Activate or deactivate an account. An admin cannot deactivate themselves.</summary>
        [HttpPatch("{id:int}/active")]
        public async Task<IActionResult> SetActive(
            int id, SetUserActiveDto dto, CancellationToken cancellationToken)
            => Ok(ApiResponse.Ok(
                await _dashboard.SetActiveAsync(id, dto.IsActive, cancellationToken), ResponseCodes.Updated));

        /// <summary>Replace an account's roles. An admin cannot strip their own Admin role.</summary>
        [HttpPatch("{id:int}/roles")]
        public async Task<IActionResult> SetRoles(
            int id, SetUserRolesDto dto, CancellationToken cancellationToken)
            => Ok(ApiResponse.Ok(
                await _dashboard.SetRolesAsync(id, dto.Roles, cancellationToken), ResponseCodes.Updated));
    }
}
