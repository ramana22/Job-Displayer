using System.ComponentModel.DataAnnotations;

namespace JobDisplayer.Web.ViewModels;

public class JobIngestRequest
{
    [Required]
    public string JobTitle { get; set; } = string.Empty;

    [Required]
    public string Company { get; set; } = string.Empty;

    public string? Location { get; set; }
    public string? Salary { get; set; }
    public string? ApplyLink { get; set; }
    public string? SearchKey { get; set; }
    public string? Description { get; set; }
    public DateTime? PostedAt { get; set; }
}
