using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using uni_schedule_builder.Data;
using uni_schedule_builder.Models;
using uni_schedule_builder.Models.ViewModels;

namespace uni_schedule_builder.Controllers;

[Authorize(Roles = "Teacher")]
public class TeacherExceptionsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public TeacherExceptionsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var teacherId = _userManager.GetUserId(User);

        var terms = await _context.RegularClassTerms
            .Include(t => t.Subject)
            .Include(t => t.Room)
            .Include(t => t.Teacher)
            .Include(t => t.RegularClassTermGroups)
                .ThenInclude(rctg => rctg.StudyGroup)
            .Where(t => t.TeacherId == teacherId)
            .ToListAsync();

        return View(terms
            .OrderBy(t => t.DayOfWeek)
            .ThenBy(t => t.StartTime));
    }

    public async Task<IActionResult> Create(int termId)
    {
        var term = await GetOwnTermAsync(termId);
        if (term == null)
        {
            return NotFound();
        }

        var model = new TeacherExceptionViewModel
        {
            RegularClassTermId = term.Id,
            SpecificDate = DateTime.Today,
            ExceptionTime = term.StartTime,
            TermInfo = FormatTerm(term),
            StatusOptions = GetStatusOptions(),
            Rooms = GetRoomOptions(null)
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TeacherExceptionViewModel model)
    {
        var term = await GetOwnTermAsync(model.RegularClassTermId);
        if (term == null)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            var exception = new TermException
            {
                RegularClassTermId = model.RegularClassTermId,
                SpecificDate = model.SpecificDate.Date + (model.ExceptionTime ?? TimeSpan.Zero),
                Status = model.Status,
                Message = model.Message,
                NewRoomId = model.Status == "Relocated" ? model.NewRoomId : null
            };

            _context.TermExceptions.Add(exception);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        model.TermInfo = FormatTerm(term);
        model.StatusOptions = GetStatusOptions(model.Status);
        model.Rooms = GetRoomOptions(model.NewRoomId);

        return View(model);
    }

    private async Task<RegularClassTerm?> GetOwnTermAsync(int termId)
    {
        var teacherId = _userManager.GetUserId(User);

        return await _context.RegularClassTerms
            .Include(t => t.Subject)
            .Include(t => t.Room)
            .Include(t => t.Teacher)
            .FirstOrDefaultAsync(t => t.Id == termId && t.TeacherId == teacherId);
    }

    private static string FormatTerm(RegularClassTerm term)
    {
        return $"{term.Subject.Name} - {term.DayOfWeek} {term.StartTime:hh\\:mm}-{term.EndTime:hh\\:mm} at {term.Room.Name}";
    }

    private static List<SelectListItem> GetStatusOptions(string? selected = null)
    {
        return new List<SelectListItem>
        {
            new() { Value = "Canceled", Text = "Canceled", Selected = (selected ?? "Canceled") == "Canceled" },
            new() { Value = "Relocated", Text = "Relocated", Selected = selected == "Relocated" },
            new() { Value = "Note", Text = "Note", Selected = selected == "Note" }
        };
    }

    private List<SelectListItem> GetRoomOptions(int? selectedId)
    {
        var rooms = _context.Rooms.OrderBy(r => r.Name).ToList();

        return rooms.Select(r => new SelectListItem
        {
            Value = r.Id.ToString(),
            Text = $"{r.Name} (Capacity: {r.Capacity})",
            Selected = r.Id == selectedId
        }).ToList();
    }
}