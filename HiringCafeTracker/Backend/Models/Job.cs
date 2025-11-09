using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HiringCafeTracker.Backend.Models;

public class Job
{
    public int Id { get; set; }

    [MaxLength(50)]
    public string JobId { get; set; } = string.Empty;

    [MaxLength(255)]
    public string JobTitle { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? Company { get; set; }

    [MaxLength(255)]
    public string? Location { get; set; }

    [MaxLength(100)]
    public string? Salary { get; set; }

    public string? Description { get; set; }

    [MaxLength(500)]
    public string? ApplyLink { get; set; }

    [MaxLength(100)]
    public string? SearchKey { get; set; }

    public DateTime? PostedTime { get; set; }

    [MaxLength(50)]
    public string? Source { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal MatchingScore { get; set; }

    [MaxLength(20)]
    public string Status { get; set; } = "Not Applied";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
