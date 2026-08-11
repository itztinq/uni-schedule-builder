using System.ComponentModel.DataAnnotations;

namespace uni_schedule_builder.Models;

public class Subject
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }
}
