using GorevTakipUygulamasi.Models;
using Microsoft.Extensions.Options;
using GorevTakipUygulamasi.Configuration;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;

namespace GorevTakipUygulamasi.Services.LogicApp
{
    public class LogicAppService : ILogicAppService
    {
        private readonly HttpClient _httpClient;
        private readonly LogicAppSettings _settings;
        private readonly ILogger<LogicAppService> _logger;
        private readonly UserManager<IdentityUser> _userManager;

        public LogicAppService(
            HttpClient httpClient,
            IOptions<LogicAppSettings> settings,
            ILogger<LogicAppService> logger,
            UserManager<IdentityUser> userManager)
        {
            _httpClient = httpClient;
            _settings = settings.Value;
            _logger = logger;
            _userManager = userManager;
        }

        public async Task<bool> ScheduleReminderAsync(ReminderItem reminder)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(reminder.UserId);
                var userEmail = user?.Email ?? "noreply@example.com";

                var notification = new ReminderNotificationDto
                {
                    ReminderId = reminder.Id.ToString(),
                    UserId = reminder.UserId,
                    UserEmail = userEmail,
                    Title = reminder.Title,
                    Description = reminder.Description,
                    ScheduledDateTime = reminder.Date.ToDateTime(reminder.Time),
                    NotificationType = "Email"
                };

                // Logic App'inizin beklediği veri formatını oluştur
                var logicAppPayload = new
                {
                    reminderId = notification.ReminderId,
                    userId = notification.UserId,
                    userEmail = notification.UserEmail,
                    title = notification.Title,
                    description = notification.Description ?? "",
                    scheduledDateTime = notification.ScheduledDateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    notificationType = notification.NotificationType,
                    // Ek alanlar Logic App'inizin ihtiyacına göre
                    action = "schedule",
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                };

                var json = JsonSerializer.Serialize(logicAppPayload, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                _logger.LogInformation("Logic App'e gönderilecek JSON: {Json}", json);

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(_settings.ScheduleReminderUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation("Hatırlatıcı Logic App'e gönderildi: {ReminderId}, Response: {Response}",
                        reminder.Id, responseContent);
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Logic App hatası: {StatusCode} - {Error}", response.StatusCode, errorContent);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Logic App'e hatırlatıcı gönderilirken hata: {ReminderId}", reminder.Id);
                return false;
            }
        }

        public async Task<bool> CancelReminderAsync(Guid reminderId)
        {
            try
            {
                var cancelRequest = new
                {
                    reminderId = reminderId.ToString(),
                    action = "cancel",
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                };

                var json = JsonSerializer.Serialize(cancelRequest, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(_settings.CancelReminderUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Hatırlatıcı iptali Logic App'e gönderildi: {ReminderId}", reminderId);
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Logic App iptal hatası: {StatusCode} - {Error}", response.StatusCode, errorContent);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Logic App'e hatırlatıcı iptali gönderilirken hata: {ReminderId}", reminderId);
                return false;
            }
        }

        public async Task<bool> SendImmediateEmailAsync(ReminderNotificationDto notification)
        {
            try
            {
                var immediateEmailPayload = new
                {
                    reminderId = notification.ReminderId,
                    userId = notification.UserId,
                    userEmail = notification.UserEmail,
                    title = notification.Title,
                    description = notification.Description ?? "",
                    scheduledDateTime = notification.ScheduledDateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    notificationType = notification.NotificationType,
                    action = "sendImmediate",
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                };

                var json = JsonSerializer.Serialize(immediateEmailPayload, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(_settings.SendEmailUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Anlık email Logic App'e gönderildi: {ReminderId}", notification.ReminderId);
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Logic App email hatası: {StatusCode} - {Error}", response.StatusCode, errorContent);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Logic App'e anlık email gönderilirken hata: {ReminderId}", notification.ReminderId);
                return false;
            }
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                var testData = new
                {
                    action = "test",
                    message = "Connection test from ASP.NET Core app",
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                };

                var json = JsonSerializer.Serialize(testData, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                using var response = await _httpClient.PostAsync(_settings.TestConnectionUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation("Logic App bağlantı testi başarılı: {Response}", responseContent);
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Logic App bağlantı testi başarısız: {StatusCode} - {Error}",
                        response.StatusCode, errorContent);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Logic App bağlantı testi hatası");
                return false;
            }
        }
    }
}