namespace uni_schedule_builder.Models;

public class StudentSchedule
{
    public int Id { get; set; }

    public string StudentId { get; set; } = string.Empty;
    public ApplicationUser Student { get; set; } = null!;

    public int RegularClassTermId { get; set; }
    public RegularClassTerm RegularClassTerm { get; set; } = null!;
}
