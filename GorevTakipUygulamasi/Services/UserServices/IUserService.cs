namespace GorevTakipUygulamasi.Services.User
{
    public interface IUserService
    {
        Task<string> GetUserEmailAsync(string userId);
        Task<bool> IsValidUserAsync(string userId);
        Task<UserProfile?> GetUserProfileAsync(string userId);
    }
    public class UserProfile
    {
        public string UserId { get; set; } = "";
        public string Email { get; set; } = "";
        public string Name { get; set; } = "";
        public TimeZoneInfo TimeZone { get; set; } = TimeZoneInfo.Local;
        public bool EmailNotificationsEnabled { get; set; } = true;
    }

}
