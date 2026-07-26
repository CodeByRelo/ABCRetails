using ABCRetail.Configuration;
using ABCRetail.Interfaces;
using Azure;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace ABCRetail.Services
{
    public class FileStorageService : IFileStorageService
    {
        private readonly ShareClient _shareClient;
        private readonly ShareDirectoryClient _directoryClient;

        public FileStorageService(IOptions<StorageSettings> options)
        {
            var settings = options.Value;

            _shareClient = new ShareClient(
                settings.ConnectionString,
                settings.FileShareName);

            _shareClient.CreateIfNotExists();

            _directoryClient = _shareClient.GetRootDirectoryClient();
        }

        public async Task UploadFileAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("No file was provided.");

            ShareFileClient fileClient = _directoryClient.GetFileClient(file.FileName);

            await fileClient.CreateAsync(file.Length);

            using (var stream = file.OpenReadStream())
            {
                await fileClient.UploadRangeAsync(
                    new HttpRange(0, file.Length),
                    stream);
            }
        }

        public async Task<List<string>> GetFilesAsync()
        {
            var files = new List<string>();

            await foreach (ShareFileItem item in _directoryClient.GetFilesAndDirectoriesAsync())
            {
                if (!item.IsDirectory)
                {
                    files.Add(item.Name);
                }
            }

            return files;
        }

        public async Task<Stream> DownloadFileAsync(string fileName)
        {
            ShareFileClient fileClient =
                _directoryClient.GetFileClient(fileName);

            if (!await fileClient.ExistsAsync())
                throw new FileNotFoundException(
                    $"File '{fileName}' was not found.");

            ShareFileDownloadInfo download =
                await fileClient.DownloadAsync();

            MemoryStream stream = new MemoryStream();

            await download.Content.CopyToAsync(stream);

            stream.Position = 0;

            return stream;
        }

        public async Task DeleteFileAsync(string fileName)
        {
            ShareFileClient fileClient =
                _directoryClient.GetFileClient(fileName);

            await fileClient.DeleteIfExistsAsync();
        }

        public async Task WriteLogAsync(string message)
        {
            const string logFileName = "application-log.txt";

            ShareFileClient fileClient =
                _directoryClient.GetFileClient(logFileName);

            string existingContent = string.Empty;

            // Read existing log if it exists
            if (await fileClient.ExistsAsync())
            {
                ShareFileDownloadInfo download =
                    await fileClient.DownloadAsync();

                using var reader = new StreamReader(download.Content);
                existingContent = await reader.ReadToEndAsync();
            }

            // Create new log entry
            string logEntry =
            $"""
                ====================================================
                [{DateTime.Now:yyyy-MM-dd HH:mm:ss}]

                {message}

                ----------------------------------------------------

            """;

            string updatedContent = existingContent + logEntry;

            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(updatedContent);

            using var stream = new MemoryStream(bytes);

            // Create or overwrite the log file
            await fileClient.CreateAsync(bytes.Length);

            await fileClient.UploadRangeAsync(
                new HttpRange(0, bytes.Length),
                stream);
        }
    }
}