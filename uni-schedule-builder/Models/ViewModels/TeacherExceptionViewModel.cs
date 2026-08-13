using System.ComponentModel.DataAnnotations;

namespace uni_schedule_builder.Models.ViewModels;

public class TeacherExceptionViewModel
{
    public int RegularClassTermId { get; set; }

    [Required(ErrorMessage = "Specific date is required.")]
    [DataType(DataType.Date)]
    public DateTime SpecificDate { get; set; }

    [DataType(DataType.Time)]
    public TimeSpan? ExceptionTime { get; set; }

    [Required(ErrorMessage = "Status is required.")]
    public string Status { get; set; } = string.Empty;

    public string? Message { get; set; }

    public int? NewRoomId { get; set; }

    public string? TermInfo { get; set; }

    public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> StatusOptions { get; set; } = new();
    public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> Rooms { get; set; } = new();
}