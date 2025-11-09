using HiringCafeTracker.Backend.Models;

namespace HiringCafeTracker.Backend.Services;

public interface IResumeMatchingService
{
    Task<double> CalculateMatchingScoreAsync(Job job, CancellationToken cancellationToken = default);
    Task RefreshScoresAsync(IEnumerable<Job> jobs, CancellationToken cancellationToken = default);
}
