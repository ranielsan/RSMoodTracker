using InterviewProjectTemplate.Models;
using Microsoft.EntityFrameworkCore;

namespace InterviewProjectTemplate.Data
{
    public static class DatabaseInitializer
    {
        public static async Task InitializeAsync(AppDbContext context)
        {
            await context.Database.MigrateAsync();

            await SeedEmployeesAsync(context);
        }

        public static async Task SeedEmployeesAsync(AppDbContext context)
        {
            string[] demoIdentifiers = ["EMP001", "EMP002", "EMP003", "EMP004", "EMP005"];

            var existingIdentifiers = await context.Employees
                .Where(e => demoIdentifiers.Contains(e.EmployeeIdentifier))
                .Select(e => e.EmployeeIdentifier)
                .ToListAsync();

            foreach (var identifier in demoIdentifiers)
            {
                if (!existingIdentifiers.Contains(identifier))
                {
                    context.Employees.Add(new Employee
                    {
                        EmployeeIdentifier = identifier,
                        IsActive = true
                    });
                }
            }

            await context.SaveChangesAsync();
        }
    }
}
