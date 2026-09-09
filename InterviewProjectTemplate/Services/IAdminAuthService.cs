using InterviewProjectTemplate.DTOs;

namespace InterviewProjectTemplate.Services
{
    public interface IAdminAuthService
    {
        Task<AdminLoginResult> LoginAsync(AdminLoginRequest request);

        Task LogoutAsync();
    }
}
