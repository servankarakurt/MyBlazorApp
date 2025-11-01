using GorevTakipUygulamasi.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
// DEĞİŞİKLİK: 'System.Threading.Tasks.Task' için 'SystemTask' takma adı eklendi.
using SystemTask = System.Threading.Tasks.Task;

namespace GorevTakipUygulamasi.Services
{
    public class ReminderCheckService : IReminderCheckService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ReminderCheckService> _logger;
        private readonly ReminderNotificationService _notificationService;

        public ReminderCheckService(ApplicationDbContext context, ILogger<ReminderCheckService> logger, ReminderNotificationService notificationService)
        {
            _context = context;
            _logger = logger;
            _notificationService = notificationService;
        }

        public async SystemTask CheckAndProcessRemindersAsync() // DEĞİŞİKLİK: 'Task' -> 'SystemTask'
        {
            _logger.LogInformation("Hatırlatıcı kontrolü başladı.");
            var remindersToProcess = await _context.Reminders
                .Where(r => r.ReminderTime <= DateTime.UtcNow && !r.IsSent)
                .ToListAsync();

            foreach (var reminder in remindersToProcess)
            {
                // Bu kısmı ReminderNotificationService'inize göre düzenlemeniz gerekebilir.
                // Örneğin, Reminder yerine ReminderItem göndermek gibi.
                // bool success = await _notificationService.SendReminderAsync(reminder); 

                // Şimdilik sadece logluyoruz.
                _logger.LogInformation("{ReminderId} ID'li hatırlatıcı işleniyor.", reminder.Id);

                reminder.IsSent = true;
                _context.Update(reminder);
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("{Count} adet hatırlatıcı işlendi.", remindersToProcess.Count);
        }

        public async SystemTask CleanupExpiredRemindersAsync() // DEĞİŞİKLİK: 'Task' -> 'SystemTask'
        {
            // Bu metodun içeriğini ihtiyacınıza göre doldurabilirsiniz.
            _logger.LogInformation("Süresi dolmuş hatırlatıcı temizliği yapılacak.");
            await SystemTask.CompletedTask;
        }
    }
}