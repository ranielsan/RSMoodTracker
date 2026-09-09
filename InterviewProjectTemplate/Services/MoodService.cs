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
    }
}
