using GorevTakipUygulamasi.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
// Artık DateOnly/TimeOnly converter'larına gerek kalmadığı için
// 'Microsoft.EntityFrameworkCore.Storage.ValueConversion' using'i kaldırılabilir.

namespace GorevTakipUygulamasi.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // SADECE GÖREVLER (TASKITEMS) KALDI
        public DbSet<TaskItem> TaskItems { get; set; }

        // public DbSet<ReminderItem> Reminders { get; set; } 
        // -> BU SATIR SİLİNDİ (Artık Azure Table Storage'da)

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // DateOnly, TimeOnly converter'ları ve
            // modelBuilder.Entity<ReminderItem> bloğunun tamamı
            // buradan kaldırıldı. Veritabanı artık Reminder'ları tanımıyor.
        }
    }
}