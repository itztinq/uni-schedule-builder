using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using uni_schedule_builder.Models;

namespace uni_schedule_builder.Data;

public static class DbSeeder
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static async Task SeedRolesAndAdminAsync(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        string[] roles = { "Admin", "Teacher", "Student" };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        const string adminEmail = "admin@unischedule.com";

        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser is null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(adminUser, "Password123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }
    }

    public static async Task SeedFromJsonAsync(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        var jsonPath = Path.Combine(Directory.GetCurrentDirectory(), "finki_raspored.json");
        var json = await File.ReadAllTextAsync(jsonPath);
        var data = JsonSerializer.Deserialize<JsonData>(json, JsonOptions);

        if (data is null)
        {
            return;
        }

        var subjectIds = new Dictionary<string, int>();
        foreach (var jsonSubject in data.Subjects)
        {
            var subject = new Subject { Name = jsonSubject.Name, Description = "/" };
            context.Subjects.Add(subject);
            await context.SaveChangesAsync();
            subjectIds[jsonSubject.Id] = subject.Id;
        }

        var roomIds = new Dictionary<string, int>();
        foreach (var jsonRoom in data.Classrooms)
        {
            var room = new Room { Name = jsonRoom.Name, Capacity = 100 };
            context.Rooms.Add(room);
            await context.SaveChangesAsync();
            roomIds[jsonRoom.Id] = room.Id;
        }

        var teacherIds = new Dictionary<string, string>();
        foreach (var jsonTeacher in data.Teachers)
        {
            var email = $"teacher_{jsonTeacher.Id}@finki.ukim.mk";
            var teacher = await userManager.FindByEmailAsync(email);
            if (teacher is null)
            {
                teacher = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(teacher, "Password123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(teacher, "Teacher");
                }
            }

            teacherIds[jsonTeacher.Id] = teacher.Id;
        }

        foreach (var card in data.Cards)
        {
            if (card.Classrooms.Count == 0 || card.Teachers.Count == 0)
            {
                continue;
            }

            if (!subjectIds.TryGetValue(card.Subject.Id, out var subjectId) ||
                !roomIds.TryGetValue(card.Classrooms[0].Id, out var roomId) ||
                !teacherIds.TryGetValue(card.Teachers[0].Id, out var teacherId))
            {
                continue;
            }

            context.RegularClassTerms.Add(new RegularClassTerm
            {
                DayOfWeek = (DayOfWeek)(card.DayIndex + 1),
                StartTime = TimeSpan.Parse(card.StartTime),
                EndTime = TimeSpan.Parse(card.EndTime),
                TermType = "Lecture",
                SubjectId = subjectId,
                RoomId = roomId,
                TeacherId = teacherId
            });
        }

        await context.SaveChangesAsync();
    }

    public record JsonData(List<JsonSubject> Subjects, List<JsonTeacher> Teachers, List<JsonRoom> Classrooms, List<JsonCard> Cards);

    public record JsonSubject(string Id, string Name);

    public record JsonTeacher(string Id, string Name);

    public record JsonRoom(string Id, string Name);

    public record JsonCard(int DayIndex, string StartTime, string EndTime, JsonSubject Subject, List<JsonRoom> Classrooms, List<JsonTeacher> Teachers);
}