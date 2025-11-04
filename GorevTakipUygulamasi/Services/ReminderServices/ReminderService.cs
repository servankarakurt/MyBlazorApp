using GorevTakipUygulamasi.Models;
using Microsoft.Extensions.Logging;
// YENİ using'ler (SQL'e ait olanlar kaldırıldı)
using Azure.Data.Tables;
using Azure; // ETag için
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace GorevTakipUygulamasi.Services.ReminderServices
{
    public class ReminderService : IReminderService
    {
        private readonly ILogger<ReminderService> _logger;
        private readonly TableClient _reminderTableClient;
        private const string TABLE_NAME = "Reminders"; // Logic App'in okuduğu tablo (Görsel 2)

        public ReminderService(
            TableServiceClient tableServiceClient, // Program.cs'ten gelir
            ILogger<ReminderService> logger)
        {
            _logger = logger;
            // "Reminders" adında bir tablo oluştur (varsa es geçer)
            tableServiceClient.CreateTableIfNotExists(TABLE_NAME);
            _reminderTableClient = tableServiceClient.GetTableClient(TABLE_NAME);
        }

        // YENİ: Azure Table'a kayıt yapan metod
        public async Task<ServiceResponse<ReminderItem>> CreateReminderAsync(CreateReminderDto createDto, string userId)
        {
            try
            {
                _logger.LogInformation($"Creating reminder for user: {userId} in Azure Table");

                var reminderEntity = new ReminderEntity
                {
                    PartitionKey = userId,
                    RowKey = Guid.NewGuid().ToString(),
                    Title = createDto.Title?.Trim() ?? "",
                    Description = createDto.Description?.Trim(),
                    Date = createDto.Date.ToString("yyyy-MM-dd"), // String format
                    Time = createDto.Time.ToString("HH:mm"), // String format
                    EmailReminder = createDto.EmailReminder,
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true,
                    IsCompleted = false,
                    EmailSent = false,
                    Status = ReminderStatus.Active.ToString()
                };

                // Geçmiş tarih kontrolü
                var reminderDateTime = createDto.Date.ToDateTime(createDto.Time);
                if (reminderDateTime < DateTime.Now.AddMinutes(-5))
                {
                    return ServiceResponse<ReminderItem>.Error("Hatırlatıcı tarihi geçmişte olamaz.");
                }

                await _reminderTableClient.AddEntityAsync(reminderEntity);

                _logger.LogInformation($"Reminder created successfully in Table: {reminderEntity.RowKey}");
                return ServiceResponse<ReminderItem>.Success(reminderEntity.ToReminderItem(), "Hatırlatıcı başarıyla oluşturuldu.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while creating reminder in Azure Table");
                return ServiceResponse<ReminderItem>.Error($"Hatırlatıcı oluşturulurken hata: {ex.Message}");
            }
        }

        // YENİ: Azure Table'dan okuyan metod
        public async Task<ServiceResponse<List<ReminderItem>>> GetUserRemindersAsync(string userId)
        {
            try
            {
                _logger.LogInformation($"Getting reminders for user: {userId} from Azure Table");
                var reminders = new List<ReminderItem>();

                var query = _reminderTableClient.QueryAsync<ReminderEntity>(r => r.PartitionKey == userId);

                await foreach (var entity in query)
                {
                    reminders.Add(entity.ToReminderItem());
                }

                _logger.LogInformation($"Found {reminders.Count} reminders for user: {userId}");
                return ServiceResponse<List<ReminderItem>>.Success(
                    reminders.OrderBy(r => r.Date).ThenBy(r => r.Time).ToList(),
                    "Hatırlatıcılar başarıyla getirildi.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error while getting reminders for user: {userId}");
                return ServiceResponse<List<ReminderItem>>.Error($"Hatırlatıcılar getirilirken hata: {ex.Message}");
            }
        }

        // YENİ: Azure Table'da güncelleyen metod
        public async Task<ServiceResponse<ReminderItem>> UpdateReminderAsync(Guid id, UpdateReminderDto updateDto, string userId)
        {
            try
            {
                var entity = await _reminderTableClient.GetEntityAsync<ReminderEntity>(userId, id.ToString());
                if (entity == null)
                    return ServiceResponse<ReminderItem>.Error("Hatırlatıcı bulunamadı.");

                var existingReminder = entity.Value;

                existingReminder.Title = updateDto.Title.Trim();
                existingReminder.Description = updateDto.Description?.Trim();
                existingReminder.Date = updateDto.Date.ToString("yyyy-MM-dd");
                existingReminder.Time = updateDto.Time.ToString("HH:mm");
                existingReminder.EmailReminder = updateDto.EmailReminder;
                existingReminder.IsCompleted = updateDto.IsCompleted;
                existingReminder.UpdatedAt = DateTime.UtcNow;
                existingReminder.Status = updateDto.IsCompleted ? ReminderStatus.Completed.ToString() : ReminderStatus.Active.ToString();

                await _reminderTableClient.UpsertEntityAsync(existingReminder, TableUpdateMode.Replace);

                _logger.LogInformation($"Reminder updated successfully: {id}");
                return ServiceResponse<ReminderItem>.Success(existingReminder.ToReminderItem(), "Hatırlatıcı başarıyla güncellendi.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error while updating reminder: {id}");
                return ServiceResponse<ReminderItem>.Error($"Hatırlatıcı güncellenirken hata: {ex.Message}");
            }
        }

        // YENİ: Azure Table'dan silen metod
        public async Task<ServiceResponse<bool>> DeleteReminderAsync(Guid id, string userId)
        {
            try
            {
                await _reminderTableClient.DeleteEntityAsync(userId, id.ToString());
                _logger.LogInformation($"Reminder deleted successfully: {id}");
                return ServiceResponse<bool>.Success(true, "Hatırlatıcı başarıyla silindi.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error while deleting reminder: {id}");
                return ServiceResponse<bool>.Error($"Hatırlatıcı silinirken hata: {ex.Message}");
            }
        }

        // --- Arayüzle Uyumlu Kalan Diğer Metodlar (Boş/Yönlendirme) ---

        public Task<List<ReminderItem>> GetRemindersByUserIdAsync(string userId)
        {
            _logger.LogWarning("Eski metod çağrıldı: GetRemindersByUserIdAsync. GetUserRemindersAsync'e yönlendiriliyor.");
            var result = GetUserRemindersAsync(userId).Result; // UI kırmamak için
            return Task.FromResult(result.Data ?? new List<ReminderItem>());
        }

        public Task<ReminderItem?> GetReminderByIdAsync(Guid id)
        {
            _logger.LogWarning("GetReminderByIdAsync (UserId'siz) Table Storage'da desteklenmiyor.");
            return Task.FromResult<ReminderItem?>(null);
        }

        public Task<List<ReminderItem>> GetActiveRemindersAsync()
        {
            _logger.LogWarning("GetActiveRemindersAsync logic'i Logic App'e taşındı.");
            return Task.FromResult(new List<ReminderItem>());
        }

        public Task<List<ReminderItem>> GetDueRemindersAsync()
        {
            _logger.LogWarning("GetDueRemindersAsync logic'i Logic App'e taşındı.");
            return Task.FromResult(new List<ReminderItem>());
        }
    }
}