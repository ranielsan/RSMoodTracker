namespace InterviewProjectTemplate.DTOs
{
    public enum AdminLoginStatus
    {
        Success,
        InvalidCredentials,
        LockedOut,
        NotAllowed
    }

    public record AdminLoginResult(AdminLoginStatus Status);
}
