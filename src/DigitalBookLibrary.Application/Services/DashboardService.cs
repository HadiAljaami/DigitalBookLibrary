using DigitalBookLibrary.Application.DTOs.Dashboard;
using DigitalBookLibrary.Application.Interfaces;
using DigitalBookLibrary.Application.Mapping;
using DigitalBookLibrary.Domain.Common;
using DigitalBookLibrary.Domain.Entities;
using DigitalBookLibrary.Domain.Errors;
using DigitalBookLibrary.Domain.Exceptions;
using DigitalBookLibrary.Domain.Interfaces;

namespace DigitalBookLibrary.Application.Services
{
    /// <summary>
    /// Admin dashboard: KPIs, top books, recent activity, time-series, distributions and user
    /// management (docs/05 §8b, §8c). Every caller is Admin-gated at the controller.
    /// </summary>
    public class DashboardService
    {
        /// <summary>Chart defaults when the caller gives no range: a month of days, or a year of months.</summary>
        private const int DefaultDays = 30;
        private const int DefaultMonths = 12;

        /// <summary>Top-N and feed sizes are clamped so a crafted `take` can't dump whole tables.</summary>
        private const int MaxTake = 50;

        private readonly IDashboardRepository _dashboard;
        private readonly IUserRepository _users;
        private readonly IRepository<Role> _roles;
        private readonly ICurrentUser _currentUser;
        private readonly IUnitOfWork _uow;

        public DashboardService(
            IDashboardRepository dashboard,
            IUserRepository users,
            IRepository<Role> roles,
            ICurrentUser currentUser,
            IUnitOfWork uow)
        {
            _dashboard = dashboard;
            _users = users;
            _roles = roles;
            _currentUser = currentUser;
            _uow = uow;
        }

        // ---------- §8b Dashboard ----------

        public async Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
            => (await _dashboard.GetSummaryAsync(cancellationToken)).ToDto();

        public async Task<List<TopBookDto>> GetTopBooksAsync(
            string? metric, int take, CancellationToken cancellationToken = default)
        {
            var parsed = ParseOrThrow<TopBooksMetric>(metric, "metric", TopBooksMetric.Downloads);
            var books = await _dashboard.GetTopBooksAsync(parsed, Clamp(take), cancellationToken);
            return books.Select(b => b.ToDto()).ToList();
        }

        public async Task<List<RecentActivityDto>> GetRecentAsync(
            string? type, int take, CancellationToken cancellationToken = default)
        {
            var parsed = ParseOrThrow<RecentActivityType>(type, "type", RecentActivityType.Users);
            var items = await _dashboard.GetRecentAsync(parsed, Clamp(take), cancellationToken);
            return items.Select(i => i.ToDto()).ToList();
        }

        public async Task<List<ActivityPointDto>> GetActivitySeriesAsync(
            DateTime? from, DateTime? to, string? interval, CancellationToken cancellationToken = default)
        {
            var parsed = ParseOrThrow<SeriesInterval>(interval, "interval", SeriesInterval.Day);

            // `to` is exclusive and defaults to the start of tomorrow, so today's activity is included.
            var today = DateTime.UtcNow.Date;
            var end = to ?? today.AddDays(1);

            // The month default starts on a month boundary. Counting back a plain 12 months from
            // mid-July would start mid-July too, and the series' first bucket — labelled the 1st —
            // would hold only half a month's rows and plot as a misleading dip.
            var start = from ?? (parsed is SeriesInterval.Month
                ? new DateTime(today.Year, today.Month, 1).AddMonths(-(DefaultMonths - 1))
                : end.AddDays(-DefaultDays));

            if (start >= end)
            {
                throw new ValidationAppException(new Error(
                    CommonErrors.Validation.Code, "'from' must be earlier than 'to'."));
            }

            var points = await _dashboard.GetActivitySeriesAsync(start, end, parsed, cancellationToken);
            return points.Select(p => p.ToDto()).ToList();
        }

        public async Task<List<DistributionSliceDto>> GetDistributionAsync(
            string? by, CancellationToken cancellationToken = default)
        {
            var parsed = ParseOrThrow<DistributionBy>(by, "by", DistributionBy.Category);
            var slices = await _dashboard.GetDistributionAsync(parsed, cancellationToken);
            return slices.Select(s => s.ToDto()).ToList();
        }

        // ---------- §8c User management ----------

        public async Task<PagedResult<AdminUserDto>> GetUsersAsync(
            AdminUserQueryOptions options, CancellationToken cancellationToken = default)
        {
            var page = await _dashboard.GetPagedUsersAsync(options, cancellationToken);
            return new PagedResult<AdminUserDto>(
                page.Items.Select(u => u.ToAdminDto()).ToList(),
                page.TotalCount, page.PageNumber, page.PageSize);
        }

