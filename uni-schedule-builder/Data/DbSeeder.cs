using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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

        const string studentEmail = "student@unischedule.com";

        var studentUser = await userManager.FindByEmailAsync(studentEmail);
        if (studentUser is null)
        {
            studentUser = new ApplicationUser
            {
                UserName = studentEmail,
                Email = studentEmail,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(studentUser, "Password123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(studentUser, "Student");
            }
        }

        const string student2Email = "student2@unischedule.com";

        var student2User = await userManager.FindByEmailAsync(student2Email);
        if (student2User is null)
        {
            student2User = new ApplicationUser
            {
                UserName = student2Email,
                Email = student2Email,
                EmailConfirmed = true
            };

            var result2 = await userManager.CreateAsync(student2User, "Password123!");
            if (result2.Succeeded)
            {
                await userManager.AddToRoleAsync(student2User, "Student");
            }
        }
    }

    /// <summary>
    /// Idempotent seeding of subjects, rooms, teachers, terms, study groups and
    /// term-to-group links from the JSON timetable. Safe to run on every startup.
    /// </summary>
    public static async Task SeedFromJsonAsync(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        var jsonPath = Path.Combine(Directory.GetCurrentDirectory(), "finki_raspored.json");
        var json = await File.ReadAllTextAsync(jsonPath);
        var data = JsonSerializer.Deserialize<JsonData>(json, JsonOptions);

        if (data is null)
        {
            return;
        }

        // ---------- Study groups (top-level "classes") ----------
        var groupsById = new Dictionary<string, StudyGroup>();
        var existingGroups = await context.StudyGroups.ToListAsync();
        foreach (var cls in data.Classes)
        {
            if (string.IsNullOrWhiteSpace(cls.Name))
            {
                continue;
            }

            var group = existingGroups.FirstOrDefault(g => g.Name == cls.Name);
            if (group is null)
            {
                group = new StudyGroup
                {
                    Name = cls.Name,
                    ShortName = string.IsNullOrWhiteSpace(cls.Short) ? null : cls.Short
                };
                context.StudyGroups.Add(group);
                await context.SaveChangesAsync();
                existingGroups.Add(group);
            }

            if (!string.IsNullOrWhiteSpace(cls.Id))
            {
                groupsById[cls.Id] = group;
            }
        }

        // ---------- Subjects ----------
        var subjectsByName = new Dictionary<string, Subject>();
        var existingSubjects = await context.Subjects.ToListAsync();
        foreach (var jsonSubject in data.Subjects)
        {
            if (subjectsByName.ContainsKey(jsonSubject.Name))
            {
                continue;
            }

            var subject = existingSubjects.FirstOrDefault(s => s.Name == jsonSubject.Name);
            if (subject is null)
            {
                subject = new Subject { Name = jsonSubject.Name, Description = "/" };
                context.Subjects.Add(subject);
                await context.SaveChangesAsync();
                existingSubjects.Add(subject);
            }

            subjectsByName[jsonSubject.Name] = subject;
        }

        // ---------- Rooms ----------
        var roomsByName = new Dictionary<string, Room>();
        var existingRooms = await context.Rooms.ToListAsync();
        foreach (var jsonRoom in data.Classrooms)
        {
            if (roomsByName.ContainsKey(jsonRoom.Name))
            {
                continue;
            }

            var room = existingRooms.FirstOrDefault(r => r.Name == jsonRoom.Name);
            if (room is null)
            {
                room = new Room { Name = jsonRoom.Name, Capacity = 100 };
                context.Rooms.Add(room);
                await context.SaveChangesAsync();
                existingRooms.Add(room);
            }

            roomsByName[jsonRoom.Name] = room;
        }

        // ---------- Teachers ----------
        // Imported teacher ids are numeric strings; historic ids carried a leading
        // minus sign that we do NOT want in the visible/imported identifier.
        // Each teacher account keeps its Identity primary key; only the email/user
        // name is normalized to `teacher_<abs(id)>@finki.ukim.mk`. Collisions are
        // resolved deterministically with a documented fallback suffix.
        var teacherAccounts = new Dictionary<string, (string Email, string UserId)>(StringComparer.Ordinal);
        var usedEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var existingEmail in await context.Users.Select(u => u.Email).Where(e => e != null).ToListAsync())
        {
            usedEmails.Add(existingEmail!);
        }

        foreach (var jsonTeacher in data.Teachers)
        {
            var rawId = jsonTeacher.Id;
            var normalizedId = NormalizeTeacherId(rawId);
            var desiredEmail = $"teacher_{normalizedId}@finki.ukim.mk";
            var legacyEmail = $"teacher_{rawId}@finki.ukim.mk";

            // The account that belongs to THIS imported teacher: look it up by its
            // legacy address first (a minus-prefixed address from earlier seeders),
            // because the normalized address may already belong to a different
            // teacher whenever the raw ids collide (e.g. -72 and 72).
            var teacher = await userManager.FindByEmailAsync(legacyEmail)
                          ?? await userManager.FindByNameAsync(legacyEmail);

            if (teacher is null)
            {
                // No legacy account; reuse an existing account that already owns
                // the normalized address (e.g. after a fresh normalized seeding),
                // or create one.
                teacher = await userManager.FindByEmailAsync(desiredEmail)
                          ?? await userManager.FindByNameAsync(desiredEmail);

                if (teacher is null)
                {
                    teacher = new ApplicationUser
                    {
                        UserName = desiredEmail,
                        Email = desiredEmail,
                        EmailConfirmed = true
                    };

                    var result = await userManager.CreateAsync(teacher, "Password123!");
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(teacher, "Teacher");
                    }
                }
            }

            // Rename the account to the normalized email unless another account
            // already owns it (collision between e.g. -72 and 72), in which case
            // fall back to a deterministic, still-unique address.
            var ownerOfDesired = await userManager.FindByEmailAsync(desiredEmail)
                                 ?? await userManager.FindByNameAsync(desiredEmail);
            string finalEmail;
            if (ownerOfDesired is null || ownerOfDesired.Id == teacher.Id)
            {
                finalEmail = desiredEmail;
            }
            else
            {
                finalEmail = UniqueFallbackEmail(desiredEmail, usedEmails);
                Console.WriteLine($"[dbseeder] teacher id collision for '{desiredEmail}': keeping " +
                                  $"user {ownerOfDesired.Email} unchanged; assigning '{finalEmail}' " +
                                  $"to imported teacher for id {rawId}.");
            }

            if (!string.Equals(teacher.Email, finalEmail, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(teacher.UserName, finalEmail, StringComparison.OrdinalIgnoreCase))
            {
                teacher.Email = finalEmail;
                teacher.UserName = finalEmail;
                teacher.FullName = jsonTeacher.Name;
                await userManager.UpdateAsync(teacher);
            }
            else if (teacher.FullName != jsonTeacher.Name)
            {
                teacher.FullName = jsonTeacher.Name;
                await userManager.UpdateAsync(teacher);
            }

            usedEmails.Add(finalEmail);
            teacherAccounts[jsonTeacher.Id] = (finalEmail, teacher.Id);
        }

        static string NormalizeTeacherId(string id)
        {
            if (long.TryParse(id, out var value) && value < 0)
            {
                return Math.Abs(value).ToString();
            }
            return id;
        }

        static string UniqueFallbackEmail(string desiredEmail, ISet<string> used)
        {
            var baseName = desiredEmail.Split('@')[0];
            var domain = desiredEmail.Split('@')[1];
            for (var i = 1; i < 100; i++)
            {
                var candidate = $"{baseName}_{i}@{domain}";
                if (!used.Contains(candidate))
                {
                    return candidate;
                }
            }
            return $"{baseName}_{Guid.NewGuid():N}@{domain}";
        }

        // ---------- Regular class terms ----------
        var existingTerms = await context.RegularClassTerms
            .Include(t => t.Subject)
            .Include(t => t.Room)
            .Include(t => t.Teacher)
            .ToListAsync();

        string CardKey(JsonCard card) =>
            $"{card.Subject.Name}|{card.DayIndex + 1}|{card.StartTime}|{card.EndTime}|{card.Classrooms[0].Name}|" +
            teacherAccounts[card.Teachers[0].Id].Email;

        string TermKey(RegularClassTerm term) =>
            $"{term.Subject.Name}|{(int)term.DayOfWeek}|{term.StartTime.ToString(@"hh\:mm")}|{term.EndTime.ToString(@"hh\:mm")}|{term.Room.Name}|{term.Teacher.UserName}";

        var cardsByKey = new Dictionary<string, JsonCard>(StringComparer.Ordinal);
        foreach (var card in data.Cards)
        {
            if (card.Classrooms.Count == 0 || card.Teachers.Count == 0 || card.Subject is null)
            {
                continue;
            }

            cardsByKey[CardKey(card)] = card;
        }

        // Backfill the source card id on terms created before SourceId existed,
        // using the deterministic attribute key (verified unique in the source data).
        foreach (var term in existingTerms.Where(t => string.IsNullOrWhiteSpace(t.SourceId)))
        {
            if (cardsByKey.TryGetValue(TermKey(term), out var card))
            {
                term.SourceId = card.Id;
            }
        }

        var termsBySource = new Dictionary<string, RegularClassTerm>(StringComparer.Ordinal);
        foreach (var term in existingTerms.Where(t => !string.IsNullOrWhiteSpace(t.SourceId)))
        {
            termsBySource[term.SourceId!] = term;
        }

        // ---------- Existing term-group links ----------
        var linkSet = (await context.RegularClassTermGroups
                .Select(x => new { x.RegularClassTermId, x.StudyGroupId })
                .ToListAsync())
            .Select(x => $"{x.RegularClassTermId}|{x.StudyGroupId}")
            .ToHashSet();

        // ---------- Cards -> terms + groups ----------
        foreach (var card in data.Cards)
        {
            if (card.Classrooms.Count == 0 || card.Teachers.Count == 0 || card.Subject is null)
            {
                continue;
            }

            if (!subjectsByName.TryGetValue(card.Subject.Name, out var subject) ||
                !roomsByName.TryGetValue(card.Classrooms[0].Name, out var room) ||
                !teacherAccounts.TryGetValue(card.Teachers[0].Id, out var teacher))
            {
                continue;
            }

            if (!termsBySource.TryGetValue(card.Id, out var term))
            {
                term = new RegularClassTerm
                {
                    DayOfWeek = (DayOfWeek)(card.DayIndex + 1),
                    StartTime = TimeSpan.Parse(card.StartTime),
                    EndTime = TimeSpan.Parse(card.EndTime),
                    TermType = "Lecture",
                    SubjectId = subject.Id,
                    RoomId = room.Id,
                    TeacherId = teacher.UserId,
                    SourceId = card.Id
                };
                context.RegularClassTerms.Add(term);
                await context.SaveChangesAsync();
                termsBySource[card.Id] = term;
            }

            foreach (var cls in card.Classes)
            {
                if (string.IsNullOrWhiteSpace(cls.Id) || !groupsById.TryGetValue(cls.Id, out var group))
                {
                    continue;
                }

                if (term.Id == 0)
                {
                    await context.SaveChangesAsync();
                }

                var linkKey = $"{term.Id}|{group.Id}";
                if (!linkSet.Contains(linkKey))
                {
                    context.RegularClassTermGroups.Add(new RegularClassTermGroup
                    {
                        RegularClassTermId = term.Id,
                        StudyGroupId = group.Id
                    });
                    linkSet.Add(linkKey);
                }
            }
        }

        await context.SaveChangesAsync();
    }

    public record JsonData(List<JsonCard> Cards, List<JsonSubject> Subjects, List<JsonTeacher> Teachers, List<JsonRoom> Classrooms, List<JsonClass> Classes);

    public record JsonSubject(string Id, string Name);

    public record JsonTeacher(string Id, string Name);

    public record JsonRoom(string Id, string Name);

    public record JsonClass(string? Id, string Name, string? Short);

    public record JsonCard(string Id, int DayIndex, string StartTime, string EndTime, JsonSubject Subject, List<JsonRoom> Classrooms, List<JsonTeacher> Teachers, List<JsonClass> Classes);
}