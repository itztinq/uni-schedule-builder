using System.ComponentModel.DataAnnotations;

namespace uni_schedule_builder.Models;

public class TermException
{
    public int Id { get; set; }

    public int RegularClassTermId { get; set; }
    public RegularClassTerm RegularClassTerm { get; set; } = null!;

    [Required]
    public DateTime SpecificDate { get; set; }

    [Required]
    public string Status { get; set; } = string.Empty;

    public string? Message { get; set; }

    public int? NewRoomId { get; set; }
    public Room? NewRoom { get; set; }
}
