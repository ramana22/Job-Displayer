using System.ComponentModel.DataAnnotations;
using HiringCafeTracker.Backend.Data;
using HiringCafeTracker.Backend.DTOs;
using HiringCafeTracker.Backend.Models;
using HiringCafeTracker.Backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HiringCafeTracker.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class JobsController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IResumeMatchingService _matchingService;
    private readonly ILogger<JobsController> _logger;
    private readonly IConfiguration _configuration;

    public JobsController(ApplicationDbContext dbContext, IResumeMatchingService matchingService, ILogger<JobsController> logger, IConfiguration configuration)
    {
        _dbContext = dbContext;
        _matchingService = matchingService;
        _logger = logger;
        _configuration = configuration;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<JobResponseDto>>> GetJobs([FromQuery] string? timeframe = null, [FromQuery] string? source = null, [FromQuery] string? status = null)
    {
        var query = _dbContext.Jobs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(source))
        {
            query = query.Where(j => j.Source == source);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(j => j.Status == status);
        }

        query = timeframe switch
        {
            "24h" => query.Where(j => j.PostedTime >= DateTime.UtcNow.AddHours(-24)),
            "3d" => query.Where(j => j.PostedTime >= DateTime.UtcNow.AddDays(-3)),
            "5d" => query.Where(j => j.PostedTime >= DateTime.UtcNow.AddDays(-5)),
            _ => query
        };

        var jobs = await query
            .OrderByDescending(j => j.PostedTime ?? j.CreatedAt)
            .Take(500)
            .ToListAsync();

        var response = jobs.Select(MapJobToResponse);

        return Ok(response);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<JobResponseDto>> GetJob(int id)
    {
        var job = await _dbContext.Jobs.FindAsync(id);
        if (job == null)
        {
            return NotFound();
        }

        return Ok(MapJobToResponse(job));
    }

    [HttpPost("import")]
    public async Task<IActionResult> Import([FromBody] IEnumerable<JobImportDto> payload, [FromHeader(Name = "X-API-KEY")] string? apiKey, CancellationToken cancellationToken)
    {
        if (!IsAuthorized(apiKey))
        {
            return Unauthorized(new { success = false, message = "Invalid API key." });
        }

        if (payload == null)
        {
            return BadRequest(new { success = false, message = "Request body is empty." });
        }

        var inserted = 0;
        var skipped = 0;
        var jobsToScore = new List<Job>();

        foreach (var jobDto in payload)
        {
            var context = new ValidationContext(jobDto);
            var validationResults = new List<ValidationResult>();
            if (!Validator.TryValidateObject(jobDto, context, validationResults, true))
            {
                skipped++;
                _logger.LogWarning("Skipping job {JobId} because validation failed: {Errors}", jobDto.JobId, string.Join("; ", validationResults.Select(v => v.ErrorMessage)));
                continue;
            }

            var exists = await _dbContext.Jobs.AnyAsync(j => j.JobId == jobDto.JobId, cancellationToken);
            if (exists)
            {
                skipped++;
                continue;
            }

            var job = new Job
            {
                JobId = jobDto.JobId,
                JobTitle = jobDto.JobTitle,
                Company = jobDto.Company,
                Location = jobDto.Location,
                Salary = jobDto.Salary,
                Description = jobDto.Description,
                ApplyLink = jobDto.ApplyLink,
                SearchKey = jobDto.SearchKey,
                PostedTime = jobDto.PostedTime,
                Source = jobDto.Source,
                Status = "Not Applied"
            };

            _dbContext.Jobs.Add(job);
            jobsToScore.Add(job);
            inserted++;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _matchingService.RefreshScoresAsync(jobsToScore, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new { success = true, inserted, skipped });
    }

    [HttpPost("{id:int}/apply")]
    public async Task<IActionResult> MarkAsApplied(int id, CancellationToken cancellationToken)
    {
        var job = await _dbContext.Jobs.FindAsync(new object[] { id }, cancellationToken);
        if (job == null)
        {
            return NotFound();
        }

        job.Status = "Applied";
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new { success = true });
    }

    [HttpPost("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] string status, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return BadRequest(new { success = false, message = "Status is required." });
        }

        var job = await _dbContext.Jobs.FindAsync(new object[] { id }, cancellationToken);
        if (job == null)
        {
            return NotFound();
        }

        job.Status = status;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new { success = true });
    }

    [HttpGet("sources")]
    public async Task<IActionResult> GetSources(CancellationToken cancellationToken)
    {
        var sources = await _dbContext.Jobs.AsNoTracking()
            .Where(j => j.Source != null)
            .Select(j => j.Source!)
            .Distinct()
            .OrderBy(s => s)
            .ToListAsync(cancellationToken);

        return Ok(sources);
    }

    private bool IsAuthorized(string? apiKey)
    {
        var configuredKey = _configuration["ApiKey"];
        return !string.IsNullOrWhiteSpace(configuredKey) && configuredKey == apiKey;
    }

    private static JobResponseDto MapJobToResponse(Job job) => new()
    {
        Id = job.Id,
        JobId = job.JobId,
        JobTitle = job.JobTitle,
        Company = job.Company,
        Location = job.Location,
        Salary = job.Salary,
        Description = job.Description,
        ApplyLink = job.ApplyLink,
        SearchKey = job.SearchKey,
        PostedTime = job.PostedTime,
        Source = job.Source,
        MatchingScore = job.MatchingScore,
        Status = job.Status,
        CreatedAt = job.CreatedAt
    };
}
