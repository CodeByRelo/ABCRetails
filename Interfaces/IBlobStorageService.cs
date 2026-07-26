using Microsoft.AspNetCore.Http;

namespace ABCRetail.Interfaces
{
    public interface IBlobStorageService
    {
        Task<string> UploadImageAsync(IFormFile file);

        Task<List<string>> GetImagesAsync();

        Task DeleteImageAsync(string fileName);
    }
}