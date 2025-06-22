using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EventEase.Data;
using EventEase.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using EventEase.Models.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EventEase.Controllers
{
    public class EventController : Controller
    {
        private readonly EventEaseDBContext _context;
        private readonly BlobStorageService _blobStorageService;

        public EventController(EventEaseDBContext context, BlobStorageService blobStorageService)
        {
            _context = context;
            _blobStorageService = blobStorageService;
        }

        public async Task<IActionResult> Index()
        {
            // Fetch events 
            var events = await _context.Events
         .Include(e => e.EventType) // Include EventType for each event
         .ToListAsync();

            // Create view model
            var viewModel = new EventViewModel(events);



            return View(viewModel);


        }

        public async Task<IActionResult> Create()
        {
            ViewBag.EventTypes = await _context.EventTypes
                .OrderBy(et => et.Name)
                .ToListAsync();

            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Event eventModel)
        {
            // Remove ImageFile validation if only ImageUpload is required
            ModelState.Remove("ImageFile");

            // Remove EventType navigation property validation since we're setting EventTypeId
            ModelState.Remove("EventType");

            // Load event types before any logic, so they're available to the view regardless
            ViewBag.EventTypes = await _context.EventTypes
                .OrderBy(et => et.Name)
                .ToListAsync();

            if (ModelState.IsValid)
            {
                try
                {
                    if (eventModel.ImageUpload != null)
                    {
                        eventModel.ImageFile = await _blobStorageService.UploadFileAsync(eventModel.ImageUpload);
                        Console.WriteLine($"Uploaded image to: {eventModel.ImageFile}");
                    }

                    // Ensure EventTypeId is set correctly
                    if (eventModel.EventTypeId <= 0)
                    {
                        ModelState.AddModelError("EventTypeId", "Please select an event type.");
                        return View(eventModel);
                    }

                    _context.Events.Add(eventModel);
                    int result = await _context.SaveChangesAsync();

                    Console.WriteLine($"SaveChanges result: {result} entity/entities affected");
                    TempData["SuccessMessage"] = "Event created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error saving event: {ex.Message}");
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
        // Optimal Edit GET method using SelectList
        public async Task<IActionResult> Edit(int id)
        {
            var eventModel = await _context.Events.FindAsync(id);
            if (eventModel == null)
            {
                return NotFound();
            }

            // Create SelectList with the current event's EventTypeId pre-selected
            ViewBag.EventTypes = new SelectList(
                await _context.EventTypes.OrderBy(et => et.Name).ToListAsync(),
                "EventTypeId",  // Value field
                "Name",         // Text field  
                eventModel.EventTypeId  // Selected value
            );

            return View(eventModel);
        }

        // Updated Edit POST method
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Event eventModel)
        {
            if (id != eventModel.EventId)
            {
                return NotFound();
            }

            // Remove ImageFile, ImageUpload, and EventType validation
            ModelState.Remove("ImageFile");
            ModelState.Remove("ImageUpload");
            ModelState.Remove("EventType");

            if (ModelState.IsValid)
            {
                try
                {
                    // Get the existing event to check if we need to delete old image
                    var existingEvent = await _context.Events.AsNoTracking()
                        .FirstOrDefaultAsync(e => e.EventId == id);

                    if (existingEvent == null)
                    {
                        return NotFound();
                    }

                    string oldImageUrl = existingEvent.ImageFile;

                    // Handle image upload
                    if (eventModel.ImageUpload != null && eventModel.ImageUpload.Length > 0)
                    {
                        try
                        {
                            // Delete old image if it exists
                            if (!string.IsNullOrEmpty(oldImageUrl))
                            {
                                await _blobStorageService.DeleteFileAsync(oldImageUrl);
                            }

                            // Upload new image
                            eventModel.ImageFile = await _blobStorageService.UploadFileAsync(eventModel.ImageUpload);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Image upload error: {ex.Message}");
                            ModelState.AddModelError("ImageUpload", "Failed to upload image. Please try again.");

                            // Repopulate SelectList for error case
                            ViewBag.EventTypes = new SelectList(
                                await _context.EventTypes.OrderBy(et => et.Name).ToListAsync(),
                                "EventTypeId", "Name", eventModel.EventTypeId);

                            return View(eventModel);
                        }
                    }
                    else
                    {
                        // IMPORTANT: If no new image is uploaded, preserve the existing image URL
                        eventModel.ImageFile = oldImageUrl;
                    }

                    // Explicitly set the entity state
                    _context.Entry(eventModel).State = EntityState.Modified;

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
                    Console.WriteLine($"Update error: {ex.Message}");
                    ModelState.AddModelError("", $"Error updating: {ex.Message}");
                }
            }

            // Repopulate SelectList if validation fails
            if (!ModelState.IsValid)
            {
                ViewBag.EventTypes = new SelectList(
                    await _context.EventTypes.OrderBy(et => et.Name).ToListAsync(),
                    "EventTypeId", "Name", eventModel.EventTypeId);

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

            // Delete the image from Azure if it exists
            if (!string.IsNullOrEmpty(eventModel.ImageFile))
            {
                await _blobStorageService.DeleteFileAsync(eventModel.ImageFile);
            }

            _context.Events.Remove(eventModel);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Event deleted successfully!";

            return RedirectToAction(nameof(Index));
        }
    }
}