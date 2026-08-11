using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using uni_schedule_builder.Data;
using uni_schedule_builder.Models;

namespace uni_schedule_builder.Controllers;

[Authorize(Roles = "Admin")]
public class AdminSubjectsController : Controller
{
    private readonly ApplicationDbContext _context;

    public AdminSubjectsController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        return View(await _context.Subjects.ToListAsync());
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var subject = await _context.Subjects
            .FirstOrDefaultAsync(m => m.Id == id);
        if (subject == null)
        {
            return NotFound();
        }

        return View(subject);
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Name,Description")] Subject subject)
    {
        if (ModelState.IsValid)
        {
            _context.Add(subject);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        return View(subject);
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var subject = await _context.Subjects.FindAsync(id);
        if (subject == null)
        {
            return NotFound();
        }

        return View(subject);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description")] Subject subject)
    {
        if (id != subject.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(subject);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SubjectExists(subject.Id))
                {
                    return NotFound();
                }

                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        return View(subject);
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var subject = await _context.Subjects
            .FirstOrDefaultAsync(m => m.Id == id);
        if (subject == null)
        {
            return NotFound();
        }

        return View(subject);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var subject = await _context.Subjects.FindAsync(id);
        if (subject != null)
        {
            _context.Subjects.Remove(subject);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    private bool SubjectExists(int id)
    {
        return _context.Subjects.Any(e => e.Id == id);
    }
}
