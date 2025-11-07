namespace JobDisplayer.Web.ViewModels;

public class DashboardViewModel
{
    public IReadOnlyCollection<JobDisplayViewModel> Jobs { get; init; } = Array.Empty<JobDisplayViewModel>();
    public string ActiveFilter { get; init; } = "recent";
    public bool HasResume { get; init; }
    public DateTime? ResumeUploadedAt { get; init; }
}
