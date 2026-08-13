using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using uni_schedule_builder.Data;
using uni_schedule_builder.Models;
using uni_schedule_builder.Models.ViewModels;

namespace uni_schedule_builder.Controllers;

[Authorize(Roles = "Admin")]
public class AdminDashboardController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminDashboardController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var today = DateTime.Today;

        var subjects = await _context.Subjects.CountAsync();
        var rooms = await _context.Rooms.CountAsync();
        var teachers = await _userManager.GetUsersInRoleAsync("Teacher");
        var groups = await _context.StudyGroups.CountAsync();
        var terms = await _context.RegularClassTerms.CountAsync();
        var activeExceptions = await _context.TermExceptions.CountAsync(e => e.SpecificDate >= today);

        var roomUsage = await _context.RegularClassTerms
            .GroupBy(t => t.RoomId)
            .Select(g => new { RoomId = g.Key, TermCount = g.Count() })
            .OrderByDescending(x => x.TermCount)
            .Take(5)
            .ToListAsync();

        var roomIds = roomUsage.Select(x => x.RoomId).ToList();
        var roomNames = await _context.Rooms
            .Where(r => roomIds.Contains(r.Id))
            .ToDictionaryAsync(r => r.Id, r => r.Name);

        var exceptions = await _context.TermExceptions
            .Include(e => e.RegularClassTerm).ThenInclude(t => t.Subject)
            .Include(e => e.RegularClassTerm).ThenInclude(t => t.Teacher)
            .Where(e => e.SpecificDate >= today)
            .OrderBy(e => e.SpecificDate)
            .Take(6)
            .ToListAsync();

        var vm = new AdminDashboardViewModel
        {
            SubjectCount = subjects,
            RoomCount = rooms,
            TeacherCount = teachers.Count,
            StudentGroupCount = groups,
            TermCount = terms,
            ActiveExceptionCount = activeExceptions,
            MostUsedRooms = roomUsage.Select(x => new RoomUsageItem
            {
                RoomName = roomNames.GetValueOrDefault(x.RoomId) ?? $"#{x.RoomId}",
                TermCount = x.TermCount
            }).ToList(),
            UpcomingExceptions = exceptions.Select(e => new ActiveExceptionItem
            {
                SpecificDate = e.SpecificDate,
                SubjectName = e.RegularClassTerm.Subject.Name,
                TeacherName = e.RegularClassTerm.Teacher.FullName ?? e.RegularClassTerm.Teacher.UserName ?? string.Empty,
                Status = e.Status,
                Message = e.Message
            }).ToList()
        };

        return View(vm);
    }
}