namespace GorevTakipUygulamasi.Services.TaskServices
{
    using GorevTakipUygulamasi.Models;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface ITaskService
    {
        Task<List<TaskItem>> GetUserTasksAsync(string userId);
        Task<List<TaskItem>> GetAllTasksAsync();
        Task<TaskItem?> GetTaskByIdAsync(int id, string userId);
        Task<TaskItem> CreateTaskAsync(TaskItem task);
        Task<TaskItem?> UpdateTaskAsync(TaskItem task);
        Task<bool> DeleteTaskAsync(int id, string userId);
        Task<bool> ChangeTaskStatusAsync(int id, string userId, Models.TaskStatus newStatus);
        Task<int> GetCompletedTaskCountAsync(string userId);
        Task<int> GetPendingTaskCountAsync(string userId);
    }
}
