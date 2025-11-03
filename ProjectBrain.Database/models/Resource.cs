using System.ComponentModel.DataAnnotations;

public class Resource
{
    public Guid Id { get; set; }

    [StringLength(128)]
    public string UserId { get; set; } = string.Empty;

    [StringLength(128)]
    public string FileName { get; set; } = string.Empty;

    [StringLength(512)]
    public string Location { get; set; } = string.Empty;

    public int SizeInBytes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}