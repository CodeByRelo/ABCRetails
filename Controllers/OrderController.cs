using System.Text.Json;
using ABCRetail.Interfaces;
using ABCRetail.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ABCRetail.Controllers
{
    public class OrderController : Controller
    {
        private readonly IQueueStorageService _queueService;
        private readonly ICustomerTableService _customerService;
        private readonly IProductTableService _productService;
        private readonly IFileStorageService _fileService;

        public OrderController(
            IQueueStorageService queueService,
            ICustomerTableService customerService,
            IProductTableService productService,
            IFileStorageService fileService)
        {
            _queueService = queueService;
            _customerService = customerService;
            _productService = productService;
            _fileService = fileService;
        }

        // =========================
        // Display Orders
        // =========================

        public async Task<IActionResult> Index()
        {
            var messages = await _queueService.PeekMessagesAsync();

            var orders = new List<Order>();

            foreach (var message in messages)
            {
                try
                {
                    var order = JsonSerializer.Deserialize<Order>(message);

                    if (order != null)
                    {
                        orders.Add(order);
                    }
                }
                catch
                {
                    // Ignore invalid queue messages
                }
            }

            return View(orders);
        }

        // =========================
        // Create Order GET
        // =========================

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadDropDowns();

            return View();
        }

        // =========================
        // Create Order POST
        // =========================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Order order)
        {
            if (!ModelState.IsValid)
            {
                await LoadDropDowns();
                return View(order);
            }

            // Generate Order ID

            order.Id = Guid.NewGuid().ToString();

            // Retrieve Customer

            var customer =
                await _customerService.GetCustomerAsync(order.CustomerId);

            if (customer == null)
            {
                ModelState.AddModelError("", "Customer not found.");

                await LoadDropDowns();

                return View(order);
            }

            // Retrieve Product

            var product =
                await _productService.GetProductAsync(order.ProductId);

            if (product == null)
            {
                ModelState.AddModelError("", "Product not found.");

                await LoadDropDowns();

                return View(order);
            }

            // Populate Order Details

            order.CustomerName =
                $"{customer.FirstName} {customer.LastName}";

            order.ProductName =
                product.Name;

            order.UnitPrice =
                product.Price;

            order.TotalPrice =
                product.Price * order.Quantity;

            order.OrderDate =
                DateTime.Now;

            order.Status =
                "Pending";

            // Send Order to Azure Queue

            await _queueService.SendOrderMessageAsync(order);

            // Write to Azure Files log

            await _fileService.WriteLogAsync(
$"""
EVENT: Order Created

Order ID    : {order.Id}
Customer    : {order.CustomerName}
Product     : {order.ProductName}
Quantity    : {order.Quantity}
Unit Price  : R{order.UnitPrice:N2}
Total Price : R{order.TotalPrice:N2}
Status      : {order.Status}
""");

            TempData["Success"] =
                "Order placed successfully.";

            return RedirectToAction(nameof(Index));
        }

        // =========================
        // Helper Method
        // =========================

        private async Task LoadDropDowns()
        {
            var customers =
                await _customerService.GetCustomersAsync();

            var products =
                await _productService.GetProductsAsync();

            ViewBag.Customers =
                new SelectList(
                    customers,
                    "Id",
                    "FullName");

            ViewBag.Products =
                new SelectList(
                    products,
                    "Id",
                    "Name");
        }
    }
}