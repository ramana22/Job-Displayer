using System.ComponentModel.DataAnnotations;

namespace HiringCafeTracker.Backend.DTOs;

public class JobImportDto
{
    [Required]
    public string JobId { get; set; } = string.Empty;

    [Required]
    public string JobTitle { get; set; } = string.Empty;

    public string? Company { get; set; }
    public string? Location { get; set; }
    public string? Salary { get; set; }
    public string? Description { get; set; }
    public string? ApplyLink { get; set; }
    public string? SearchKey { get; set; }
    public DateTime? PostedTime { get; set; }
    public string? Source { get; set; }
}
