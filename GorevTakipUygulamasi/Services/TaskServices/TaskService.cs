namespace GorevTakipUygulamasi.Services.TaskServices
{
    using GorevTakipUygulamasi.Data;
    using GorevTakipUygulamasi.Models;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.AspNetCore.Identity;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System;

    public class TaskService : ITaskService
    {
        private readonly ApplicationDbContext _context;
        private readonly ITaskCompletionService _taskCompletionService;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<TaskService> _logger;

        public TaskService(
            ApplicationDbContext context,
            ITaskCompletionService taskCompletionService,
            UserManager<IdentityUser> userManager,
            ILogger<TaskService> logger)
        {
            _context = context;
            _taskCompletionService = taskCompletionService;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<List<TaskItem>> GetUserTasksAsync(string userId)
        {
            return await _context.TaskItems
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.CreatedDate)
                .ToListAsync();
        }

        public async Task<List<TaskItem>> GetAllTasksAsync()
        {
            return await _context.TaskItems
                .OrderByDescending(t => t.CreatedDate)
                .ToListAsync();
        }

        public async Task<TaskItem?> GetTaskByIdAsync(int id, string userId)
        {
            return await _context.TaskItems
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
        }

        public async Task<TaskItem> CreateTaskAsync(TaskItem task)
        {
            task.CreatedDate = DateTime.Now;
            task.CompletedDate = null;

            _context.TaskItems.Add(task);
            await _context.SaveChangesAsync();
            return task;
        }

        public async Task<TaskItem?> UpdateTaskAsync(TaskItem task)
        {
            var existingTask = await _context.TaskItems
                .FirstOrDefaultAsync(t => t.Id == task.Id && t.UserId == task.UserId);

            if (existingTask == null)
                return null;

            var wasCompleted = existingTask.Status == Models.TaskStatus.Tamamlandi;

            existingTask.Title = task.Title;
            existingTask.Description = task.Description;
            existingTask.DueDate = task.DueDate;
            existingTask.Status = task.Status;

            // Görev yeni tamamlandıysa
            if (task.Status == Models.TaskStatus.Tamamlandi && !wasCompleted)
            {
                existingTask.CompletedDate = DateTime.Now;

                // Veritabanını önce kaydet
                await _context.SaveChangesAsync();

                // Sonra e-posta gönderme işlemini başlat (async olarak)
                _ = Task.Run(async () => await _taskCompletionService.ProcessTaskCompletionAsync(existingTask));
            }
            else if (task.Status != Models.TaskStatus.Tamamlandi && wasCompleted)
            {
                existingTask.CompletedDate = null;
                await _context.SaveChangesAsync();
            }
            else
            {
                await _context.SaveChangesAsync();
            }

            return existingTask;
        }

        public async Task<bool> DeleteTaskAsync(int id, string userId)
        {
            var task = await _context.TaskItems
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (task == null)
                return false;

            _context.TaskItems.Remove(task);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ChangeTaskStatusAsync(int id, string userId, Models.TaskStatus newStatus)
        {
            var task = await _context.TaskItems
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (task == null)
                return false;

            var wasCompleted = task.Status == Models.TaskStatus.Tamamlandi;
            task.Status = newStatus;

            // Görev yeni tamamlandıysa
            if (newStatus == Models.TaskStatus.Tamamlandi && !wasCompleted)
            {
                task.CompletedDate = DateTime.Now;

                // Veritabanını önce kaydet
                await _context.SaveChangesAsync();

                _logger.LogInformation("Görev durumu 'Tamamlandı' olarak güncellendi: {TaskId}", task.Id);

                // E-posta gönderme işlemini arka planda başlat
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _taskCompletionService.ProcessTaskCompletionAsync(task);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Task completion service hatası: {TaskId}", task.Id);
                    }
                });
            }
            else if (newStatus != Models.TaskStatus.Tamamlandi && wasCompleted)
            {
                task.CompletedDate = null;
                await _context.SaveChangesAsync();
            }
            else
            {
                await _context.SaveChangesAsync();
            }

            return true;
        }

        public async Task<int> GetCompletedTaskCountAsync(string userId)
        {
            return await _context.TaskItems
                .CountAsync(t => t.UserId == userId && t.Status == Models.TaskStatus.Tamamlandi);
        }

        public async Task<int> GetPendingTaskCountAsync(string userId)
        {
            return await _context.TaskItems
                .CountAsync(t => t.UserId == userId && t.Status != Models.TaskStatus.Tamamlandi);
        }
    }
}