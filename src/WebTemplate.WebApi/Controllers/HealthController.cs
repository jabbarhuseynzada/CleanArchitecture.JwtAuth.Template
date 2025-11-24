using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebTemplate.Infrastructure.Persistence;

namespace WebTemplate.WebApi.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Route("api/[controller]")]
[AllowAnonymous]
public class HealthController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<HealthController> _logger;

    public HealthController(ApplicationDbContext dbContext, ILogger<HealthController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Basic health check - returns 200 if API is running
    /// </summary>
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            status = "Healthy",
            timestamp = DateTime.UtcNow,
            version = GetType().Assembly.GetName().Version?.ToString() ?? "1.0.0"
        });
    }

    /// <summary>
    /// Detailed health check including database connectivity
    /// </summary>
    [HttpGet("detailed")]
    public async Task<IActionResult> GetDetailed()
    {
        var healthReport = new
        {
            status = "Healthy",
            timestamp = DateTime.UtcNow,
            version = GetType().Assembly.GetName().Version?.ToString() ?? "1.0.0",
            checks = new List<object>()
        };

        var checks = (List<object>)healthReport.checks;

        // Database check
        try
        {
            var canConnect = await _dbContext.Database.CanConnectAsync();
            checks.Add(new
            {
                name = "Database",
                status = canConnect ? "Healthy" : "Unhealthy",
                description = canConnect ? "Database connection successful" : "Database connection failed"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed");
            checks.Add(new
            {
                name = "Database",
                status = "Unhealthy",
                description = $"Database connection failed: {ex.Message}"
            });
        }

        // Memory check
        var allocatedMemory = GC.GetTotalMemory(false) / 1024 / 1024;
        checks.Add(new
        {
            name = "Memory",
            status = allocatedMemory < 500 ? "Healthy" : "Degraded",
            description = $"Allocated memory: {allocatedMemory} MB"
        });

        var isHealthy = checks.All(c => ((dynamic)c).status != "Unhealthy");

        return isHealthy ? Ok(healthReport) : StatusCode(503, healthReport);
    }

    /// <summary>
    /// Liveness probe for Kubernetes/Docker
    /// </summary>
    [HttpGet("live")]
    public IActionResult Live()
    {
        return Ok(new { status = "Live" });
    }

    /// <summary>
    /// Readiness probe for Kubernetes/Docker
    /// </summary>
    [HttpGet("ready")]
    public async Task<IActionResult> Ready()
    {
        try
        {
            await _dbContext.Database.CanConnectAsync();
            return Ok(new { status = "Ready" });
        }
        catch
        {
            return StatusCode(503, new { status = "Not Ready" });
        }
    }
}
