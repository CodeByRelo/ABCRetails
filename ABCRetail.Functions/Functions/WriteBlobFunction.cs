using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Text.Json;

namespace ABCRetail.Functions.Functions
{
    public class WriteBlobFunction
    {
        private readonly BlobContainerClient _containerClient;

        public WriteBlobFunction()
        {
            var connectionString =
                Environment.GetEnvironmentVariable("StorageConnection");

            var blobServiceClient =
                new BlobServiceClient(connectionString);

            var containerName =
                Environment.GetEnvironmentVariable("BlobContainerName");

            _containerClient =
                blobServiceClient.GetBlobContainerClient(containerName);

            _containerClient.CreateIfNotExists();
        }

        [Function("WriteBlobFunction")]
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
                    "Request body cannot be empty.");

                return badResponse;
            }

            ProductImageRequest? request;

            try
            {
                request =
                    JsonSerializer.Deserialize<ProductImageRequest>(
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
                string.IsNullOrWhiteSpace(request.FileName) ||
                string.IsNullOrWhiteSpace(request.ContentBase64))
            {
                var badResponse =
                    req.CreateResponse(HttpStatusCode.BadRequest);

                await badResponse.WriteStringAsync(
                    "FileName and ContentBase64 are required.");

                return badResponse;
            }

            byte[] imageBytes;

            try
            {
                imageBytes =
                    Convert.FromBase64String(request.ContentBase64);
            }
            catch (FormatException)
            {
                var badResponse =
                    req.CreateResponse(HttpStatusCode.BadRequest);

                await badResponse.WriteStringAsync(
                    "ContentBase64 is not valid.");

                return badResponse;
            }

            var extension =
                Path.GetExtension(request.FileName);

            if (string.IsNullOrWhiteSpace(extension))
            {
                extension = ".jpg";
            }

            var fileName =
                $"{Guid.NewGuid()}{extension}";

            var blobClient =
                _containerClient.GetBlobClient(fileName);

            using var stream =
                new MemoryStream(imageBytes);

            await blobClient.UploadAsync(
                stream,
                overwrite: true);

            var response =
                req.CreateResponse(HttpStatusCode.OK);

            await response.WriteAsJsonAsync(new
            {
                message = "Product image uploaded successfully.",
                container = "product-images",
                fileName = fileName
            });

            return response;
        }
    }

    public class ProductImageRequest
    {
        public string FileName { get; set; } = "";
        public string ContentBase64 { get; set; } = "";
    }
}