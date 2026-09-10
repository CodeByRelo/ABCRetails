using ABCRetail.Configuration;
using ABCRetail.Interfaces;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace ABCRetail.Services
{
    public class BlobStorageService : IBlobStorageService
    {
        private readonly BlobContainerClient _containerClient;

        public BlobStorageService(IOptions<StorageSettings> options)
        {
            var settings = options.Value;

            var blobServiceClient =
                new BlobServiceClient(settings.ConnectionString);

            _containerClient =
                blobServiceClient.GetBlobContainerClient(
                    settings.BlobContainerName);

            // Keep container private
            _containerClient.CreateIfNotExists();
        }

        // =========================
        // Upload Image
        // =========================

        public async Task<string> UploadImageAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("No file was provided.");

            var fileName =
                $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";

            BlobClient blobClient =
                _containerClient.GetBlobClient(fileName);

            using (var stream = file.OpenReadStream())
            {
                await blobClient.UploadAsync(
                    stream,
                    overwrite: true);
            }

            // Store only the filename in Azure Table Storage
            return fileName;
        }

        // =========================
        // Generate Secure Image URL
        // =========================

        public string GetImageUrl(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return string.Empty;

            return GenerateBlobSasUrl(fileName);
        }

        // =========================
        // Get All Images
        // =========================

        public async Task<List<string>> GetImagesAsync()
        {
            var images = new List<string>();

            await foreach (BlobItem blobItem in _containerClient.GetBlobsAsync())
            {
                images.Add(
                    GenerateBlobSasUrl(blobItem.Name));
            }

            return images;
        }

        // =========================
        // Delete Image
        // =========================

        public async Task DeleteImageAsync(string fileName)
        {
            BlobClient blobClient =
                _containerClient.GetBlobClient(fileName);

            await blobClient.DeleteIfExistsAsync();
        }

        // =========================
        // Generate SAS URL
        // =========================

        private string GenerateBlobSasUrl(string fileName)
        {
            BlobClient blobClient =
                _containerClient.GetBlobClient(fileName);

            if (!blobClient.CanGenerateSasUri)
            {
                throw new InvalidOperationException(
                    "Storage account does not support SAS generation.");
            }

            BlobSasBuilder sasBuilder =
                new BlobSasBuilder
                {
                    BlobContainerName = _containerClient.Name,
                    BlobName = fileName,
                    Resource = "b",

                    // Longer expiry for demonstration purposes
                    ExpiresOn = DateTimeOffset.UtcNow.AddDays(7)
                };

            sasBuilder.SetPermissions(
                BlobSasPermissions.Read);

            Uri sasUri =
                blobClient.GenerateSasUri(sasBuilder);

            return sasUri.ToString();
        }
    }
}