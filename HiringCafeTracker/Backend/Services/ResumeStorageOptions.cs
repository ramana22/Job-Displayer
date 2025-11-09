namespace HiringCafeTracker.Backend.Services;

public class ResumeStorageOptions
{
    public string RootDirectory { get; set; } = Path.Combine(AppContext.BaseDirectory, "resumes");
}
