using System.Text;
using DocumentFormat.OpenXml.Packaging;
using HiringCafeTracker.Backend.Data;
using HiringCafeTracker.Backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace HiringCafeTracker.Backend.Services;

public class ResumeMatchingService : IResumeMatchingService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ResumeStorageOptions _options;
    private string? _resumeCache;

    public ResumeMatchingService(ApplicationDbContext dbContext, IOptions<ResumeStorageOptions> options)
    {
        _dbContext = dbContext;
        _options = options.Value;
    }

    public async Task<double> CalculateMatchingScoreAsync(Job job, CancellationToken cancellationToken = default)
    {
        var resume = await EnsureResumeTextAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(resume))
        {
            return 0;
        }

        var jobCorpus = string.Join(" ", new[]
        {
            job.JobTitle,
            job.Company,
            job.Location,
            job.Description,
            job.SearchKey
        }.Where(s => !string.IsNullOrWhiteSpace(s))).ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(jobCorpus))
        {
            return 0;
        }

        var resumeTokens = Tokenize(resume);
        var jobTokens = Tokenize(jobCorpus);

        if (resumeTokens.Count == 0 || jobTokens.Count == 0)
        {
            return 0;
        }

        var intersection = resumeTokens.Intersect(jobTokens).Count();
        var score = (double)intersection / jobTokens.Count * 100;
        return Math.Round(score, 2);
    }

    public async Task RefreshScoresAsync(IEnumerable<Job> jobs, CancellationToken cancellationToken = default)
    {
        var resume = await EnsureResumeTextAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(resume))
        {
            foreach (var job in jobs)
            {
                job.MatchingScore = 0;
            }

            return;
        }

        var resumeTokens = Tokenize(resume);
        foreach (var job in jobs)
        {
            var jobCorpus = string.Join(" ", new[]
            {
                job.JobTitle,
                job.Company,
                job.Location,
                job.Description,
                job.SearchKey
            }.Where(s => !string.IsNullOrWhiteSpace(s))).ToLowerInvariant();

            var jobTokens = Tokenize(jobCorpus);
            var intersection = resumeTokens.Intersect(jobTokens).Count();
            job.MatchingScore = jobTokens.Count == 0 ? 0 : Math.Round((double)intersection / jobTokens.Count * 100, 2);
        }
    }

    private async Task<string?> EnsureResumeTextAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(_resumeCache))
        {
            return _resumeCache;
        }

        var resume = await _dbContext.Resumes.AsNoTracking().Where(r => r.IsActive).OrderByDescending(r => r.UploadedAt).FirstOrDefaultAsync(cancellationToken);
        if (resume == null)
        {
            return null;
        }

        var path = resume.FilePath;
        if (!File.Exists(path))
        {
            return null;
        }

        _resumeCache = await ExtractTextAsync(path, cancellationToken);
        return _resumeCache;
    }

    private async Task<string?> ExtractTextAsync(string path, CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(path).ToLowerInvariant();
        return extension switch
        {
            ".pdf" => await ExtractPdfAsync(path),
            ".docx" => ExtractDocx(path),
            ".txt" => await File.ReadAllTextAsync(path, cancellationToken),
            _ => null
        };
    }

    private static async Task<string?> ExtractPdfAsync(string path)
    {
        await using var stream = File.OpenRead(path);
        using var document = PdfDocument.Open(stream);
        var builder = new StringBuilder();
        foreach (var page in document.GetPages())
        {
            var text = ContentOrderTextExtractor.GetText(page);
            builder.AppendLine(text);
        }

        return builder.ToString();
    }

    private static string? ExtractDocx(string path)
    {
        using var document = WordprocessingDocument.Open(path, false);
        var body = document.MainDocumentPart?.Document.Body;
        return body?.InnerText;
    }

    private static HashSet<string> Tokenize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new HashSet<string>();
        }

        var tokens = text.ToLowerInvariant()
            .Split(new[] { ' ', '\n', '\r', '\t', '.', ',', ';', ':', '/', '-', '(', ')', '"', '\'', '?' }, StringSplitOptions.RemoveEmptyEntries);
        return tokens.Where(t => t.Length > 2).ToHashSet();
    }
}
