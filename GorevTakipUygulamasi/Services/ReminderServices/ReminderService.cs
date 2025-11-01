using GorevTakipUygulamasi.Data;
using GorevTakipUygulamasi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GorevTakipUygulamasi.Services.ReminderServices
{
    public class ReminderService : IReminderService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ReminderService> _logger;

        public ReminderService(ApplicationDbContext context, ILogger<ReminderService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<ReminderItem>> GetActiveRemindersAsync()
        {
            return await _context.Reminders
                .Where(r => r.IsActive && !r.IsCompleted)
                .OrderBy(r => r.Date)
                .ThenBy(r => r.Time)
                .ToListAsync();
        }

        public async Task<ReminderItem?> GetReminderByIdAsync(Guid id)
        {
            return await _context.Reminders
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        // CreateReminderAsync - CreateReminderDto kullanarak
        public async Task<ServiceResponse<ReminderItem>> CreateReminderAsync(CreateReminderDto createDto, string userId)
        {
            try
            {
                _logger.LogInformation($"Creating reminder for user: {userId}");

                var reminder = new ReminderItem
                {
                    Id = Guid.NewGuid(), // Yeni Guid oluştur
                    Title = createDto.Title?.Trim() ?? "",
                    Description = createDto.Description?.Trim(),
                    Date = createDto.Date,
                    Time = createDto.Time,
                    EmailReminder = createDto.EmailReminder,
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true,
                    IsCompleted = false,
                    EmailSent = false,
                    NotificationFrequencyMinutes = 60,
                    Status = ReminderStatus.Active
                };

                // Validation
                if (string.IsNullOrWhiteSpace(reminder.Title))
                {
                    return ServiceResponse<ReminderItem>.Error("Başlık gereklidir.");
                }

                if (reminder.Title.Length > 100)
                {
                    return ServiceResponse<ReminderItem>.Error("Başlık en fazla 100 karakter olabilir.");
                }

                if (!string.IsNullOrEmpty(reminder.Description) && reminder.Description.Length > 500)
                {
                    return ServiceResponse<ReminderItem>.Error("Açıklama en fazla 500 karakter olabilir.");
                }

                // Geçmiş tarih kontrolü
                var reminderDateTime = reminder.Date.ToDateTime(reminder.Time);
                if (reminderDateTime < DateTime.Now.AddMinutes(-5)) // 5 dakika tolerans
                {
                    return ServiceResponse<ReminderItem>.Error("Hatırlatıcı tarihi geçmişte olamaz.");
                }

                _context.Reminders.Add(reminder);

                _logger.LogInformation($"Saving reminder to database: {reminder.Id}");
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Reminder created successfully: {reminder.Id}");
                return ServiceResponse<ReminderItem>.Success(reminder, "Hatırlatıcı başarıyla oluşturuldu.");
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error while creating reminder");
                var innerMessage = dbEx.InnerException?.Message ?? dbEx.Message;
                return ServiceResponse<ReminderItem>.Error($"Veritabanı hatası: {innerMessage}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while creating reminder");
                return ServiceResponse<ReminderItem>.Error($"Hatırlatıcı oluşturulurken hata: {ex.Message}");
            }
        }

        // UpdateReminderAsync - UpdateReminderDto kullanarak
        public async Task<ServiceResponse<ReminderItem>> UpdateReminderAsync(Guid id, UpdateReminderDto updateDto, string userId)
        {
            try
            {
                _logger.LogInformation($"Updating reminder: {id} for user: {userId}");

                var existingReminder = await _context.Reminders
                    .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

                if (existingReminder == null)
                {
                    return ServiceResponse<ReminderItem>.Error("Hatırlatıcı bulunamadı veya erişim reddedildi.");
                }

                // Validation
                if (string.IsNullOrWhiteSpace(updateDto.Title))
                {
                    return ServiceResponse<ReminderItem>.Error("Başlık gereklidir.");
                }

                if (updateDto.Title.Length > 100)
                {
                    return ServiceResponse<ReminderItem>.Error("Başlık en fazla 100 karakter olabilir.");
                }

                if (!string.IsNullOrEmpty(updateDto.Description) && updateDto.Description.Length > 500)
                {
                    return ServiceResponse<ReminderItem>.Error("Açıklama en fazla 500 karakter olabilir.");
                }

                // Güncelleme işlemi
                existingReminder.Title = updateDto.Title.Trim();
                existingReminder.Description = updateDto.Description?.Trim();
                existingReminder.Date = updateDto.Date;
                existingReminder.Time = updateDto.Time;
                existingReminder.EmailReminder = updateDto.EmailReminder;
                existingReminder.IsCompleted = updateDto.IsCompleted;
                existingReminder.UpdatedAt = DateTime.UtcNow;

                if (updateDto.IsCompleted && existingReminder.CompletedAt == null)
                {
                    existingReminder.CompletedAt = DateTime.UtcNow;
                    existingReminder.Status = ReminderStatus.Completed;
                }
                else if (!updateDto.IsCompleted && existingReminder.CompletedAt != null)
                {
                    existingReminder.CompletedAt = null;
                    existingReminder.Status = ReminderStatus.Active;
                }

                _context.Entry(existingReminder).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Reminder updated successfully: {id}");
                return ServiceResponse<ReminderItem>.Success(existingReminder, "Hatırlatıcı başarıyla güncellendi.");
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error while updating reminder");
                var innerMessage = dbEx.InnerException?.Message ?? dbEx.Message;
                return ServiceResponse<ReminderItem>.Error($"Veritabanı hatası: {innerMessage}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error while updating reminder: {id}");
                return ServiceResponse<ReminderItem>.Error($"Hatırlatıcı güncellenirken hata: {ex.Message}");
            }
        }

        // DeleteReminderAsync
        public async Task<ServiceResponse<bool>> DeleteReminderAsync(Guid id, string userId)
        {
            try
            {
                _logger.LogInformation($"Deleting reminder: {id} for user: {userId}");

                var reminder = await _context.Reminders
                    .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

                if (reminder == null)
                {
                    return ServiceResponse<bool>.Error("Hatırlatıcı bulunamadı veya erişim reddedildi.");
                }

                _context.Reminders.Remove(reminder);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Reminder deleted successfully: {id}");
                return ServiceResponse<bool>.Success(true, "Hatırlatıcı başarıyla silindi.");
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error while deleting reminder");
                var innerMessage = dbEx.InnerException?.Message ?? dbEx.Message;
                return ServiceResponse<bool>.Error($"Veritabanı hatası: {innerMessage}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error while deleting reminder: {id}");
                return ServiceResponse<bool>.Error($"Hatırlatıcı silinirken hata: {ex.Message}");
            }
        }

        public async Task<List<ReminderItem>> GetRemindersByUserIdAsync(string userId)
        {
            return await _context.Reminders
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        // GetUserRemindersAsync metodu - ServiceResponse ile
        public async Task<ServiceResponse<List<ReminderItem>>> GetUserRemindersAsync(string userId)
        {
            try
            {
                _logger.LogInformation($"Getting reminders for user: {userId}");

                var reminders = await _context.Reminders
                    .Where(r => r.UserId == userId)
                    .OrderBy(r => r.Date)
                    .ThenBy(r => r.Time)
                    .ToListAsync();

                _logger.LogInformation($"Found {reminders.Count} reminders for user: {userId}");
                return ServiceResponse<List<ReminderItem>>.Success(reminders, "Hatırlatıcılar başarıyla getirildi.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error while getting reminders for user: {userId}");
                return ServiceResponse<List<ReminderItem>>.Error($"Hatırlatıcılar getirilirken hata: {ex.Message}");
            }
        }

        public async Task<List<ReminderItem>> GetDueRemindersAsync()
        {
            return await _context.Reminders
                .Where(r => r.IsActive && !r.IsCompleted)
                .Where(r => EF.Functions.DateFromParts(r.Date.Year, r.Date.Month, r.Date.Day)
                    .Add(TimeSpan.FromTicks(r.Time.Ticks)) <= DateTime.Now)
                .OrderBy(r => r.Date)
                .ThenBy(r => r.Time)
                .ToListAsync();
        }
    }
}