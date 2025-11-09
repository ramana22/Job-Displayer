namespace JobDisplayer.Web.ViewModels;

public class JobDisplayViewModel
{
    public int Id { get; set; }
    public string JobTitle { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string? Salary { get; set; }
    public string? ApplyLink { get; set; }
    public string? SearchKey { get; set; }
    public DateTime PostedAt { get; set; }
    public double MatchingScore { get; set; }
    public IReadOnlyCollection<string> MatchedKeywords { get; set; } = Array.Empty<string>();
}
