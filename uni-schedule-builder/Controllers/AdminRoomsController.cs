using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using uni_schedule_builder.Data;
using uni_schedule_builder.Models;

namespace uni_schedule_builder.Controllers;

[Authorize(Roles = "Admin")]
public class AdminRoomsController : Controller
{
    private readonly ApplicationDbContext _context;

    public AdminRoomsController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        return View(await _context.Rooms.ToListAsync());
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var room = await _context.Rooms
            .FirstOrDefaultAsync(m => m.Id == id);
        if (room == null)
        {
            return NotFound();
        }

        return View(room);
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Name,Capacity")] Room room)
    {
        if (ModelState.IsValid)
        {
            _context.Add(room);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        return View(room);
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var room = await _context.Rooms.FindAsync(id);
        if (room == null)
        {
            return NotFound();
        }

        return View(room);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Capacity")] Room room)
    {
        if (id != room.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(room);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!RoomExists(room.Id))
                {
                    return NotFound();
                }

                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        return View(room);
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var room = await _context.Rooms
            .FirstOrDefaultAsync(m => m.Id == id);
        if (room == null)
        {
            return NotFound();
        }

        return View(room);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var room = await _context.Rooms.FindAsync(id);
        if (room != null)
        {
            _context.Rooms.Remove(room);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    private bool RoomExists(int id)
    {
        return _context.Rooms.Any(e => e.Id == id);
    }
}
