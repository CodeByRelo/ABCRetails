using ABCRetail.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ABCRetail.Controllers
{
    public class LogController : Controller
    {
        private readonly IFileStorageService _fileService;

        public LogController(IFileStorageService fileService)
        {
            _fileService = fileService;
        }

        // =========================
        // View All Log Files
        // =========================

        public async Task<IActionResult> Index()
        {
            var files = await _fileService.GetFilesAsync();

            return View(files);
        }

        // =========================
        // Download Log File
        // =========================

        [HttpGet]
        public async Task<IActionResult> Download(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return NotFound();
            }

            var stream = await _fileService.DownloadFileAsync(fileName);

            return File(
                stream,
                "text/plain",
                fileName);
        }

        // =========================
        // Delete Log File
        // =========================

        [HttpGet]
        public IActionResult Delete(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return NotFound();
            }

            ViewBag.FileName = fileName;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return NotFound();
            }

            await _fileService.DeleteFileAsync(fileName);

            TempData["Success"] =
                "Log file deleted successfully.";

            return RedirectToAction(nameof(Index));
        }
    }
}