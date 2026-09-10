using InterviewProjectTemplate.Data;
using InterviewProjectTemplate.DTOs;
using InterviewProjectTemplate.Models;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;

namespace InterviewProjectTemplate.Services
{
    public class MoodService : IMoodService
    {
        private readonly AppDbContext _context;
        private readonly TimeProvider _timeProvider;

        public MoodService(AppDbContext context, TimeProvider timeProvider)
        {
            _context = context;
            _timeProvider = timeProvider;
        }
        public async Task<CreateMoodEntryResult> CreateAsync(CreateMoodEntryRequest request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.EmployeeIdentifier) ||
                request.EmployeeIdentifier.Length > 50 ||
                request.Rating is null ||
                !Enum.IsDefined(typeof(MoodRating), request.Rating.Value) ||
            request.Comment?.Length > 1000)
            {
                return new(CreateMoodEntryStatus.InvalidInput);
            }

            var identifier = request.EmployeeIdentifier
                .Trim()
                .ToUpperInvariant();

            var employee = await _context.Employees
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    e => e.EmployeeIdentifier == identifier,
                    cancellationToken);

            if (employee is null)
            {
                return new(CreateMoodEntryStatus.EmployeeNotFound);
            }

            if (!employee.IsActive)
            {
                return new(CreateMoodEntryStatus.EmployeeInactive);
            }

            var now = _timeProvider.GetUtcNow().UtcDateTime;
            var submissionDate = DateOnly.FromDateTime(now);

            var alreadySubmitted = await _context.MoodEntries
                .AnyAsync(
                    e => e.EmployeeId == employee.Id &&
                         e.SubmissionDate == submissionDate,
                    cancellationToken);

            if (alreadySubmitted)
            {
                return new(CreateMoodEntryStatus.AlreadySubmitted);
            }

            var entry = new MoodEntry
            {
                EmployeeId = employee.Id,
                Rating = request.Rating.Value,
                Comment = string.IsNullOrWhiteSpace(request.Comment)
                    ? null
                    : request.Comment.Trim(),
                CreatedAtUtc = now,
                SubmissionDate = submissionDate
            };

            _context.MoodEntries.Add(entry);

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException exception)
                when (exception.InnerException is MySqlException
                { Number: 1062 })
            {
                _context.Entry(entry).State = EntityState.Detached;

                var duplicateExists = await _context.MoodEntries
                    .AnyAsync(
                        e => e.EmployeeId == employee.Id &&
                             e.SubmissionDate == submissionDate,
                        cancellationToken);

                if (duplicateExists)
                {
                    return new(CreateMoodEntryStatus.AlreadySubmitted);
                }

                throw;
            }

            return new(CreateMoodEntryStatus.Success, entry.Id);
        }

        public async Task<IReadOnlyList<MoodEntryResponse>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.MoodEntries
                .OrderByDescending(entry => entry.CreatedAtUtc)
                .ThenByDescending(entry => entry.Id)
                .Select(entry => new MoodEntryResponse(
                    entry.Id,
                    entry.Employee.EmployeeIdentifier,
                    entry.Rating,
                    entry.Comment,
                    entry.CreatedAtUtc))
                .ToListAsync(cancellationToken);
        }

        public async Task<MoodDashboardResponse> GetDashboardAsync(MoodDashboardRequest request, CancellationToken cancellationToken = default)
        {
            System.ComponentModel.DataAnnotations.Validator.ValidateObject(
                request, new System.ComponentModel.DataAnnotations.ValidationContext(request), true);

            var query = _context.MoodEntries.AsNoTracking();
            if (request.From is { } from)
                query = query.Where(entry => entry.SubmissionDate >= from);
            if (request.To is { } to)
                query = query.Where(entry => entry.SubmissionDate <= to);
            if (request.Rating is { } rating)
                query = query.Where(entry => entry.Rating == rating);

            var counts = await query.GroupBy(entry => entry.Rating)
                .Select(group => new { Rating = group.Key, Count = group.Count() })
                .ToListAsync(cancellationToken);
            var total = counts.Sum(item => item.Count);
            var statistics = Enum.GetValues<MoodRating>().Select(rating =>
            {
                var count = counts.FirstOrDefault(item => item.Rating == rating)?.Count ?? 0;
                return new MoodStatistic(rating, count,
                    total == 0 ? 0 : Math.Round(count * 100m / total, 1, MidpointRounding.AwayFromZero));
            }).ToArray();
            var page = Math.Min(request.Page, Math.Max(1, (int)Math.Ceiling(total / (double)request.PageSize)));
            var entries = await query.OrderByDescending(entry => entry.CreatedAtUtc)
                .ThenByDescending(entry => entry.Id)
                .Skip((page - 1) * request.PageSize).Take(request.PageSize)
                .Select(entry => new MoodEntryResponse(entry.Id, entry.Employee.EmployeeIdentifier,
                    entry.Rating, entry.Comment, entry.CreatedAtUtc))
                .ToListAsync(cancellationToken);
            return new MoodDashboardResponse(entries, total, page, request.PageSize, statistics);
        }
    }
}
