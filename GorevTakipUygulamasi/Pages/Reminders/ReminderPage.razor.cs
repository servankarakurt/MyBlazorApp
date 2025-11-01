using GorevTakipUygulamasi.Models;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace GorevTakipUygulamasi.Pages
{
    public partial class ReminderPage
    {
        private List<ReminderItem> reminders = new();
        private ReminderItem editingReminder = new();
        private bool isLoading = true;
        private bool showModal = false;
        private string? userId;

        protected override async Task OnInitializedAsync()
        {
            var authState = await AuthProvider.GetAuthenticationStateAsync();
            var user = authState.User;

            if (user.Identity is null || !user.Identity.IsAuthenticated)
            {
                Navigation.NavigateTo("/Identity/Account/Login?returnUrl=/hatirlatici");
                return;
            }

            userId = user.FindFirst(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                // Kullanıcı kimliği bulunamazsa bir hata durumu yönetilebilir
                return;
            }

            await LoadReminders();
        }

        private async Task LoadReminders()
        {
            isLoading = true;
            // ReminderService'den Azure Table Storage'daki verileri çekiyoruz.
            var serviceResponse = await ReminderService.GetUserRemindersAsync(userId!);
            if (serviceResponse.IsSuccess)
            {
                reminders = serviceResponse.Data ?? new List<ReminderItem>();
            }
            else
            {
                // Kullanıcıya bir hata mesajı gösterebiliriz.
                // Örnek: await JSRuntime.InvokeVoidAsync("alert", serviceResponse.Message);
            }
            isLoading = false;
        }

        private void OpenAddModal()
        {
            // Yeni bir hatırlatıcı eklemek için boş bir model oluşturuyoruz.
            editingReminder = new ReminderItem
            {
                ReminderDate = DateTime.Today,
                IsCompleted = false
            };
            showModal = true;
        }

        private void OpenEditModal(ReminderItem reminderToEdit)
        {
            // Mevcut bir hatırlatıcıyı düzenlemek için kopyasını alıyoruz.
            editingReminder = new ReminderItem
            {
                Id = reminderToEdit.Id,
                Title = reminderToEdit.Title,
                ReminderDate = reminderToEdit.ReminderDate,
                IsCompleted = reminderToEdit.IsCompleted
            };
            showModal = true;
        }

        private void CloseModal()
        {
            showModal = false;
        }

        private async Task SaveReminder()
        {
            if (string.IsNullOrWhiteSpace(editingReminder.Title))
            {
                await JSRuntime.InvokeVoidAsync("alert", "Başlık boş olamaz!");
                return;
            }

            // Eğer Id Guid.Empty değilse bu bir güncellemedir, aksi halde yeni kayıttır.
            if (editingReminder.Id != Guid.Empty)
            {
                // GÜNCELLEME İŞLEMİ
                await ReminderService.UpdateReminderAsync(editingReminder.Id, new UpdateReminderDto
                {
                    Title = editingReminder.Title,
                    Description = editingReminder.Description,
                    Date = DateOnly.FromDateTime(editingReminder.ReminderDate),
                    Time = TimeOnly.FromDateTime(editingReminder.ReminderDate),
                    EmailReminder = editingReminder.EmailReminder,
                    IsCompleted = editingReminder.IsCompleted
                }, userId!);
            }
            else
            {
                // YENİ KAYIT İŞLEMİ
                await ReminderService.CreateReminderAsync(new CreateReminderDto
                {
                    Title = editingReminder.Title,
                    Description = editingReminder.Description,
                    Date = DateOnly.FromDateTime(editingReminder.ReminderDate),
                    Time = TimeOnly.FromDateTime(editingReminder.ReminderDate),
                    EmailReminder = editingReminder.EmailReminder
                }, userId!);
            }

            CloseModal();
            await LoadReminders(); // Listeyi yenilemek için verileri tekrar çekiyoruz.
        }

        private async Task DeleteReminder(ReminderItem reminderToDelete)
        {
            // Silme onayı (isteğe bağlı ama önerilir)
            var confirmed = await JSRuntime.InvokeAsync<bool>("confirm", $"'{reminderToDelete.Title}' başlıklı hatırlatıcıyı silmek istediğinize emin misiniz?");
            if (confirmed)
            {
                await ReminderService.DeleteReminderAsync(reminderToDelete.Id, userId!);
                await LoadReminders(); // Listeyi yeniliyoruz.
            }
        }
    }
}