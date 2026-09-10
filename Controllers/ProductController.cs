using ABCRetail.Interfaces;
using ABCRetail.Models;
using ABCRetail.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace ABCRetail.Controllers
{
    public class ProductController : Controller
    {
        private readonly IProductTableService _productService;
        private readonly IBlobStorageService _blobService;
        private readonly IFileStorageService _fileService;
        private readonly FunctionService _functionService;

        public ProductController(
            IProductTableService productService,
            IBlobStorageService blobService,
            IFileStorageService fileService,
            FunctionService functionService)
        {
            _productService = productService;
            _blobService = blobService;
            _fileService = fileService;
            _functionService = functionService;
        }

        // =========================
        // Display all products
        // =========================

        public async Task<IActionResult> Index()
        {
            var products =
                await _productService.GetProductsAsync();

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

            product.Id =
                Guid.NewGuid().ToString();

            // =========================
            // Upload Image through Function
            // =========================

            if (imageFile != null &&
                imageFile.Length > 0)
            {
                var imageResult =
                    await _functionService
                        .UploadProductImageAsync(imageFile);

                using var imageDocument =
                    JsonDocument.Parse(imageResult);

                product.ImageUrl =
                    imageDocument.RootElement
                        .GetProperty("fileName")
                        .GetString() ?? "";
            }

            // =========================
            // Store Product through Function
            // =========================

            await _functionService
                .StoreProductAsync(product);

            // =========================
            // Write Log through Function
            // =========================

            await _functionService.WriteLogAsync(
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

            var existingProduct =
                await _productService.GetProductAsync(product.Id);

            if (existingProduct != null &&
                !string.IsNullOrEmpty(existingProduct.ImageUrl))
            {
                try
                {
                    var fileName =
                        Path.GetFileName(
                            new Uri(existingProduct.ImageUrl)
                                .AbsolutePath);

                    await _blobService
                        .DeleteImageAsync(fileName);
                }
                catch
                {
                    // Product deletion continues
                    // if image deletion fails.
                }
            }

            await _productService
                .DeleteProductAsync(product.Id);

            await _functionService.WriteLogAsync(
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