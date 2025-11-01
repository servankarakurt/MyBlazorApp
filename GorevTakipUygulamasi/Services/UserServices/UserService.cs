using Azure.Data.Tables;
using GorevTakipUygulamasi.Configuration;
using Microsoft.Extensions.Options;

namespace GorevTakipUygulamasi.Services.User
{
    public class UserService : IUserService
    {
        private readonly TableClient _tableClient;
        private readonly ILogger<UserService> _logger;

        public UserService(
            TableServiceClient tableServiceClient,
            IOptions<AzureStorageSettings> settings,
            ILogger<UserService> logger)
        {
            _tableClient = tableServiceClient.GetTableClient(settings.Value.UsersTableName);
            _logger = logger;
            _tableClient.CreateIfNotExists();
        }

        public async Task<string> GetUserEmailAsync(string userId)
        {
            try
            {
                var response = await _tableClient.GetEntityAsync<UserEntity>("user", userId);
                return response?.Value?.Email ?? "noreply@example.com";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı email'i alınırken hata: {UserId}", userId);
                return "noreply@example.com";
            }
        }

        public async Task<bool> IsValidUserAsync(string userId)
        {
            try
            {
                var response = await _tableClient.GetEntityAsync<UserEntity>("user", userId);
                return response?.Value != null;
            }
            catch
            {
                return false;
            }
        }

        public async Task<UserProfile?> GetUserProfileAsync(string userId)
        {
            try
            {
                var response = await _tableClient.GetEntityAsync<UserEntity>("user", userId);
                var entity = response?.Value;

                if (entity == null) return null;

                return new UserProfile
                {
                    UserId = entity.RowKey,
                    Email = entity.Email,
                    Name = entity.Name,
                    EmailNotificationsEnabled = entity.EmailNotificationsEnabled
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı profili alınırken hata: {UserId}", userId);
                return null;
            }
        }
    }

    // User entity for Azure Table Storage
    public class UserEntity : ITableEntity
    {
        public string PartitionKey { get; set; } = "";
        public string RowKey { get; set; } = "";
        public DateTimeOffset? Timestamp { get; set; }
        public Azure.ETag ETag { get; set; }

        public string Email { get; set; } = "";
        public string Name { get; set; } = "";
        public bool EmailNotificationsEnabled { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
    }
}