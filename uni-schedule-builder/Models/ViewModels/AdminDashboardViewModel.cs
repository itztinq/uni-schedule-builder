namespace uni_schedule_builder.Models.ViewModels;

public class AdminDashboardViewModel
{
    public int SubjectCount { get; set; }
    public int RoomCount { get; set; }
    public int TeacherCount { get; set; }
    public int StudentGroupCount { get; set; }
    public int TermCount { get; set; }
    public int ActiveExceptionCount { get; set; }

    public List<RoomUsageItem> MostUsedRooms { get; set; } = new();
    public List<ActiveExceptionItem> UpcomingExceptions { get; set; } = new();
}

public class RoomUsageItem
{
    public string RoomName { get; set; } = string.Empty;
    public int TermCount { get; set; }
}

public class ActiveExceptionItem
{
    public DateTime SpecificDate { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public string TeacherName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Message { get; set; }
}