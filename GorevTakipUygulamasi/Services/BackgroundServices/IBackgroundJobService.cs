namespace GorevTakipUygulamasi.Services.Background
{
    public interface IBackgroundJobService
    {
        System.Threading.Tasks.Task ProcessPendingEmailRemindersAsync();
        System.Threading.Tasks.Task CleanupExpiredRemindersAsync();
        System.Threading.Tasks.Task SendDailySummaryEmailsAsync();
    }
}