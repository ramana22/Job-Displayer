using JobDisplayer.Web.Models;
using System.Text.RegularExpressions;

namespace JobDisplayer.Web.Services;

public class ResumeMatcher : IResumeMatcher
{
    private static readonly Regex TokenRegex = new("[a-zA-Z0-9]+", RegexOptions.Compiled);

    public (double Score, IReadOnlyCollection<string> Matches) CalculateScore(string resumeText, JobPosting jobPosting)
    {
        if (string.IsNullOrWhiteSpace(resumeText))
        {
            return (0, Array.Empty<string>());
        }

        var resumeTokens = Tokenize(resumeText);
        var jobTokens = Tokenize(string.Join(' ', new[]
        {
            jobPosting.JobTitle,
            jobPosting.Company,
            jobPosting.Location,
            jobPosting.SearchKey,
            jobPosting.Description
        }));

        if (jobTokens.Count == 0)
        {
            return (0, Array.Empty<string>());
        }

        var matches = jobTokens
            .Where(resumeTokens.Contains)
            .Distinct()
            .ToArray();

        var score = jobTokens.Count == 0
            ? 0
            : Math.Round(matches.Length / (double)jobTokens.Count * 100, 1, MidpointRounding.AwayFromZero);

        return (score, matches);
    }

    private static HashSet<string> Tokenize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        var tokens = TokenRegex
            .Matches(text)
            .Select(m => m.Value.ToLowerInvariant())
            .Where(t => t.Length > 1)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return tokens;
    }
}
