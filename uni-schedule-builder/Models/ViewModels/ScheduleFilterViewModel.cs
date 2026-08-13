using uni_schedule_builder.Models;

namespace uni_schedule_builder.Models.ViewModels;

public class ScheduleFilterViewModel
{
    public List<RegularClassTerm> Terms { get; set; } = new();

    public int? SelectedGroupId { get; set; }
    public int? SelectedSubjectId { get; set; }
    public string? SelectedTeacherId { get; set; }
    public string? SelectedGroupName { get; set; }

    public List<StudyGroup> Groups { get; set; } = new();
    public List<Subject> Subjects { get; set; } = new();
    public List<(string Id, string Name)> Teachers { get; set; } = new();
}