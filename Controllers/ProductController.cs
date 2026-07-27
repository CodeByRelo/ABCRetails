using ABCRetail.Interfaces;
using ABCRetail.Models;
using Microsoft.AspNetCore.Mvc;

namespace ABCRetail.Controllers
{
    public class ProductController : Controller
    {
        private readonly IProductTableService _productService;
        private readonly IBlobStorageService _blobService;
        private readonly IFileStorageService _fileService;

        public ProductController(
            IProductTableService productService,
            IBlobStorageService blobService,
            IFileStorageService fileService)
        {
            _productService = productService;
            _blobService = blobService;
            _fileService = fileService;
        }

        // =========================
        // Display all products
        // =========================

        public async Task<IActionResult> Index()
        {
            var products = await _productService.GetProductsAsync();

            return View(products);
        }

        // =========================
        // Create Product GET
        // =========================

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // =========================
        // Create Product POST
        // =========================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            Product product,
            IFormFile imageFile)
        {

            if (!ModelState.IsValid)
            {
                return View(product);
            }

            // Generate Product ID

            product.Id = Guid.NewGuid().ToString();

            // Upload Product Image

            if (imageFile != null && imageFile.Length > 0)
            {
                product.ImageUrl =
                    await _blobService.UploadImageAsync(imageFile);
            }

            // Save Product

            await _productService.AddProductAsync(product);

            // Write to Azure Files log

            await _fileService.WriteLogAsync(
$"""
EVENT: Product Created

Product ID : {product.Id}
Name       : {product.Name}
Category   : {product.Category}
Price      : R{product.Price:N2}
Stock      : {product.Stock}
""");

            TempData["Success"] =
                "Product added successfully.";

            return RedirectToAction(nameof(Index));
        }

        // =========================
        // Product Details
        // =========================

        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            var product =
                await _productService.GetProductAsync(id);

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // =========================
        // Delete Product GET
        // =========================

        [HttpGet]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            var product =
                await _productService.GetProductAsync(id);

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // =========================
        // Delete Product POST
        // =========================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Product product)
        {
            if (product == null ||
                string.IsNullOrWhiteSpace(product.Id))
            {
                return NotFound();
            }

            // Get existing product

            var existingProduct =
                await _productService.GetProductAsync(product.Id);

            // Delete associated image from Blob Storage

            if (existingProduct != null &&
                !string.IsNullOrEmpty(existingProduct.ImageUrl))
            {
                try
                {
                    var fileName =
                        Path.GetFileName(
                            new Uri(existingProduct.ImageUrl)
                            .AbsolutePath);

                    await _blobService.DeleteImageAsync(fileName);
                }
                catch
                {
                    // Ignore image deletion errors
                    // Product deletion should continue
                }
            }

            // Delete Product from Table Storage

            await _productService.DeleteProductAsync(product.Id);

            // Write to Azure Files log

            await _fileService.WriteLogAsync(
$"""
EVENT: Product Deleted

Product ID : {product.Id}
""");

            TempData["Success"] =
                "Product deleted successfully.";

            return RedirectToAction(nameof(Index));
        }
    }
}