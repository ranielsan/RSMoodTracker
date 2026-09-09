using InterviewProjectTemplate.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace InterviewProjectTemplate.Data
{
    public class AppDbContext :  IdentityDbContext<IdentityUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
        {
        }

        public DbSet<Employee> Employees => Set<Employee>();

        public DbSet<MoodEntry> MoodEntries => Set<MoodEntry>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Employee>(entity =>
            {
                entity.ToTable("Employees");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.EmployeeIdentifier)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.HasIndex(e => e.EmployeeIdentifier)
                    .IsUnique();
            });

            modelBuilder.Entity<MoodEntry>(entity =>
            {
                entity.ToTable("MoodEntries", table =>
                    table.HasCheckConstraint(
                        "CK_MoodEntries_Rating",
                        "`Rating` BETWEEN 1 AND 4"));

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Rating)
                    .HasConversion<int>()
                    .IsRequired();

                entity.Property(e => e.Comment)
                    .HasMaxLength(1000);

                entity.Property(e => e.CreatedAtUtc)
                    .HasColumnType("datetime(6)");

                entity.Property(e => e.SubmissionDate)
                    .HasColumnType("date");

                entity.HasIndex(e => new
                {
                    e.EmployeeId,
                    e.SubmissionDate
                })
                    .IsUnique();

                entity.HasIndex(e => new
                {
                    e.CreatedAtUtc,
                    e.Id
                });

                entity.HasOne(e => e.Employee)
                    .WithMany(e => e.MoodEntries)
                    .HasForeignKey(e => e.EmployeeId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
