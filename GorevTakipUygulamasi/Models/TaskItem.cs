using System.ComponentModel.DataAnnotations;

namespace GorevTakipUygulamasi.Models
{
    public class TaskItem
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Görev başlığı zorunludur")]
        [StringLength(200, ErrorMessage = "Başlık en fazla 200 karakter olabilir")]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Açıklama en fazla 1000 karakter olabilir")]
        public string? Description { get; set; }

        public TaskStatus Status { get; set; } = TaskStatus.Beklemede;

        [Display(Name = "Son Teslim Tarihi")]
        public DateTime? DueDate { get; set; }

        [Display(Name = "Oluşturulma Tarihi")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Display(Name = "Tamamlanma Tarihi")]
        public DateTime? CompletedDate { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;
    }

    public enum TaskStatus
    {
        [Display(Name = "Beklemede")]
        Beklemede = 0,

        [Display(Name = "Devam Ediyor")]
        DevamEdiyor = 1,

        [Display(Name = "Tamamlandı")]
        Tamamlandi = 2
    }
}
