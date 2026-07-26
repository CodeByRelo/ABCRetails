using System.Text.Json;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using ABCRetail.Configuration;
using ABCRetail.Interfaces;
using ABCRetail.Models;
using Microsoft.Extensions.Options;

namespace ABCRetail.Services
{
    public class QueueStorageService : IQueueStorageService
    {
        private readonly QueueClient _queueClient;

        public QueueStorageService(IOptions<StorageSettings> options)
        {
            var settings = options.Value;

            var queueServiceClient = new QueueServiceClient(
                settings.ConnectionString);

            _queueClient = queueServiceClient.GetQueueClient(
                settings.QueueName);

            _queueClient.CreateIfNotExists();
        }

        // =========================
        // Send Order to Queue
        // =========================

        public async Task SendOrderMessageAsync(Order order)
        {
            string json =
                JsonSerializer.Serialize(order);

            await _queueClient.SendMessageAsync(json);
        }

        // =========================
        // Peek Queue Messages
        // =========================

        public async Task<List<string>> PeekMessagesAsync()
        {
            var messages = new List<string>();

            PeekedMessage[] results =
                await _queueClient.PeekMessagesAsync(maxMessages: 32);

            foreach (var message in results)
            {
                messages.Add(message.MessageText);
            }

            return messages;
        }
    }
}