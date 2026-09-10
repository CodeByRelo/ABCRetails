using ABCRetail.Interfaces;
using ABCRetail.Models;
using Microsoft.AspNetCore.Mvc;

namespace ABCRetail.Controllers
{
    public class CustomerController : Controller
    {
        private readonly ICustomerTableService _customerService;
        private readonly IFileStorageService _fileService;

        public CustomerController(
            ICustomerTableService customerService,
            IFileStorageService fileService)
        {
            _customerService = customerService;
            _fileService = fileService;
        }

        // =========================
        // Display all customers
        // =========================

        public async Task<IActionResult> Index()
        {
            var customers = await _customerService.GetCustomersAsync();

            return View(customers);
        }

        // =========================
        // Create Customer
        // =========================

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Customer customer)
        {
            if (!ModelState.IsValid)
                return View(customer);

            customer.Id = Guid.NewGuid().ToString();

            await _customerService.AddCustomerAsync(customer);

            // Write to Azure Files log
            await _fileService.WriteLogAsync(
$"""
EVENT: Customer Created

Customer ID : {customer.Id}
Name        : {customer.FirstName} {customer.LastName}
Email       : {customer.Email}
Phone       : {customer.PhoneNumber}
""");

            TempData["Success"] = "Customer added successfully.";

            return RedirectToAction(nameof(Index));
        }

        // =========================
        // Customer Details
        // =========================

        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return NotFound();

            var customer = await _customerService.GetCustomerAsync(id);

            if (customer == null)
                return NotFound();

            return View(customer);
        }

        // =========================
        // Delete Customer
        // =========================

        [HttpGet]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return NotFound();

            var customer = await _customerService.GetCustomerAsync(id);

            if (customer == null)
                return NotFound();

            return View(customer);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Customer customer)
        {
            await _customerService.DeleteCustomerAsync(customer.Id);

            // Write to Azure Files log
            await _fileService.WriteLogAsync(
$"""
EVENT: Customer Deleted

Customer ID : {customer.Id}
""");

            TempData["Success"] = "Customer deleted successfully.";

            return RedirectToAction(nameof(Index));
        }
    }
}