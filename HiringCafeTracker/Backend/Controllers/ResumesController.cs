using HiringCafeTracker.Backend.Data;
using HiringCafeTracker.Backend.Models;
using HiringCafeTracker.Backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HiringCafeTracker.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ResumesController : ControllerBase
{
    private static readonly string[] AllowedExtensions = new[] { ".pdf", ".docx" };
    private readonly ApplicationDbContext _dbContext;
    private readonly ResumeStorageOptions _options;
    private readonly ILogger<ResumesController> _logger;
    private readonly IResumeMatchingService _matchingService;

    public ResumesController(ApplicationDbContext dbContext, IOptions<ResumeStorageOptions> options, ILogger<ResumesController> logger, IResumeMatchingService matchingService)
    {
        _dbContext = dbContext;
        _options = options.Value;
        _logger = logger;
        _matchingService = matchingService;
    }

    [HttpGet("active")]
    public async Task<IActionResult> GetActiveResume(CancellationToken cancellationToken)
    {
        var resume = await _dbContext.Resumes.AsNoTracking().Where(r => r.IsActive)
            .OrderByDescending(r => r.UploadedAt)
            .Select(r => new { r.Id, r.FileName, r.UploadedAt })
            .FirstOrDefaultAsync(cancellationToken);

        if (resume == null)
        {
            return Ok(null);
        }

        return Ok(resume);
    }

    [HttpPost]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> UploadResume([FromForm] IFormFile file, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { success = false, message = "Resume file is required." });
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
        {
            return BadRequest(new { success = false, message = "Only PDF and DOCX resumes are supported." });
        }

        Directory.CreateDirectory(_options.RootDirectory);
        var fileName = $"resume-{DateTime.UtcNow:yyyyMMddHHmmssfff}{extension}";
        var path = Path.Combine(_options.RootDirectory, fileName);

        await using (var stream = System.IO.File.Create(path))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            await _dbContext.Resumes.Where(r => r.IsActive).ExecuteUpdateAsync(setters => setters.SetProperty(r => r.IsActive, false), cancellationToken);

            var resume = new Resume
            {
                FileName = file.FileName,
                FilePath = path,
                UploadedAt = DateTime.UtcNow,
                IsActive = true
            };

            _dbContext.Resumes.Add(resume);
            await _dbContext.SaveChangesAsync(cancellationToken);
            await _dbContext.Database.CommitTransactionAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload resume");
            await _dbContext.Database.RollbackTransactionAsync(cancellationToken);
            return StatusCode(StatusCodes.Status500InternalServerError, new { success = false, message = "Unable to store resume." });
        }

        var jobs = await _dbContext.Jobs.ToListAsync(cancellationToken);
        await _matchingService.RefreshScoresAsync(jobs, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new { success = true });
    }
}
