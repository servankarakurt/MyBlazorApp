using GorevTakipUygulamasi.Models;

namespace GorevTakipUygulamasi.Services.TaskServices
{
    public interface ITaskCompletionService
    {
        Task ProcessTaskCompletionAsync(TaskItem completedTask);
    }
}
