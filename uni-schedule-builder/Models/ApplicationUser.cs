using Microsoft.AspNetCore.Identity;

namespace uni_schedule_builder.Models;

public class ApplicationUser : IdentityUser
{
    public string? FullName { get; set; }
}
