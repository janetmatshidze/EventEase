using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EventEase.Data;
using EventEase.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using EventEase.Models.ViewModels;

namespace EventEase.Controllers
{
    public class EventController : Controller
    {
        private readonly EventEaseDBContext _context;

        public EventController(EventEaseDBContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Fetch events 
            var events = await _context.Events.ToListAsync();

            // Create view model
            var viewModel = new EventViewModel(events);

            return View(viewModel);
        }
        public IActionResult Create()
        {
            // Removed ViewBag for Venues since it's not needed
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Event eventModel)
        {
            Console.WriteLine($"Event data: Name={eventModel.EventName}, Date={eventModel.EventDate}, ImageUrl={eventModel.ImageUrl}");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Events.Add(eventModel);
                    int result = await _context.SaveChangesAsync();

                    Console.WriteLine($"SaveChanges result: {result} entity/entities affected");
                    TempData["SuccessMessage"] = "Event created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error saving event: {ex.Message}");
                    Console.WriteLine($"Inner exception: {ex.InnerException?.Message}");
                    ModelState.AddModelError("", $"Could not save to database: {ex.Message}");
                }
            }
            else
            {
                // Log validation errors
                foreach (var state in ModelState)
                {
                    foreach (var error in state.Value.Errors)
                    {
                        Console.WriteLine($"Validation error - {state.Key}: {error.ErrorMessage}");
                    }
                }
            }

            return View(eventModel);
        }
        public async Task<IActionResult> Edit(int id)
        {
            var eventModel = await _context.Events.FindAsync(id);
            if (eventModel == null)
            {
                return NotFound();
            }

            return View(eventModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Event eventModel)
        {
            if (id != eventModel.EventId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(eventModel);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Event updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Events.Any(e => e.EventId == id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error updating: {ex.Message}");
                }
            }

            return View(eventModel);
        }
        public async Task<IActionResult> Delete(int id)
        {
            var eventModel = await _context.Events.FindAsync(id);
            if (eventModel == null)
            {
                return NotFound();
            }

            // Check if event has any existing bookings
            bool hasBookings = await _context.Bookings.AnyAsync(b => b.EventId == id);
            if (hasBookings)
            {
                // Add error message to ViewData for immediate display
                ViewData["ErrorMessage"] = "Cannot delete event as it has existing bookings.";

                // Retrieve bookings related to this event to display
                var bookings = await _context.Bookings
                    .Where(b => b.EventId == id)
                    .Include(b => b.Venue)
                    .ToListAsync();

                ViewBag.Bookings = bookings;
            }

            return View(eventModel);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var eventModel = await _context.Events.FindAsync(id);
            if (eventModel == null)
            {
                return NotFound();
            }

            // Check for existing bookings before deletion
            bool hasBookings = await _context.Bookings.AnyAsync(b => b.EventId == id);
            if (hasBookings)
            {
                TempData["ErrorMessage"] = "Cannot delete event as it has existing bookings.";
                return RedirectToAction(nameof(Index));
            }

            _context.Events.Remove(eventModel);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Event deleted successfully!";

            return RedirectToAction(nameof(Index));
        }
    }
}
