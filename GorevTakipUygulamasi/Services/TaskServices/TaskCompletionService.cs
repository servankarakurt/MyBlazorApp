using GorevTakipUygulamasi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace GorevTakipUygulamasi.Services.TaskServices
{
    public class TaskCompletionService : ITaskCompletionService
    {
        private readonly ITaskLogicAppService _taskLogicAppService;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<TaskCompletionService> _logger;

        public TaskCompletionService(
            ITaskLogicAppService taskLogicAppService,
            UserManager<IdentityUser> userManager,
            ILogger<TaskCompletionService> logger)
        {
            _taskLogicAppService = taskLogicAppService;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task ProcessTaskCompletionAsync(TaskItem completedTask)
        {
            try
            {
                _logger.LogInformation("🎯 Görev tamamlanma işlemi başlıyor: {TaskId} - {TaskTitle}",
                    completedTask.Id, completedTask.Title);

                // Kullanıcı bilgilerini al
                var user = await _userManager.FindByIdAsync(completedTask.UserId);
                if (user?.Email == null)
                {
                    _logger.LogWarning("⚠️ Kullanıcı bulunamadı veya e-posta adresi yok: {UserId}", completedTask.UserId);
                    return;
                }

                _logger.LogInformation("👤 Kullanıcı bilgileri:");
                _logger.LogInformation("   Email: {Email}", user.Email);
                _logger.LogInformation("   UserName: {UserName}", user.UserName);

                // Kullanıcı adını belirle
                var userName = user.UserName ?? user.Email.Split('@')[0];

                // Task Logic App Service kullanarak email gönder
                var success = await _taskLogicAppService.SendTaskCompletionEmailAsync(
                    completedTask,
                    user.Email,
                    userName
                );

                if (success)
                {
                    _logger.LogInformation("✅ Görev tamamlanma e-postası başarıyla gönderildi: {TaskId}", completedTask.Id);
                }
                else
                {
                    _logger.LogError("❌ Görev tamamlanma e-postası gönderilemedi: {TaskId}", completedTask.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Görev tamamlanma işlemi sırasında hata: {TaskId}", completedTask.Id);
            }
        }
    }
}