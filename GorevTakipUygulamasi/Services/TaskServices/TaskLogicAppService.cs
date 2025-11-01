using GorevTakipUygulamasi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace GorevTakipUygulamasi.Services.TaskServices
{
    public interface ITaskLogicAppService
    {
        Task<bool> SendTaskCompletionEmailAsync(TaskItem completedTask, string userEmail, string userName);
    }

    public class TaskLogicAppService : ITaskLogicAppService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<TaskLogicAppService> _logger;
        private readonly string _logicAppUrl;

        public TaskLogicAppService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<TaskLogicAppService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            // appsettings.json'dan Task için özel URL al
            _logicAppUrl = configuration["LogicApp:TaskCompletionUrl"]
                ?? configuration["LogicApp:SendEmailUrl"]
                ?? throw new InvalidOperationException("Logic App URL bulunamadı!");
        }

        public async Task<bool> SendTaskCompletionEmailAsync(TaskItem completedTask, string userEmail, string userName)
        {
            try
            {
                _logger.LogInformation("📧 Task completion email gönderiliyor...");
                _logger.LogInformation("   Task ID: {TaskId}", completedTask.Id);
                _logger.LogInformation("   Task Title: {Title}", completedTask.Title);
                _logger.LogInformation("   User Email: {Email}", userEmail);
                _logger.LogInformation("   User Name: {Name}", userName);

                // Logic App'in beklediği DOĞRU formatta payload oluştur
                var payload = new
                {
                    taskTitle = completedTask.Title,
                    taskDescription = completedTask.Description ?? "Açıklama yok",
                    userEmail = userEmail,
                    userName = userName,
                    completedDate = (completedTask.CompletedDate ?? DateTime.Now).ToString("dd.MM.yyyy HH:mm"),
                    taskId = completedTask.Id
                };

                var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                });

                _logger.LogInformation("📤 Logic App'e gönderilecek JSON:\n{Json}", json);
                _logger.LogInformation("🌐 Logic App URL: {Url}", _logicAppUrl);

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.LogInformation("⏳ HTTP POST isteği gönderiliyor...");

                var response = await _httpClient.PostAsync(_logicAppUrl, content);

                _logger.LogInformation("📥 HTTP Response alındı: {StatusCode}", response.StatusCode);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation("✅ Task completion email başarıyla gönderildi!");
                    _logger.LogInformation("   Response: {Response}", responseContent);
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("❌ Logic App hatası!");
                    _logger.LogError("   StatusCode: {StatusCode}", response.StatusCode);
                    _logger.LogError("   Error Content: {Error}", errorContent);
                    _logger.LogError("   Request JSON: {Json}", json);
                    return false;
                }
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "🌐 HTTP isteği hatası");
                _logger.LogError("   URL: {Url}", _logicAppUrl);
                _logger.LogError("   Message: {Message}", httpEx.Message);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Task completion email gönderilirken beklenmeyen hata");
                return false;
            }
        }
    }
}