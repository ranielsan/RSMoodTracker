using InterviewProjectTemplate.DTOs;
using Microsoft.AspNetCore.Identity;

namespace InterviewProjectTemplate.Services
{
    public class AdminAuthService : IAdminAuthService
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;

        public AdminAuthService(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public async Task<AdminLoginResult> LoginAsync(AdminLoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) ||
            string.IsNullOrEmpty(request.Password))
            {
                return new(AdminLoginStatus.InvalidCredentials);
            }

            var user = await _userManager.FindByNameAsync(
                request.Username.Trim());

            if (user is null)
            {
                return new(AdminLoginStatus.InvalidCredentials);
            }

            var passwordResult =
                await _signInManager.CheckPasswordSignInAsync(
                    user,
                    request.Password,
                    lockoutOnFailure: true);

            if (passwordResult.IsLockedOut)
            {
                return new(AdminLoginStatus.LockedOut);
            }

            if (passwordResult.IsNotAllowed)
            {
                return new(AdminLoginStatus.NotAllowed);
            }

            if (!passwordResult.Succeeded)
            {
                return new(AdminLoginStatus.InvalidCredentials);
            }

            if (!await _userManager.IsInRoleAsync(user, "Admin"))
            {
                return new(AdminLoginStatus.NotAllowed);
            }

            // This login flow does not implement two-factor authentication.
            // Reject accounts requiring it instead of bypassing the second step.
            if (await _userManager.GetTwoFactorEnabledAsync(user))
            {
                return new(AdminLoginStatus.NotAllowed);
            }

            await _signInManager.SignInAsync(
                user,
                isPersistent: false);

            return new(AdminLoginStatus.Success);
        }

        public async Task LogoutAsync()
        {
            await _signInManager.SignOutAsync();
        }
    }
}
