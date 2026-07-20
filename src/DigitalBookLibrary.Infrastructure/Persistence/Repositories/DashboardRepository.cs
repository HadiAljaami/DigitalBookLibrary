using DigitalBookLibrary.Domain.Common;
using DigitalBookLibrary.Domain.Entities;
using DigitalBookLibrary.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DigitalBookLibrary.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// Dashboard aggregates. Every read is projected in SQL and <c>AsNoTracking</c> — these queries
    /// touch whole tables, so nothing is materialized that a card or chart doesn't display.
    /// </summary>
    public sealed class DashboardRepository : IDashboardRepository
    {
        private readonly AppDbContext _context;

        public DashboardRepository(AppDbContext context) => _context = context;

        public async Task<DashboardSummary> GetSummaryAsync(CancellationToken cancellationToken = default)
            => new(
                Books: await _context.Books.CountAsync(cancellationToken),
                Users: await _context.Users.CountAsync(cancellationToken),
                Authors: await _context.Authors.CountAsync(cancellationToken),
                Categories: await _context.Categories.CountAsync(cancellationToken),
                // Counted from the logs rather than summing Book.DownloadsCount: BookActivityService
                // commits the counter and the log row in one unit of work, so the two agree — and
                // using the logs keeps these totals consistent with the activity series below.
                Downloads: await _context.BookDownloadLogs.CountAsync(cancellationToken),
                Reads: await _context.BookReadLogs.CountAsync(cancellationToken));

        public async Task<IReadOnlyList<TopBook>> GetTopBooksAsync(
            TopBooksMetric metric, int take, CancellationToken cancellationToken = default)
        {
            // No IsVisible filter: this is the admin's view of the whole catalogue.
            // Ordering happens on the entity and before the projection — EF cannot translate an
            // OrderBy over a record projection's properties (it can't see through the constructor).
            var query = _context.Books.AsNoTracking().AsQueryable();

            query = metric switch
            {
                TopBooksMetric.Reads => query.OrderByDescending(b => b.ReadsCount).ThenBy(b => b.Title),
                // An unrated book scores 0, and a single 5★ rating shouldn't outrank a book with a
                // solid average over many ratings, so ties break on how many ratings back it up.
                TopBooksMetric.Rating => query
                    .OrderByDescending(b => b.Ratings.Any() ? b.Ratings.Average(r => (double)r.Value) : 0d)
                    .ThenByDescending(b => b.Ratings.Count)
                    .ThenBy(b => b.Title),
                _ => query.OrderByDescending(b => b.DownloadsCount).ThenBy(b => b.Title)
            };

            return await query
                .Take(take)
                .Select(b => new TopBook(
                    b.Id,
                    b.Title,
                    b.Author!.Person!.FullName,
                    b.DownloadsCount,
                    b.ReadsCount,
                    b.Ratings.Any() ? Math.Round(b.Ratings.Average(r => (double)r.Value), 2) : 0d,
                    b.Ratings.Count))
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<RecentActivity>> GetRecentAsync(
            RecentActivityType type, int take, CancellationToken cancellationToken = default)
            => type switch
            {
                RecentActivityType.Books => await _context.Books
                    .AsNoTracking()
                    .OrderByDescending(b => b.DateCreated)
                    .Take(take)
                    .Select(b => new RecentActivity(
                        "book", b.Id, b.Title, b.Author!.Person!.FullName, b.DateCreated))
                    .ToListAsync(cancellationToken),

                RecentActivityType.Comments => await _context.Comments
                    .AsNoTracking()
                    .OrderByDescending(c => c.DateCreated)
                    .Take(take)
                    .Select(c => new RecentActivity(
                        "comment", c.Id, c.Text, c.User!.Username + " · " + c.Book!.Title, c.DateCreated))
                    .ToListAsync(cancellationToken),

                _ => await _context.Users
                    .AsNoTracking()
                    .OrderByDescending(u => u.DateCreated)
                    .Take(take)
                    .Select(u => new RecentActivity("user", u.Id, u.Username, u.Email, u.DateCreated))
                    .ToListAsync(cancellationToken)
            };

        public async Task<IReadOnlyList<ActivityPoint>> GetActivitySeriesAsync(
            DateTime from, DateTime to, SeriesInterval interval, CancellationToken cancellationToken = default)
        {
            var downloads = await BucketAsync(
                _context.BookDownloadLogs
                    .Where(l => l.DateDownloaded >= from && l.DateDownloaded < to)
                    .Select(l => l.DateDownloaded),
                interval, cancellationToken);

            var reads = await BucketAsync(
                _context.BookReadLogs
                    .Where(l => l.DateRead >= from && l.DateRead < to)
                    .Select(l => l.DateRead),
                interval, cancellationToken);

            // Walk every bucket in range rather than only those with rows, so a quiet day still
            // plots as zero and the chart has no gaps.
            var points = new List<ActivityPoint>();
            for (var cursor = Floor(from, interval); cursor < to; cursor = Next(cursor, interval))
            {
                points.Add(new ActivityPoint(
                    cursor,
                    downloads.GetValueOrDefault(cursor),
                    reads.GetValueOrDefault(cursor)));
            }

            return points;
        }

        public async Task<IReadOnlyList<DistributionSlice>> GetDistributionAsync(
            DistributionBy by, CancellationToken cancellationToken = default)
        {
            // Grouped counts are projected to an anonymous type and ordered in SQL; the record is
            // built afterwards in memory, since EF can't order over a record projection.
            var rows = by switch
            {
                DistributionBy.Language => await _context.Books
                    .AsNoTracking()
                    .GroupBy(b => b.Language)
                    .Select(g => new { Label = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .ToListAsync(cancellationToken),

                _ => await _context.Books
                    .AsNoTracking()
                    .GroupBy(b => b.Category!.Name)
                    .Select(g => new { Label = (string?)g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .ToListAsync(cancellationToken)
            };

            // Books with no language recorded still deserve a slice rather than vanishing.
            return rows.Select(r => new DistributionSlice(r.Label ?? "UNKNOWN", r.Count)).ToList();
        }

        public async Task<PagedResult<UserAccount>> GetPagedUsersAsync(
            AdminUserQueryOptions options, CancellationToken cancellationToken = default)
        {
            var query = _context.Users
                .AsNoTracking()
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(options.Search))
            {
                var term = options.Search.Trim();
                query = query.Where(u => u.Username.Contains(term) || u.Email.Contains(term));
            }

            if (options.IsActive is not null)
            {
                query = query.Where(u => u.IsActive == options.IsActive);
            }

            if (!string.IsNullOrWhiteSpace(options.Role))
            {
                query = query.Where(u => u.UserRoles.Any(ur => ur.Role!.Name == options.Role));
            }

            query = query.OrderByDescending(u => u.DateCreated).ThenBy(u => u.Id);

            return await query.ToPagedResultAsync(options.PageNumber, options.PageSize, cancellationToken);
        }

        public async Task<PagedResult<AuditEntry>> GetPagedAuditAsync(
            AuditQueryOptions options, CancellationToken cancellationToken = default)
        {
            var query = _context.AuditLogs.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(options.EntityName))
            {
                query = query.Where(a => a.EntityName == options.EntityName);
            }

            if (!string.IsNullOrWhiteSpace(options.Action))
            {
                query = query.Where(a => a.Action == options.Action);
            }

            // Left join: AuditLog has no User navigation (rows outlive the accounts they name), and a
            // seeded/system change has no user at all — those must still appear in the trail.
            var projected =
                from audit in query
                join user in _context.Users.AsNoTracking() on audit.UserId equals user.Id into matches
                from user in matches.DefaultIfEmpty()
                orderby audit.Id descending
                select new AuditEntry(
                    audit.Id, audit.EntityName, audit.EntityId, audit.Action,
                    audit.UserId, user != null ? user.Username : null, audit.IpAddress, audit.CreatedAt,
                    audit.OldValues, audit.NewValues);

            return await projected.ToPagedResultAsync(options.PageNumber, options.PageSize, cancellationToken);
        }

        /// <summary>Counts rows per bucket in SQL, then keys them by bucket start.</summary>
        private static async Task<Dictionary<DateTime, int>> BucketAsync(
            IQueryable<DateTime> dates, SeriesInterval interval, CancellationToken cancellationToken)
        {
            if (interval is SeriesInterval.Month)
            {
                var months = await dates
                    .GroupBy(d => new { d.Year, d.Month })
                    .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
                    .ToListAsync(cancellationToken);

                return months.ToDictionary(m => new DateTime(m.Year, m.Month, 1), m => m.Count);
            }

            var days = await dates
                .GroupBy(d => d.Date)
                .Select(g => new { Period = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            return days.ToDictionary(d => d.Period, d => d.Count);
        }

        private static DateTime Floor(DateTime value, SeriesInterval interval)
            => interval is SeriesInterval.Month
                ? new DateTime(value.Year, value.Month, 1)
                : value.Date;

        private static DateTime Next(DateTime value, SeriesInterval interval)
            => interval is SeriesInterval.Month ? value.AddMonths(1) : value.AddDays(1);
    }
}
