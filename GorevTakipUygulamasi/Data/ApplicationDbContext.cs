using GorevTakipUygulamasi.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace GorevTakipUygulamasi.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<TaskItem> TaskItems { get; set; }
        public DbSet<ReminderItem> Reminders { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // DateOnly ve TimeOnly için converter'lar
            var dateOnlyConverter = new ValueConverter<DateOnly, DateTime>(
                d => d.ToDateTime(TimeOnly.MinValue),
                d => DateOnly.FromDateTime(d)
            );

            var timeOnlyConverter = new ValueConverter<TimeOnly, TimeSpan>(
                t => t.ToTimeSpan(),
                t => TimeOnly.FromTimeSpan(t)
            );

            // ReminderItem konfigürasyonu
            modelBuilder.Entity<ReminderItem>(entity =>
            {
                // Primary Key
                entity.HasKey(e => e.Id);

                // ID için Guid kullan
                entity.Property(e => e.Id)
                    .HasDefaultValueSql("NEWID()");

                // String alanlar
                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Description)
                    .HasMaxLength(500);

                entity.Property(e => e.UserId)
                    .IsRequired()
                    .HasMaxLength(450);

                // Date ve Time alanları
                entity.Property(e => e.Date)
                    .IsRequired()
                    .HasConversion(dateOnlyConverter); // DateOnly desteği

                entity.Property(e => e.Time)
                    .IsRequired()
                    .HasConversion(timeOnlyConverter); // TimeOnly desteği

                // DateTime alanları
                entity.Property(e => e.CreatedAt)
                    .IsRequired();

                entity.Property(e => e.UpdatedAt)
                    .IsRequired(false);

                entity.Property(e => e.CompletedAt)
                    .IsRequired(false);

                entity.Property(e => e.EmailSentAt)
                    .IsRequired(false);

                // Boolean alanlar
                entity.Property(e => e.IsActive)
                    .IsRequired()
                    .HasDefaultValue(true);

                entity.Property(e => e.IsCompleted)
                    .IsRequired()
                    .HasDefaultValue(false);

                entity.Property(e => e.EmailReminder)
                    .IsRequired()
                    .HasDefaultValue(true);

                entity.Property(e => e.EmailSent)
                    .IsRequired()
                    .HasDefaultValue(false);

                // Enum
                entity.Property(e => e.Status)
                    .IsRequired()
                    .HasDefaultValue(ReminderStatus.Active)
                    .HasConversion<string>();

                // Index'ler
                entity.HasIndex(e => e.UserId)
                    .HasDatabaseName("IX_Reminders_UserId");

                entity.HasIndex(e => new { e.UserId, e.Date })
                    .HasDatabaseName("IX_Reminders_UserId_Date");

                entity.HasIndex(e => e.IsCompleted)
                    .HasDatabaseName("IX_Reminders_IsCompleted");

                // Computed columns - EF Core'da hesaplanan alanlar
                entity.Ignore(e => e.DueDate);
                entity.Ignore(e => e.ReminderDate);
                entity.Ignore(e => e.ReminderTime);
                entity.Ignore(e => e.IsSent);
            });
        }
    }
}
