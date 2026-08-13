using System.ComponentModel.DataAnnotations;

namespace uni_schedule_builder.Models;

public class StudyGroup
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public string? ShortName { get; set; }

    public ICollection<RegularClassTermGroup> RegularClassTermGroups { get; set; } = new List<RegularClassTermGroup>();
}