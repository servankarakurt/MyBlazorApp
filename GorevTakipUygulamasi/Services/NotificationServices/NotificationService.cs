using GorevTakipUygulamasi.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SystemTask = System.Threading.Tasks.Task;

namespace GorevTakipUygulamasi.Services
{
    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(ApplicationDbContext context, ILogger<NotificationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async SystemTask SendDailySummaryEmailsAsync()
        {
            _logger.LogInformation("Günlük özet e-postaları gönderimi başlıyor.");

            // Identity User'ları almak için doğru tablo adını kullanıyoruz
            var users = await _context.Users.ToListAsync();

            foreach (var user in users)
            {
                var tasksDueToday = await _context.TaskItems
                    .Where(t => t.UserId == user.Id &&
                               t.DueDate.HasValue &&
                               t.DueDate.Value.Date == DateTime.UtcNow.Date &&
                               t.Status != Models.TaskStatus.Tamamlandi)
                    .ToListAsync();

                if (tasksDueToday.Any())
                {
                    var subject = "Bugün Yapılacak Görevleriniz";
                    var message = "Merhaba, bugün tamamlamanız gereken görevler aşağıdadır:\n\n";
                    tasksDueToday.ForEach(t => message += $"- {t.Title}\n");

                    _logger.LogInformation("{UserId} ID'li kullanıcıya e-posta gönderiliyor: {Subject}", user.Id, subject);
                    // await _emailSender.SendEmailAsync(user.Email, subject, message);
                }
            }

            _logger.LogInformation("Günlük özet e-postaları gönderimi tamamlandı.");
        }

        // Görev tamamlama bildirimi için yeni method
        public async SystemTask SendTaskCompletionNotificationAsync(string taskTitle, string taskDescription, string userEmail)
        {
            try
            {
                _logger.LogInformation("Görev tamamlama bildirimi gönderiliyor: {TaskTitle} -> {Email}", taskTitle, userEmail);

                var subject = $"🎉 Görev Tamamlandı: {taskTitle}";
                var message = $"Tebrikler! '{taskTitle}' görevini başarıyla tamamladınız.\n\n";

                if (!string.IsNullOrWhiteSpace(taskDescription))
                {
                    message += $"Açıklama: {taskDescription}\n\n";
                }

                message += $"Tamamlanma Tarihi: {DateTime.Now:dd.MM.yyyy HH:mm}\n";
                message += "\nBu otomatik bir bildirimdir.";

                // Burada gerçek email servisi entegre edilecek
                // await _emailSender.SendEmailAsync(userEmail, subject, message);

                _logger.LogInformation("Görev tamamlama bildirimi gönderildi: {Email}", userEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Görev tamamlama bildirimi gönderilirken hata: {TaskTitle}", taskTitle);
            }
        }

        // Hatırlatıcı bildirimi için yeni method
        public async SystemTask SendReminderNotificationAsync(string reminderTitle, string reminderDescription, string userEmail, DateTime scheduledTime)
        {
            try
            {
                _logger.LogInformation("Hatırlatıcı bildirimi gönderiliyor: {ReminderTitle} -> {Email}", reminderTitle, userEmail);

                var subject = $"🔔 Hatırlatıcı: {reminderTitle}";
                var message = $"Merhaba!\n\n";
                message += $"'{reminderTitle}' hatırlatıcınızın zamanı geldi.\n\n";

                if (!string.IsNullOrWhiteSpace(reminderDescription))
                {
                    message += $"Detay: {reminderDescription}\n\n";
                }

                message += $"Planlanan Zaman: {scheduledTime:dd.MM.yyyy HH:mm}\n";
                message += "\nBu otomatik bir hatırlatıcıdır.";

                // Burada gerçek email servisi entegre edilecek
                // await _emailSender.SendEmailAsync(userEmail, subject, message);

                _logger.LogInformation("Hatırlatıcı bildirimi gönderildi: {Email}", userEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Hatırlatıcı bildirimi gönderilirken hata: {ReminderTitle}", reminderTitle);
            }
        }
    }

    public interface INotificationService
    {
        SystemTask SendDailySummaryEmailsAsync();
        SystemTask SendTaskCompletionNotificationAsync(string taskTitle, string taskDescription, string userEmail);
        SystemTask SendReminderNotificationAsync(string reminderTitle, string reminderDescription, string userEmail, DateTime scheduledTime);
    }
}