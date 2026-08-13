using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using uni_schedule_builder.Data;
using uni_schedule_builder.Models;
using uni_schedule_builder.Models.ViewModels;

namespace uni_schedule_builder.Controllers;

[Authorize(Roles = "Student, Teacher, Admin")]
public class ScheduleController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public ScheduleController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(int? groupId, int? subjectId, string? teacherId)
    {
        var groups = await _context.StudyGroups.OrderBy(g => g.Name).ToListAsync();

        var effectiveGroupId = groupId;
        if (!effectiveGroupId.HasValue || !groups.Any(g => g.Id == effectiveGroupId.Value))
        {
            effectiveGroupId = groups.FirstOrDefault(g => g.Name == "1y-SEIS")?.Id
                               ?? groups.FirstOrDefault()?.Id;
        }

        var selectedGroupName = groups.FirstOrDefault(g => g.Id == effectiveGroupId)?.Name;

        var query = _context.RegularClassTerms
            .Include(t => t.Subject)
            .Include(t => t.Room)
            .Include(t => t.Teacher)
            .Include(t => t.RegularClassTermGroups)
                .ThenInclude(rctg => rctg.StudyGroup)
            .AsQueryable();

        if (effectiveGroupId.HasValue)
        {
            query = query.Where(t => t.RegularClassTermGroups.Any(rctg => rctg.StudyGroupId == effectiveGroupId.Value));
        }

        if (subjectId.HasValue)
        {
            query = query.Where(t => t.SubjectId == subjectId.Value);
        }

        if (!string.IsNullOrWhiteSpace(teacherId))
        {
            query = query.Where(t => t.TeacherId == teacherId);
        }

        var terms = await query.ToListAsync();

        ViewData["ShowAddException"] = false;
        ViewData["ShowAdminActions"] = User.IsInRole("Admin");
        ViewData["CurrentGroupId"] = effectiveGroupId;
        ViewData["CurrentSubjectId"] = subjectId;
        ViewData["CurrentTeacherId"] = teacherId;

        var model = new ScheduleFilterViewModel
        {
            Terms = terms
                .OrderBy(t => t.DayOfWeek)
                .ThenBy(t => t.StartTime)
                .ToList(),
            SelectedGroupId = effectiveGroupId,
            SelectedSubjectId = subjectId,
            SelectedTeacherId = teacherId,
            SelectedGroupName = selectedGroupName,
            Groups = groups,
            Subjects = await _context.Subjects.OrderBy(s => s.Name).ToListAsync(),
            Teachers = (await _context.RegularClassTerms
                    .Where(t => t.Teacher != null)
                    .Select(t => new { t.TeacherId, Name = t.Teacher.FullName ?? t.Teacher.UserName })
                    .Where(x => x.Name != null)
                    .Distinct()
                    .ToListAsync())
                .OrderBy(t => t.Name, StringComparer.Ordinal)
                .Select(t => (t.TeacherId, t.Name!))
                .ToList()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddToMySchedule(int termId, int? groupId, int? subjectId, string? teacherId)
    {
        if (!User.IsInRole("Student"))
        {
            return Forbid();
        }

        var student = await _userManager.GetUserAsync(User);
        if (student is null)
        {
            return Challenge();
        }

        var termExists = await _context.RegularClassTerms.AnyAsync(t => t.Id == termId);
        if (!termExists)
        {
            return NotFound();
        }

        var alreadyAdded = await _context.StudentSchedules
            .AnyAsync(ss => ss.StudentId == student.Id && ss.RegularClassTermId == termId);

        if (alreadyAdded)
        {
            TempData["Message"] = "This class is already in your schedule.";
        }
        else
        {
            _context.StudentSchedules.Add(new StudentSchedule
            {
                StudentId = student.Id,
                RegularClassTermId = termId
            });
            await _context.SaveChangesAsync();
            TempData["Message"] = "Class added to your schedule.";
        }

        var routeValues = new RouteValueDictionary();
        if (groupId.HasValue)
        {
            routeValues["groupId"] = groupId;
        }
        if (subjectId.HasValue)
        {
            routeValues["subjectId"] = subjectId;
        }
        if (!string.IsNullOrWhiteSpace(teacherId))
        {
            routeValues["teacherId"] = teacherId;
        }

        return RedirectToAction(nameof(Index), routeValues);
    }

    // ---------- Admin timetable management (Admin only) ----------

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create()
    {
        var model = new AdminTermViewModel
        {
            DayOfWeek = 1,
            TermType = "Lecture"
        };
        await PopulateAdminTermAsync(model);
        return View(model);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AdminTermViewModel model)
    {
        if (!TryNormalizeTimes(model, out var start, out var end))
        {
            await PopulateAdminTermAsync(model);
            return View(model);
        }

        if (ModelState.IsValid)
        {
            var term = new RegularClassTerm
            {
                DayOfWeek = (DayOfWeek)model.DayOfWeek,
                StartTime = start,
                EndTime = end,
                TermType = model.TermType,
                SubjectId = model.SubjectId,
                RoomId = model.RoomId,
                TeacherId = model.TeacherId
            };
            _context.RegularClassTerms.Add(term);
            await _context.SaveChangesAsync();

            foreach (var groupId in model.SelectedGroupIds)
            {
                var link = new RegularClassTermGroup
                {
                    RegularClassTermId = term.Id,
                    StudyGroupId = groupId
                };
                _context.RegularClassTermGroups.Add(link);
            }
            await _context.SaveChangesAsync();

            TempData["Message"] = "Term created.";
            return RedirectToAction(nameof(Index));
        }

        await PopulateAdminTermAsync(model);
        return View(model);
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int id)
    {
        var term = await _context.RegularClassTerms
            .Include(t => t.RegularClassTermGroups)
            .FirstOrDefaultAsync(t => t.Id == id);
        if (term is null)
        {
            return NotFound();
        }

        var model = new AdminTermViewModel
        {
            Id = term.Id,
            DayOfWeek = (int)term.DayOfWeek,
            StartTime = term.StartTime.ToString(@"hh\:mm"),
            EndTime = term.EndTime.ToString(@"hh\:mm"),
            TermType = term.TermType,
            SubjectId = term.SubjectId,
            RoomId = term.RoomId,
            TeacherId = term.TeacherId,
            SelectedGroupIds = term.RegularClassTermGroups.Select(g => g.StudyGroupId).ToList()
        };
        await PopulateAdminTermAsync(model);
        return View(model);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, AdminTermViewModel model)
    {
        var term = await _context.RegularClassTerms
            .Include(t => t.RegularClassTermGroups)
            .FirstOrDefaultAsync(t => t.Id == id);
        if (term is null)
        {
            return NotFound();
        }

        model.Id = id;

        if (!TryNormalizeTimes(model, out var start, out var end))
        {
            await PopulateAdminTermAsync(model);
            return View(model);
        }

        if (ModelState.IsValid)
        {
            term.DayOfWeek = (DayOfWeek)model.DayOfWeek;
            term.StartTime = start;
            term.EndTime = end;
            term.TermType = model.TermType;
            term.SubjectId = model.SubjectId;
            term.RoomId = model.RoomId;
            term.TeacherId = model.TeacherId;

            var currentGroupIds = term.RegularClassTermGroups.Select(g => g.StudyGroupId).ToList();
            foreach (var groupId in model.SelectedGroupIds.Except(currentGroupIds))
            {
                _context.RegularClassTermGroups.Add(new RegularClassTermGroup
                {
                    RegularClassTermId = term.Id,
                    StudyGroupId = groupId
                });
            }
            foreach (var link in term.RegularClassTermGroups
                .Where(g => !model.SelectedGroupIds.Contains(g.StudyGroupId))
                .ToList())
            {
                _context.RegularClassTermGroups.Remove(link);
            }

            await _context.SaveChangesAsync();
            TempData["Message"] = "Term updated.";
            return RedirectToAction(nameof(Index));
        }

        await PopulateAdminTermAsync(model);
        return View(model);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var term = await _context.RegularClassTerms.FirstOrDefaultAsync(t => t.Id == id);
        if (term is null)
        {
            return NotFound();
        }

        var studentSchedules = _context.StudentSchedules.Where(ss => ss.RegularClassTermId == id);
        _context.StudentSchedules.RemoveRange(studentSchedules);

        var exceptions = _context.TermExceptions.Where(e => e.RegularClassTermId == id);
        _context.TermExceptions.RemoveRange(exceptions);

        var links = _context.RegularClassTermGroups.Where(l => l.RegularClassTermId == id);
        _context.RegularClassTermGroups.RemoveRange(links);

        _context.RegularClassTerms.Remove(term);
        await _context.SaveChangesAsync();

        TempData["Message"] = "Term deleted.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateAdminTermAsync(AdminTermViewModel model)
    {
        model.DayOptions = Enumerable.Range(1, 5)
            .Select(d => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
            {
                Value = d.ToString(),
                Text = ((DayOfWeek)d).ToString(),
                Selected = model.DayOfWeek == d
            })
            .ToList();

        var termTypes = new[] { "Lecture", "Lab", "Seminar", "Exercise", "Other" };
        model.TermTypeOptions = termTypes
            .Select(t => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
            {
                Value = t,
                Text = t,
                Selected = string.Equals(t, model.TermType, StringComparison.OrdinalIgnoreCase)
            })
            .ToList();

        model.SubjectOptions = (await _context.Subjects.OrderBy(s => s.Name).ToListAsync())
            .Select(s => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
            {
                Value = s.Id.ToString(),
                Text = s.Name,
                Selected = s.Id == model.SubjectId
            })
            .ToList();

        model.RoomOptions = (await _context.Rooms.OrderBy(r => r.Name).ToListAsync())
            .Select(r => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
            {
                Value = r.Id.ToString(),
                Text = $"{r.Name} (Capacity: {r.Capacity})",
                Selected = r.Id == model.RoomId
            })
            .ToList();

        var teachers = (await _userManager.GetUsersInRoleAsync("Teacher"))
            .OrderBy(u => u.FullName ?? u.UserName, StringComparer.Ordinal)
            .ToList();

        model.TeacherOptions = teachers
            .Select(u => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
            {
                Value = u.Id,
                Text = u.FullName ?? u.UserName ?? u.Id,
                Selected = u.Id == model.TeacherId
            })
            .ToList();

        model.Groups = await _context.StudyGroups.OrderBy(g => g.Name).ToListAsync();
    }

    private bool TryNormalizeTimes(AdminTermViewModel model, out TimeSpan start, out TimeSpan end)
    {
        start = TimeSpan.Zero;
        end = TimeSpan.Zero;

        var startOk = TimeSpan.TryParse(model.StartTime, out var parsedStart);
        var endOk = TimeSpan.TryParse(model.EndTime, out var parsedEnd);

        if (!startOk)
        {
            ModelState.AddModelError(nameof(model.StartTime), "Start time must be a valid time.");
        }
        if (!endOk)
        {
            ModelState.AddModelError(nameof(model.EndTime), "End time must be a valid time.");
        }

        if (startOk && endOk)
        {
            if (parsedEnd <= parsedStart)
            {
                ModelState.AddModelError(nameof(model.EndTime), "End time must be after the start time.");
            }
            else
            {
                start = parsedStart;
                end = parsedEnd;
                return true;
            }
        }

        return false;
    }
}