        /// <summary>Activate/deactivate an account (FR-DASH-6).</summary>
        public async Task<AdminUserDto> SetActiveAsync(
            int id, bool isActive, CancellationToken cancellationToken = default)
        {
            var user = await _users.GetByIdWithRolesAsync(id, cancellationToken)
                ?? throw new NotFoundException(UserErrors.NotFound);

            if (!isActive)
            {
                // Locking yourself out of the panel you are standing in.
                if (id == _currentUser.UserId)
                {
                    throw new ValidationAppException(UserErrors.CannotModifySelf);
                }

                await EnsureNotLastAdminAsync(user, cancellationToken);
            }

            user.IsActive = isActive;
            _users.Update(user);
            await _uow.SaveChangesAsync(cancellationToken);

            return user.ToAdminDto();
        }

        /// <summary>Replace an account's roles with the given set (FR-DASH-6).</summary>
        public async Task<AdminUserDto> SetRolesAsync(
            int id, IEnumerable<string> roleNames, CancellationToken cancellationToken = default)
        {
            var requested = roleNames
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Select(name => name.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var user = await _users.GetByIdWithRolesAsync(id, cancellationToken)
                ?? throw new NotFoundException(UserErrors.NotFound);

            var all = await _roles.ListAsync(cancellationToken);
            var resolved = requested
                .Select(name => all.FirstOrDefault(r =>
                    string.Equals(r.Name, name, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            // Unknown role names are rejected rather than silently dropped — a typo must not quietly
            // strip someone's access.
            if (resolved.Any(role => role is null))
            {
                throw new ValidationAppException(UserErrors.RoleInvalid);
            }

            if (!resolved.Any(r => r!.Name == Roles.Admin))
            {
                if (id == _currentUser.UserId)
                {
                    throw new ValidationAppException(UserErrors.CannotModifySelf);
                }

                await EnsureNotLastAdminAsync(user, cancellationToken);
            }

            // Diff rather than clear-and-re-add: the join rows have a composite key, so deleting and
            // re-inserting an unchanged one only churns the tracker.
            var wanted = resolved.Select(r => r!.Id).ToHashSet();

            foreach (var dropped in user.UserRoles.Where(ur => !wanted.Contains(ur.RoleId)).ToList())
            {
                user.UserRoles.Remove(dropped);
            }

            foreach (var role in resolved.Where(r => user.UserRoles.All(ur => ur.RoleId != r!.Id)))
            {
                // Only the FK. Attaching the Role instance from ListAsync (untracked — the generic
                // repository reads AsNoTracking) would collide with the tracked instance already
                // loaded through the user's includes.
                user.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role!.Id });
            }

            // No Update() call: the user is tracked, so SaveChanges picks the change up on its own —
            // and re-attaching the graph is exactly what caused that collision.
            await _uow.SaveChangesAsync(cancellationToken);

            // Names come from the resolved set: the rows just added carry no Role navigation.
            return user.ToAdminDto(resolved.Select(r => r!.Name).OrderBy(name => name).ToList());
        }

        /// <summary>Audit trail listing (FR-DASH-7).</summary>
        public async Task<PagedResult<AuditLogDto>> GetAuditAsync(
            AuditQueryOptions options, CancellationToken cancellationToken = default)
        {
            var page = await _dashboard.GetPagedAuditAsync(options, cancellationToken);
            return new PagedResult<AuditLogDto>(
                page.Items.Select(e => e.ToDto()).ToList(),
                page.TotalCount, page.PageNumber, page.PageSize);
        }

        // ---------- helpers ----------

        /// <summary>
        /// Blocks a change that would leave the system with no usable Admin. Only active admins count —
        /// a deactivated admin cannot administer anything.
        /// </summary>
        private async Task EnsureNotLastAdminAsync(UserAccount user, CancellationToken cancellationToken)
        {
            if (!user.UserRoles.Any(ur => ur.Role?.Name == Roles.Admin))
            {
                return;
            }

            var admins = await _dashboard.GetPagedUsersAsync(
                new AdminUserQueryOptions { Role = Roles.Admin, IsActive = true, PageSize = 1 },
                cancellationToken);

            if (admins.TotalCount <= 1)
            {
                throw new ValidationAppException(UserErrors.LastAdmin);
            }
        }

        private static int Clamp(int take) => take < 1 ? 10 : Math.Min(take, MaxTake);

        /// <summary>
        /// Parses a query-string enum case-insensitively. An unrecognised value is a 400 rather than a
        /// silent fallback, so a typo'd `metric=downlods` cannot quietly return the wrong chart.
        /// </summary>
        private static TEnum ParseOrThrow<TEnum>(string? value, string field, TEnum fallback)
            where TEnum : struct, Enum
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return fallback;
            }

            if (!Enum.TryParse<TEnum>(value.Trim(), ignoreCase: true, out var parsed) ||
                !Enum.IsDefined(parsed))
            {
                throw new ValidationAppException(new Error(
                    CommonErrors.Validation.Code,
                    $"'{field}' must be one of: {string.Join(" | ", Enum.GetNames<TEnum>()).ToLowerInvariant()}."));
            }

            return parsed;
        }
    }
}
