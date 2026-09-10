using Azure.Storage.Queues;
using ABCRetail.Functions.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Text.Json;

namespace ABCRetail.Functions.Functions
{
    public class QueueFunction
    {
        private readonly QueueClient _queueClient;

        public QueueFunction()
        {
            var connectionString =
                Environment.GetEnvironmentVariable("StorageConnection");

            var queueName =
                Environment.GetEnvironmentVariable("QueueName");

            _queueClient = new QueueClient(
                connectionString,
                queueName);

            _queueClient.CreateIfNotExists();
        }

        [Function("QueueFunction")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")]
            HttpRequestData req)
        {
            var requestBody =
                await new StreamReader(req.Body).ReadToEndAsync();

            if (string.IsNullOrWhiteSpace(requestBody))
            {
                var badResponse =
                    req.CreateResponse(HttpStatusCode.BadRequest);

                await badResponse.WriteStringAsync(
                    "Order data cannot be empty.");

                return badResponse;
            }

            Order? order;

            try
            {
                order =
                    JsonSerializer.Deserialize<Order>(
                        requestBody,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
            }
            catch (JsonException)
            {
                var badResponse =
                    req.CreateResponse(HttpStatusCode.BadRequest);

                await badResponse.WriteStringAsync(
                    "Invalid order JSON.");

                return badResponse;
            }

            if (order == null ||
                string.IsNullOrWhiteSpace(order.CustomerId) ||
                string.IsNullOrWhiteSpace(order.ProductId) ||
                order.Quantity <= 0)
            {
                var badResponse =
                    req.CreateResponse(HttpStatusCode.BadRequest);

                await badResponse.WriteStringAsync(
                    "CustomerId, ProductId and a valid Quantity are required.");

                return badResponse;
            }

            order.TotalPrice =
                order.UnitPrice * order.Quantity;

            string message =
                JsonSerializer.Serialize(order);

            await _queueClient.SendMessageAsync(message);

            var response =
                req.CreateResponse(HttpStatusCode.OK);

            await response.WriteAsJsonAsync(new
            {
                message = "Order added to queue successfully.",
                queue = "order-processing",
                orderId = order.Id,
                status = order.Status
            });

            return response;
        }

        [Function("ReadQueueFunction")]
        public async Task<HttpResponseData> ReadQueue(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get")]
            HttpRequestData req)
        {
            var messages =
                await _queueClient.ReceiveMessagesAsync(
                    maxMessages: 1);

            var response =
                req.CreateResponse(HttpStatusCode.OK);

            if (messages.Value.Length == 0)
            {
                await response.WriteAsJsonAsync(new
                {
                    message = "No orders available in the queue."
                });

                return response;
            }

            var queueMessage =
                messages.Value[0];

            Order? order;

            try
            {
                order =
                    JsonSerializer.Deserialize<Order>(
                        queueMessage.MessageText,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
            }
            catch (JsonException)
            {
                await _queueClient.DeleteMessageAsync(
                    queueMessage.MessageId,
                    queueMessage.PopReceipt);

                var badResponse =
                    req.CreateResponse(
                        HttpStatusCode.InternalServerError);

                await badResponse.WriteStringAsync(
                    "Queue message contained invalid order data.");

                return badResponse;
            }

            await _queueClient.DeleteMessageAsync(
                queueMessage.MessageId,
                queueMessage.PopReceipt);

            await response.WriteAsJsonAsync(new
            {
                message = "Order read successfully.",
                queue = "order-processing",
                order
            });

            return response;
        }
    }
}