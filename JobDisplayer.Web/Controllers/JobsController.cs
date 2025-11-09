using JobDisplayer.Web.Data;
using JobDisplayer.Web.Models;
using JobDisplayer.Web.Services;
using JobDisplayer.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobDisplayer.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class JobsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<JobsController> _logger;

    public JobsController(ApplicationDbContext context, ILogger<JobsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<JobPosting>>> GetJobs([FromQuery] string? timeframe = null)
    {
        var query = _context.JobPostings.AsNoTracking();

        if (TimeframeFilter.TryGetCutoff(timeframe, out var cutoff))
        {
            query = query.Where(job => job.PostedAt >= cutoff);
        }

        var jobs = await query
            .OrderByDescending(job => job.PostedAt)
            .ThenByDescending(job => job.CreatedAt)
            .Take(500)
            .ToListAsync();

        return Ok(jobs);
    }

    [HttpPost]
    public async Task<IActionResult> PostJobs([FromBody] IEnumerable<JobIngestRequest> payload)
    {
        if (payload is null)
        {
            return BadRequest("Request body cannot be empty.");
        }

        var incoming = payload
            .Where(p => !string.IsNullOrWhiteSpace(p.JobTitle) && !string.IsNullOrWhiteSpace(p.Company))
            .ToList();

        if (incoming.Count == 0)
        {
            return BadRequest("No valid job postings supplied.");
        }

        var normalizedApplyLinks = incoming
            .Select(p => p.ApplyLink?.Trim())
            .Where(link => !string.IsNullOrEmpty(link))
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var existingApplyLinks = normalizedApplyLinks.Length == 0
            ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            : (await _context.JobPostings
                .Where(job => job.ApplyLink != null && normalizedApplyLinks.Contains(job.ApplyLink))
                .Select(job => job.ApplyLink!)
                .ToListAsync())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var now = DateTime.UtcNow;
        var newJobs = new List<JobPosting>();

        foreach (var jobRequest in incoming)
        {
            var applyLink = jobRequest.ApplyLink?.Trim();
            if (!string.IsNullOrEmpty(applyLink) && existingApplyLinks.Contains(applyLink))
            {
                _logger.LogInformation("Skipping duplicate job with apply link {ApplyLink}", applyLink);
                continue;
            }

            var postedAt = jobRequest.PostedAt?.ToUniversalTime() ?? now;
            var dedupeKey = string.Join('|',
                (applyLink ?? string.Empty).ToLowerInvariant(),
                jobRequest.JobTitle.ToLowerInvariant(),
                jobRequest.Company.ToLowerInvariant(),
                jobRequest.Location?.ToLowerInvariant() ?? string.Empty,
                postedAt.ToString("O"));

            if (!seenKeys.Add(dedupeKey))
            {
                continue;
            }

            var jobPosting = new JobPosting
            {
                JobTitle = jobRequest.JobTitle,
                Company = jobRequest.Company,
                Location = jobRequest.Location,
                Salary = jobRequest.Salary,
                ApplyLink = applyLink,
                SearchKey = jobRequest.SearchKey,
                Description = jobRequest.Description,
                PostedAt = postedAt,
                CreatedAt = now
            };

            newJobs.Add(jobPosting);

            if (!string.IsNullOrEmpty(applyLink))
            {
                existingApplyLinks.Add(applyLink);
            }
        }

        if (newJobs.Count == 0)
        {
            return Conflict("All provided job postings are duplicates.");
        }

        await _context.JobPostings.AddRangeAsync(newJobs);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetJobs), routeValues: null, value: new { inserted = newJobs.Count });
    }
}
