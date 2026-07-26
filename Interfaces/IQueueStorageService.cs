using ABCRetail.Models;

namespace ABCRetail.Interfaces
{
    public interface IQueueStorageService
    {
        Task SendOrderMessageAsync(Order order);

        Task<List<string>> PeekMessagesAsync();
    }
}