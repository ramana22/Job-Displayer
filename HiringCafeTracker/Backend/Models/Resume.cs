using System.ComponentModel.DataAnnotations;

namespace HiringCafeTracker.Backend.Models;

public class Resume
{
    public int Id { get; set; }

    [MaxLength(255)]
    public string FileName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string FilePath { get; set; } = string.Empty;

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;
}
