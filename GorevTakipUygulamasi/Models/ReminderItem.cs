using Azure;
using Azure.Data.Tables;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GorevTakipUygulamasi.Models
{
    [Table("Reminders")]
    public class ReminderItem
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Başlık gereklidir")]
        [StringLength(100, ErrorMessage = "Başlık en fazla 100 karakter olabilir")]
        public string Title { get; set; } = "";

        [StringLength(500, ErrorMessage = "Açıklama en fazla 500 karakter olabilir")]
        public string? Description { get; set; }

        public DateOnly Date { get; set; }

        public TimeOnly Time { get; set; }

        // Uyumluluk için ReminderDate property'si eklendi
        public DateTime ReminderDate
        {
            get => Date.ToDateTime(Time);
            set
            {
                Date = DateOnly.FromDateTime(value);
                Time = TimeOnly.FromDateTime(value);
            }
        }

        // Uyumluluk için ReminderTime property'si eklendi  
        public DateTime ReminderTime => ReminderDate;

        // Uyumluluk için DueDate property'si eklendi
        public DateTime DueDate
        {
            get => Date.ToDateTime(Time);
            set
            {
                Date = DateOnly.FromDateTime(value);
                Time = TimeOnly.FromDateTime(value);
            }
        }

        public bool EmailReminder { get; set; } = true;

        public bool IsCompleted { get; set; } = false;

        // Uyumluluk için IsActive property'si eklendi
        public bool IsActive { get; set; } = true;

        // Uyumluluk için NotificationFrequencyMinutes property'si eklendi
        public int NotificationFrequencyMinutes { get; set; } = 60;

        // Uyumluluk için IsSent property'si eklendi
        public bool IsSent
        {
            get => EmailSent;
            set => EmailSent = value;
        }

        public string UserId { get; set; } = "";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public DateTime? CompletedAt { get; set; }

        // Email gönderilme durumu
        public bool EmailSent { get; set; } = false;

        public DateTime? EmailSentAt { get; set; }

        // Hatırlatıcı durumu
        public ReminderStatus Status { get; set; } = ReminderStatus.Active;
    }

    // Azure Table Storage için Entity
    public class ReminderEntity : ITableEntity
    {
        public string PartitionKey { get; set; } = "";
        public string RowKey { get; set; } = "";
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        // Hatırlatıcı özellikleri
        public string Title { get; set; } = "";
        public string? Description { get; set; }
        public string Date { get; set; } = ""; // YYYY-MM-DD formatında
        public string Time { get; set; } = ""; // HH:mm formatında
        public bool EmailReminder { get; set; } = true;
        public bool IsCompleted { get; set; } = false;
        public bool IsActive { get; set; } = true;
        public int NotificationFrequencyMinutes { get; set; } = 60;
        public string UserId { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public bool EmailSent { get; set; } = false;
        public DateTime? EmailSentAt { get; set; }
        public string Status { get; set; } = "Active";

        // Uyumluluk için ek property'ler
        public DateTime ReminderTime
        {
            get => DateTime.Parse($"{Date} {Time}");
            set
            {
                Date = value.ToString("yyyy-MM-dd");
                Time = value.ToString("HH:mm");
            }
        }

        public DateTime DueDate
        {
            get => DateTime.Parse($"{Date} {Time}");
            set
            {
                Date = value.ToString("yyyy-MM-dd");
                Time = value.ToString("HH:mm");
            }
        }

        public bool IsSent
        {
            get => EmailSent;
            set => EmailSent = value;
        }

        // Parametresiz constructor
        public ReminderEntity() { }

        // ReminderItem'den dönüştürme constructor'ı
        public ReminderEntity(ReminderItem reminder, string userId)
        {
            PartitionKey = userId; // Kullanıcı bazlı partitioning
            RowKey = reminder.Id.ToString();
            Title = reminder.Title;
            Description = reminder.Description;
            Date = reminder.Date.ToString("yyyy-MM-dd");
            Time = reminder.Time.ToString("HH:mm");
            EmailReminder = reminder.EmailReminder;
            IsCompleted = reminder.IsCompleted;
            IsActive = reminder.IsActive;
            NotificationFrequencyMinutes = reminder.NotificationFrequencyMinutes;
            UserId = userId;
            CreatedAt = reminder.CreatedAt;
            UpdatedAt = reminder.UpdatedAt;
            CompletedAt = reminder.CompletedAt;
            EmailSent = reminder.EmailSent;
            EmailSentAt = reminder.EmailSentAt;
            Status = reminder.Status.ToString();
        }

        // ReminderItem'a dönüştürme
        public ReminderItem ToReminderItem()
        {
            return new ReminderItem
            {
                Id = Guid.Parse(RowKey),
                Title = Title,
                Description = Description,
                Date = DateOnly.Parse(Date),
                Time = TimeOnly.Parse(Time),
                EmailReminder = EmailReminder,
                IsCompleted = IsCompleted,
                IsActive = IsActive,
                NotificationFrequencyMinutes = NotificationFrequencyMinutes,
                UserId = UserId,
                CreatedAt = CreatedAt,
                UpdatedAt = UpdatedAt,
                CompletedAt = CompletedAt,
                EmailSent = EmailSent,
                EmailSentAt = EmailSentAt,
                Status = Enum.Parse<ReminderStatus>(Status)
            };
        }
    }

    // Hatırlatıcı durumu
    public enum ReminderStatus
    {
        Active,      // Aktif
        Completed,   // Tamamlandı
        Cancelled,   // İptal edildi
        Expired      // Süresi doldu
    }

    // API için DTO'lar
    public class CreateReminderDto
    {
        [Required(ErrorMessage = "Başlık gereklidir")]
        [StringLength(100, ErrorMessage = "Başlık en fazla 100 karakter olabilir")]
        public string Title { get; set; } = "";

        [StringLength(500, ErrorMessage = "Açıklama en fazla 500 karakter olabilir")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Tarih gereklidir")]
        public DateOnly Date { get; set; }

        [Required(ErrorMessage = "Saat gereklidir")]
        public TimeOnly Time { get; set; }

        public bool EmailReminder { get; set; } = true;
    }

    public class UpdateReminderDto
    {
        [Required(ErrorMessage = "Başlık gereklidir")]
        [StringLength(100, ErrorMessage = "Başlık en fazla 100 karakter olabilir")]
        public string Title { get; set; } = "";

        [StringLength(500, ErrorMessage = "Açıklama en fazla 500 karakter olabilir")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Tarih gereklidir")]
        public DateOnly Date { get; set; }

        [Required(ErrorMessage = "Saat gereklidir")]
        public TimeOnly Time { get; set; }

        public bool EmailReminder { get; set; } = true;

        public bool IsCompleted { get; set; } = false;
    }

    // Logic App için webhook modeli
    public class ReminderNotificationDto
    {
        public string ReminderId { get; set; } = "";
        public string UserId { get; set; } = "";
        public string UserEmail { get; set; } = "";
        public string Title { get; set; } = "";
        public string? Description { get; set; }
        public DateTime ScheduledDateTime { get; set; }
        public string NotificationType { get; set; } = "Email"; // Email, SMS, Push
    }

    // Servis yanıt modelleri
    public class ServiceResponse<T>
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = "";
        public T? Data { get; set; }
        public List<string> Errors { get; set; } = new();

        public static ServiceResponse<T> Success(T data, string message = "İşlem başarılı")
        {
            return new ServiceResponse<T>
            {
                IsSuccess = true,
                Message = message,
                Data = data
            };
        }

        public static ServiceResponse<T> Error(string message, List<string>? errors = null)
        {
            return new ServiceResponse<T>
            {
                IsSuccess = false,
                Message = message,
                Errors = errors ?? new List<string>()
            };
        }
    }

    // Takvim görünümü için yardımcı model
    public class CalendarDay
    {
        public DateTime Date { get; set; }
        public bool IsCurrentMonth { get; set; }
        public bool IsToday { get; set; }
        public bool HasReminders { get; set; }
        public int ReminderCount { get; set; }
        public List<ReminderItem> Reminders { get; set; } = new();
    }

    // Sayfalama için
    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }

    // Filtreleme için
    public class ReminderFilter
    {
        public string? UserId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool? IsCompleted { get; set; }
        public bool? EmailReminder { get; set; }
        public ReminderStatus? Status { get; set; }
        public string? SearchTerm { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }
}