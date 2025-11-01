using GorevTakipUygulamasi.Models;

namespace GorevTakipUygulamasi.Services.LogicApp
{
    public interface ILogicAppService
    {
        Task<bool> ScheduleReminderAsync(ReminderItem reminder);
        Task<bool> CancelReminderAsync(Guid reminderId);
        Task<bool> SendImmediateEmailAsync(ReminderNotificationDto notification);
        Task<bool> TestConnectionAsync();
    }
}
