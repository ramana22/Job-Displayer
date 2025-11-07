namespace JobDisplayer.Web.Models;

public class JobPosting
{
    public int Id { get; set; }
    public string JobTitle { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string? Salary { get; set; }
    public string? ApplyLink { get; set; }
    public string? SearchKey { get; set; }
    public string? Description { get; set; }
    public DateTime PostedAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
