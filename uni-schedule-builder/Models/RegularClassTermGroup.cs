namespace uni_schedule_builder.Models;

public class RegularClassTermGroup
{
    public int RegularClassTermId { get; set; }
    public RegularClassTerm RegularClassTerm { get; set; } = null!;

    public int StudyGroupId { get; set; }
    public StudyGroup StudyGroup { get; set; } = null!;
}