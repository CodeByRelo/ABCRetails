using ABCRetail.Configuration;
using ABCRetail.Interfaces;
using ABCRetail.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Configure Azure Storage Settings
builder.Services.Configure<StorageSettings>(
    builder.Configuration.GetSection("AzureStorage"));

// Register Azure Storage Services
builder.Services.AddScoped<ICustomerTableService, CustomerTableService>();
builder.Services.AddScoped<IProductTableService, ProductTableService>();
builder.Services.AddScoped<IBlobStorageService, BlobStorageService>();
builder.Services.AddScoped<IQueueStorageService, QueueStorageService>();
builder.Services.AddScoped<IFileStorageService, FileStorageService>();

builder.Services.AddHttpClient<FunctionService>((serviceProvider, client) =>
{
    var configuration =
        serviceProvider.GetRequiredService<IConfiguration>();

    var baseUrl =
        configuration["AzureFunctions:BaseUrl"];

    client.BaseAddress =
        new Uri(baseUrl!);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();