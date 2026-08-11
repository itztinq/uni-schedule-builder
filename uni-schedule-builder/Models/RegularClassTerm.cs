using System.ComponentModel.DataAnnotations;

namespace uni_schedule_builder.Models;

public class RegularClassTerm
{
    public int Id { get; set; }

    [Required]
    public DayOfWeek DayOfWeek { get; set; }

    [Required]
    public TimeSpan StartTime { get; set; }

    [Required]
    public TimeSpan EndTime { get; set; }

    [Required]
    public string TermType { get; set; } = string.Empty;

    public int SubjectId { get; set; }
    public Subject Subject { get; set; } = null!;

    public int RoomId { get; set; }
    public Room Room { get; set; } = null!;

    public string TeacherId { get; set; } = string.Empty;
    public ApplicationUser Teacher { get; set; } = null!;
}
