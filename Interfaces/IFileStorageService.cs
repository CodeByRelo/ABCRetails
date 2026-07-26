using Microsoft.AspNetCore.Http;

namespace ABCRetail.Interfaces
{
    public interface IFileStorageService
    {
        Task UploadFileAsync(IFormFile file);

        Task<List<string>> GetFilesAsync();

        Task<Stream> DownloadFileAsync(string fileName);

        Task DeleteFileAsync(string fileName);

        Task WriteLogAsync(string message);
    }
}