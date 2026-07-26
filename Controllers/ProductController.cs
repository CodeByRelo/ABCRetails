using ABCRetail.Interfaces;
using ABCRetail.Models;
using Microsoft.AspNetCore.Mvc;

namespace ABCRetail.Controllers
{
    public class ProductController : Controller
    {
        private readonly IProductTableService _productService;
        private readonly IBlobStorageService _blobService;


        public ProductController(
            IProductTableService productService,
            IBlobStorageService blobService)
        {
            _productService = productService;
            _blobService = blobService;
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
            Console.WriteLine("=================================");
            Console.WriteLine($"PRODUCT NAME: {product.Name}");
            Console.WriteLine($"PRODUCT PRICE: {product.Price}");
            Console.WriteLine($"PRODUCT STOCK: {product.Stock}");
            Console.WriteLine($"MODEL STATE VALID: {ModelState.IsValid}");
            Console.WriteLine("=================================");

            if (!ModelState.IsValid)
            {
                return View(product);
            }

            product.Id = Guid.NewGuid().ToString();

            if (imageFile != null && imageFile.Length > 0)
            {
                product.ImageUrl =
                    await _blobService.UploadImageAsync(imageFile);
            }

            await _productService.AddProductAsync(product);

            TempData["Success"] = "Product added successfully.";

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
            // to remove associated image

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



                    await _blobService.DeleteImageAsync(fileName);

                }
                catch
                {
                    // Ignore image deletion errors
                    // Product deletion should continue
                }
            }





            // Delete from Azure Table Storage

            await _productService.DeleteProductAsync(product.Id);




            TempData["Success"] =
                "Product deleted successfully.";


            return RedirectToAction(nameof(Index));
        }

    }
}