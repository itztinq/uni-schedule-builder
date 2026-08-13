using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using uni_schedule_builder.Models;

namespace uni_schedule_builder.Models.ViewModels;

public class AdminTermViewModel
{
    public int Id { get; set; }

    [Display(Name = "Day")]
    [Range(1, 5, ErrorMessage = "Select a day from Monday to Friday.")]
    public int DayOfWeek { get; set; }

    [Required(ErrorMessage = "Start time is required.")]
    [RegularExpression(@"^\d{1,2}:\d{2}$", ErrorMessage = "Use HH:mm format, e.g. 09:00.")]
    public string StartTime { get; set; } = "09:00";

    [Required(ErrorMessage = "End time is required.")]
    [RegularExpression(@"^\d{1,2}:\d{2}$", ErrorMessage = "Use HH:mm format, e.g. 10:00.")]
    public string EndTime { get; set; } = "10:00";

    [Required(ErrorMessage = "Term type is required.")]
    public string TermType { get; set; } = "Lecture";

    [Required(ErrorMessage = "Subject is required.")]
    public int SubjectId { get; set; }

    [Required(ErrorMessage = "Room is required.")]
    public int RoomId { get; set; }

    [Required(ErrorMessage = "Teacher is required.")]
    public string TeacherId { get; set; } = string.Empty;

    public List<int> SelectedGroupIds { get; set; } = new();

    // Populated by the controller.
    public List<SelectListItem> DayOptions { get; set; } = new();
    public List<SelectListItem> TermTypeOptions { get; set; } = new();
    public List<SelectListItem> SubjectOptions { get; set; } = new();
    public List<SelectListItem> RoomOptions { get; set; } = new();
    public List<SelectListItem> TeacherOptions { get; set; } = new();
    public List<StudyGroup> Groups { get; set; } = new();
}