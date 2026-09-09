using Microsoft.AspNetCore.Identity;

namespace InterviewProjectTemplate.Data
{
    public static class AdminSeeder
    {
        public static async Task SeedAsync(
        UserManager<IdentityUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IConfiguration configuration)
        {
            var username = configuration["AdminSeed:Username"];
            var password = configuration["AdminSeed:Password"];

            if (string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(password))
            {
                throw new InvalidOperationException(
                    "AdminSeed username and password must be configured.");
            }

            const string adminRole = "Admin";

            if (!await roleManager.RoleExistsAsync(adminRole))
            {
                var roleResult = await roleManager.CreateAsync(
                    new IdentityRole(adminRole));

                EnsureSucceeded(roleResult, "creating the Admin role");
            }

            var admin = await userManager.FindByNameAsync(username);

            if (admin is null)
            {
                admin = new IdentityUser
                {
                    UserName = username,
                    LockoutEnabled = true
                };

                var userResult = await userManager.CreateAsync(
                    admin,
                    password);

                EnsureSucceeded(userResult, "creating the admin account");
            }

            if (!await userManager.IsInRoleAsync(admin, adminRole))
            {
                var assignmentResult = await userManager.AddToRoleAsync(
                    admin,
                    adminRole);

                EnsureSucceeded(assignmentResult, "assigning the Admin role");
            }
        }

        private static void EnsureSucceeded(
            IdentityResult result,
            string operation)
        {
            if (!result.Succeeded)
            {
                var errorCodes = string.Join(
                    ", ",
                    result.Errors.Select(error => error.Code));

                throw new InvalidOperationException(
                    $"Failed while {operation}: {errorCodes}");
            }
        }
    }
}
