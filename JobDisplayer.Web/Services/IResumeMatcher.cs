using JobDisplayer.Web.Models;

namespace JobDisplayer.Web.Services;

public interface IResumeMatcher
{
    (double Score, IReadOnlyCollection<string> Matches) CalculateScore(string resumeText, JobPosting jobPosting);
}
