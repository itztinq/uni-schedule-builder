using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using uni_schedule_builder.Data;
using uni_schedule_builder.Models;
using uni_schedule_builder.Models.ViewModels;

namespace uni_schedule_builder.Controllers;

[Authorize(Roles = "Student")]
public class MyScheduleController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public MyScheduleController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var student = await _userManager.GetUserAsync(User);
        if (student is null)
        {
            return Challenge();
        }

        var today = DateTime.Today;

        var entries = await _context.StudentSchedules
            .Include(ss => ss.RegularClassTerm.Subject)
            .Include(ss => ss.RegularClassTerm.Room)
            .Include(ss => ss.RegularClassTerm.Teacher)
            .Include(ss => ss.RegularClassTerm.RegularClassTermGroups)
                .ThenInclude(rctg => rctg.StudyGroup)
            .Where(ss => ss.StudentId == student.Id)
            .ToListAsync();

        var termIds = entries.Select(e => e.RegularClassTermId).Distinct().ToList();

        var exceptions = await _context.TermExceptions
            .Where(e => termIds.Contains(e.RegularClassTermId) && e.SpecificDate >= today)
            .ToListAsync();

        var model = new MyScheduleViewModel
        {
            Entries = entries
                .OrderBy(e => e.RegularClassTerm.DayOfWeek)
                .ThenBy(e => e.RegularClassTerm.StartTime)
                .ToList()
        };

        foreach (var exception in exceptions)
        {
            if (!model.ExceptionsByTerm.TryGetValue(exception.RegularClassTermId, out var list))
            {
                list = new List<TermException>();
                model.ExceptionsByTerm[exception.RegularClassTermId] = list;
            }
            list.Add(exception);
        }

        var occupancyRows = await _context.StudentSchedules
            .Where(ss => termIds.Contains(ss.RegularClassTermId))
            .GroupBy(ss => ss.RegularClassTermId)
            .Select(g => new { TermId = g.Key, Count = g.Count() })
            .ToListAsync();

        var occupancy = new Dictionary<int, (int Count, int Capacity)>();
        foreach (var row in occupancyRows)
        {
            var term = entries.FirstOrDefault(e => e.RegularClassTermId == row.TermId)?.RegularClassTerm;
            if (term != null)
            {
                occupancy[row.TermId] = (row.Count, term.Room.Capacity);
            }
        }

        ViewData["OccupancyByTerm"] = occupancy;

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(int scheduleId)
    {
        var student = await _userManager.GetUserAsync(User);
        if (student is null)
        {
            return Challenge();
        }

        var entry = await _context.StudentSchedules
            .FirstOrDefaultAsync(ss => ss.Id == scheduleId && ss.StudentId == student.Id);

        if (entry is not null)
        {
            _context.StudentSchedules.Remove(entry);
            await _context.SaveChangesAsync();
            TempData["Message"] = "Class removed from your schedule.";
        }

        return RedirectToAction(nameof(Index));
    }
}