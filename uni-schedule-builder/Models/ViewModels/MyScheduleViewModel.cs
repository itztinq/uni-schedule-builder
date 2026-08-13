namespace uni_schedule_builder.Models.ViewModels;

public class MyScheduleViewModel
{
    public List<StudentSchedule> Entries { get; set; } = new();

    public Dictionary<int, List<TermException>> ExceptionsByTerm { get; set; } = new();

    public List<TermException> ExceptionsFor(int termId) =>
        ExceptionsByTerm.TryGetValue(termId, out var list) ? list : new List<TermException>();
}