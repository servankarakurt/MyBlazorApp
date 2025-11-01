using GorevTakipUygulamasi.Models;

namespace GorevTakipUygulamasi.Services.ReminderServices
{
    public interface IReminderService
    {
        Task<List<ReminderItem>> GetActiveRemindersAsync();
        Task<ReminderItem?> GetReminderByIdAsync(Guid id);
        
        // CreateReminderAsync - CreateReminderDto kullanarak
        Task<ServiceResponse<ReminderItem>> CreateReminderAsync(CreateReminderDto createDto, string userId);
        
        // UpdateReminderAsync - UpdateReminderDto kullanarak
        Task<ServiceResponse<ReminderItem>> UpdateReminderAsync(Guid id, UpdateReminderDto updateDto, string userId);
        
        // DeleteReminderAsync
        Task<ServiceResponse<bool>> DeleteReminderAsync(Guid id, string userId);
        
        Task<List<ReminderItem>> GetRemindersByUserIdAsync(string userId);
        Task<List<ReminderItem>> GetDueRemindersAsync();
        
        // GetUserRemindersAsync metodu - ServiceResponse ile
        Task<ServiceResponse<List<ReminderItem>>> GetUserRemindersAsync(string userId);
    }
}