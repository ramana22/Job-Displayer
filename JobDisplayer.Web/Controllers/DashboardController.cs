using System.IO;
using System.Text;
using JobDisplayer.Web.Data;
using JobDisplayer.Web.Services;
using JobDisplayer.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobDisplayer.Web.Controllers;

public class DashboardController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IResumeMatcher _resumeMatcher;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(ApplicationDbContext context, IResumeMatcher resumeMatcher, ILogger<DashboardController> logger)
    {
        _context = context;
        _resumeMatcher = resumeMatcher;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string timeframe = "recent")
    {
        var resume = await _context.Resumes
            .AsNoTracking()
            .OrderByDescending(r => r.UploadedAt)
            .FirstOrDefaultAsync();

        var resumeText = resume?.ExtractedText ?? string.Empty;

        var jobsQuery = _context.JobPostings.AsNoTracking();

        if (TimeframeFilter.TryGetCutoff(timeframe, out var cutoff))
        {
            jobsQuery = jobsQuery.Where(job => job.PostedAt >= cutoff);
        }

        var jobs = await jobsQuery
            .OrderByDescending(job => job.PostedAt)
            .ThenByDescending(job => job.CreatedAt)
            .Take(500)
            .ToListAsync();

        var jobViewModels = jobs
            .Select(job =>
            {
                var (score, matches) = _resumeMatcher.CalculateScore(resumeText, job);

                return new JobDisplayViewModel
                {
                    Id = job.Id,
                    JobTitle = job.JobTitle,
                    Company = job.Company,
                    Location = job.Location,
                    Salary = job.Salary,
                    ApplyLink = job.ApplyLink,
                    SearchKey = job.SearchKey,
                    PostedAt = job.PostedAt,
                    MatchingScore = score,
                    MatchedKeywords = matches
                };
            })
            .ToArray();

        var viewModel = new DashboardViewModel
        {
            Jobs = jobViewModels,
            ActiveFilter = timeframe,
            HasResume = resume is not null,
            ResumeUploadedAt = resume?.UploadedAt
        };

        ViewBag.StatusMessage = TempData[nameof(ViewBag.StatusMessage)] as string;
        ViewBag.ErrorMessage = TempData[nameof(ViewBag.ErrorMessage)] as string;

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadResume(IFormFile resumeFile)
    {
        if (resumeFile is null || resumeFile.Length == 0)
        {
            TempData[nameof(ViewBag.ErrorMessage)] = "Please choose a non-empty resume file.";
            return RedirectToAction(nameof(Index));
        }

        if (resumeFile.Length > 5 * 1024 * 1024)
        {
            TempData[nameof(ViewBag.ErrorMessage)] = "Resume file is too large. Please upload a file smaller than 5 MB.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            using var memoryStream = new MemoryStream();
            await resumeFile.CopyToAsync(memoryStream);
            var contentBytes = memoryStream.ToArray();

            string resumeText;
            if (resumeFile.ContentType == "text/plain" || Path.GetExtension(resumeFile.FileName).Equals(".txt", StringComparison.OrdinalIgnoreCase))
            {
                resumeText = Encoding.UTF8.GetString(contentBytes);
            }
            else
            {
                resumeText = string.Empty;
            }

            var resume = new Models.ResumeFile
            {
                FileName = resumeFile.FileName,
                ContentType = resumeFile.ContentType,
                Content = contentBytes,
                ExtractedText = resumeText,
                UploadedAt = DateTime.UtcNow
            };

            _context.Resumes.Add(resume);
            await _context.SaveChangesAsync();

            TempData[nameof(ViewBag.StatusMessage)] = resumeText.Length == 0
                ? "Resume saved, but automatic keyword matching is only available for plain text resumes."
                : "Resume uploaded successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload resume");
            TempData[nameof(ViewBag.ErrorMessage)] = "We were unable to upload the resume. Please try again.";
        }

        return RedirectToAction(nameof(Index));
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View();
    }
}
