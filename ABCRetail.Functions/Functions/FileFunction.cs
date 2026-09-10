using Azure;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Text;
using System.Text.Json;

namespace ABCRetail.Functions.Functions
{
    public class FileFunction
    {
        private readonly ShareClient _shareClient;

        public FileFunction()
        {
            var connectionString =
                Environment.GetEnvironmentVariable("StorageConnection");

            var fileShareName =
                Environment.GetEnvironmentVariable("FileShareName");

            _shareClient = new ShareClient(
                connectionString,
                fileShareName);

            _shareClient.CreateIfNotExists();
        }

        [Function("FileFunction")]
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
                    "Log message cannot be empty.");

                return badResponse;
            }

            LogRequest? request;

            try
            {
                request =
                    JsonSerializer.Deserialize<LogRequest>(
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
                    "Invalid JSON.");

                return badResponse;
            }

            if (request == null ||
                string.IsNullOrWhiteSpace(request.Message))
            {
                var badResponse =
                    req.CreateResponse(HttpStatusCode.BadRequest);

                await badResponse.WriteStringAsync(
                    "Message is required.");

                return badResponse;
            }

            const string logFileName =
                "application-log.txt";

            var directoryClient =
                _shareClient.GetRootDirectoryClient();

            var fileClient =
                directoryClient.GetFileClient(logFileName);

            string existingContent =
                string.Empty;

            if (await fileClient.ExistsAsync())
            {
                ShareFileDownloadInfo download =
                    await fileClient.DownloadAsync();

                using var reader =
                    new StreamReader(download.Content);

                existingContent =
                    await reader.ReadToEndAsync();
            }

            string logEntry =
                $"""
                ====================================================
                [{DateTime.Now:yyyy-MM-dd HH:mm:ss}]

                {request.Message}

                ----------------------------------------------------

                """;

            string updatedContent =
                existingContent + logEntry;

            byte[] bytes =
                Encoding.UTF8.GetBytes(updatedContent);

            using var stream =
                new MemoryStream(bytes);

            await fileClient.CreateAsync(bytes.Length);

            await fileClient.UploadRangeAsync(
                new HttpRange(0, bytes.Length),
                stream);

            var response =
                req.CreateResponse(HttpStatusCode.OK);

            await response.WriteAsJsonAsync(new
            {
                message = "Application log written successfully.",
                fileShare = "logs",
                fileName = logFileName
            });

            return response;
        }
    }

    public class LogRequest
    {
        public string Message { get; set; } = "";
    }
}