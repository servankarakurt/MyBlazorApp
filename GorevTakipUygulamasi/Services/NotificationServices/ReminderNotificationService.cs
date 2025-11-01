using GorevTakipUygulamasi.Configuration;
using GorevTakipUygulamasi.Models;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;


namespace GorevTakipUygulamasi.Services
{
    public class ReminderNotificationService
    {
        private readonly HttpClient _httpClient;
        private readonly ReminderNotificationSettings _settings;
        private readonly ILogger<ReminderNotificationService> _logger;

        public ReminderNotificationService(
            HttpClient httpClient,
            IOptions<ReminderNotificationSettings> settings,
            ILogger<ReminderNotificationService> logger)
        {
            _httpClient = httpClient;
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<bool> SendReminderAsync(ReminderItem reminder)
        {
            try
            {
                var json = JsonSerializer.Serialize(reminder);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(_settings.SendReminderUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Hatırlatıcı Logic App'e gönderildi: {ReminderId}", reminder.Id);
                    return true;
                }
                else
                {
                    _logger.LogError("Logic App'e hatırlatıcı gönderilirken hata: {StatusCode}", response.StatusCode);
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
                var cancelRequest = new { ReminderId = reminderId.ToString() };
                var json = JsonSerializer.Serialize(cancelRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(_settings.CancelReminderUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Hatırlatıcı iptali Logic App'e gönderildi: {ReminderId}", reminderId);
                    return true;
                }
                else
                {
                    _logger.LogError("Logic App iptal hatası: {StatusCode}", response.StatusCode);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Logic App iptali sırasında hata: {ReminderId}", reminderId);
                return false;
            }
        }
    }
}
