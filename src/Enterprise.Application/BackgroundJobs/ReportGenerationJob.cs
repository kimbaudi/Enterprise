using Enterprise.Application.Common.Interfaces;
using Enterprise.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Enterprise.Application.BackgroundJobs;

public class ReportGenerationJob
{
    private readonly IRepository<Product> _productRepository;
    private readonly IRepository<User> _userRepository;
    private readonly IEmailService _emailService;
    private readonly ILogger<ReportGenerationJob> _logger;

    public ReportGenerationJob(
        IRepository<Product> productRepository,
        IRepository<User> userRepository,
        IEmailService _emailService,
        ILogger<ReportGenerationJob> logger)
    {
        _productRepository = productRepository;
        _userRepository = userRepository;
        this._emailService = _emailService;
        _logger = logger;
    }

    public async Task GenerateDailySummaryAsync()
    {
        _logger.LogInformation("Generating daily summary report");

        var products = await _productRepository.GetAllAsync(CancellationToken.None);
        var users = await _userRepository.GetAllAsync(CancellationToken.None);

        var totalProducts = products.Count();
        var lowStockProducts = products.Count(p => p.Stock < 10);
        var totalUsers = users.Count();
        var activeUsers = users.Count(u => u.IsActive);

        var reportContent = $@"
Daily Summary Report - {DateTime.UtcNow:yyyy-MM-dd}

Products:
- Total Products: {totalProducts}
- Low Stock Items: {lowStockProducts}
- Total Inventory Value: ${products.Sum(p => p.Price * p.Stock):N2}

Users:
- Total Users: {totalUsers}
- Active Users: {activeUsers}
- Inactive Users: {totalUsers - activeUsers}
";

        _logger.LogInformation("Daily summary generated: {TotalProducts} products, {TotalUsers} users",
            totalProducts, totalUsers);

        // In production, send via email service
        _logger.LogInformation("Report Content: {ReportContent}", reportContent);
    }
}
