using SystemTask = System.Threading.Tasks.Task;

namespace GorevTakipUygulamasi.Services
{
    public interface IReminderCheckService
    {
        SystemTask CheckAndProcessRemindersAsync(); // DEĞİŞİKLİK: 'Task' -> 'SystemTask'
        SystemTask CleanupExpiredRemindersAsync();
    }
}