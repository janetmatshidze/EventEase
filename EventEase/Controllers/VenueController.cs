using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EventEase.Data;
using EventEase.Models;
using System.IO;
using System.Threading.Tasks;
using System.Linq;

namespace EventEase.Controllers
{
    public class VenueController : Controller
    {
        private readonly EventEaseDBContext _context;
        private readonly BlobStorageService _blobStorageService;

        public VenueController(EventEaseDBContext context, BlobStorageService blobStorageService)
        {
            _context = context;
            _blobStorageService = blobStorageService;
        }

        public async Task<IActionResult> Index()
        {
            var venues = await _context.Venues.ToListAsync();
            return View(venues);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("VenueName,Location,Capacity,ImageUpload")] Venue venue)
        {
            try
            {
                // Remove ImageFile validation errors since it's populated by the controller
                ModelState.Remove("ImageFile");

                if (ModelState.IsValid)
                {
                    // Debug information
                    Console.WriteLine($"Creating venue: {venue.VenueName}, Location: {venue.Location}, Capacity: {venue.Capacity}");

                    // Handle file upload to Azure Blob Storage
                    if (venue.ImageUpload != null && venue.ImageUpload.Length > 0)
                    {
                        try
                        {
                            venue.ImageFile = await _blobStorageService.UploadFileAsync(venue.ImageUpload);
                            Console.WriteLine($"Image uploaded successfully: {venue.ImageFile}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Image upload error: {ex.Message}");
                            ModelState.AddModelError("ImageUpload", $"Failed to upload image: {ex.Message}");
                            return View(venue);
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("ImageUpload", "Please select an image file");
                        return View(venue);
                    }

                    _context.Add(venue);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Venue created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    // Log validation errors for debugging
                    foreach (var state in ModelState)
                    {
                        foreach (var error in state.Value.Errors)
                        {
                            Console.WriteLine($"Property: {state.Key}, Error: {error.ErrorMessage}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating venue: {ex.Message}");
                ModelState.AddModelError(string.Empty, $"Failed to create venue: {ex.Message}");
            }

            return View(venue);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var venue = await _context.Venues.FindAsync(id);
            if (venue == null)
            {
                return NotFound();
            }
            return View(venue);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Venue venue)
        {
            if (id != venue.VenueId)
            {
                return NotFound();
            }

            // Explicitly remove any validation errors for ImageUpload
            ModelState.Remove("ImageUpload");

            if (!ModelState.IsValid)
            {
                // Log validation errors for debugging
                foreach (var state in ModelState)
                {
                    foreach (var error in state.Value.Errors)
                    {
                        Console.WriteLine($"Property: {state.Key}, Error: {error.ErrorMessage}");
                    }
                }
                return View(venue);
            }

            try
            {
                // Get the existing venue to get the old image URL
                var existingVenue = await _context.Venues.AsNoTracking()
                    .FirstOrDefaultAsync(v => v.VenueId == id);

                if (existingVenue == null)
                {
                    return NotFound();
                }

                string oldImageUrl = existingVenue.ImageFile;

                // Handle file upload to Azure Blob Storage
                if (venue.ImageUpload != null && venue.ImageUpload.Length > 0)
                {
                    try
                    {
                        // Upload new image
                        venue.ImageFile = await _blobStorageService.UploadFileAsync(venue.ImageUpload);

                        // Delete old image if it exists
                        if (!string.IsNullOrEmpty(oldImageUrl))
                        {
                            await _blobStorageService.DeleteFileAsync(oldImageUrl);
                        }
                    }
                    catch (System.Exception ex)
                    {
                        // Log the error
                        Console.WriteLine($"Image upload error: {ex.Message}");
                        ModelState.AddModelError("ImageUpload", "Failed to upload image. Please try again.");
                        return View(venue);
                    }
                }
                else
                {
                    // IMPORTANT: If no new image is uploaded, preserve the existing image URL
                    venue.ImageFile = oldImageUrl;
                }

                // Set entity state explicitly
                _context.Entry(venue).State = EntityState.Modified;

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Venue updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException ex)
            {
                Console.WriteLine($"Concurrency error: {ex.Message}");
                if (!VenueExists(venue.VenueId))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            catch (System.Exception ex)
            {
                // Log the general exception
                Console.WriteLine($"Update error: {ex.Message}");
                ModelState.AddModelError(string.Empty, "An error occurred while saving the venue. Please try again.");
                return View(venue);
            }
        }
        public async Task<IActionResult> Delete(int id)
        {
            var venue = await _context.Venues.FindAsync(id);
            if (venue == null)
            {
                return NotFound();
            }

            // Check if venue has any existing bookings
            bool hasBookings = await _context.Bookings.AnyAsync(b => b.VenueId == id);
            if (hasBookings)
            {
                ViewData["ErrorMessage"] = "Cannot delete venue as it has existing bookings.";

                // You could also include a list of bookings for this venue if desired
                var bookings = await _context.Bookings
                    .Where(b => b.VenueId == id)
                    .Include(b => b.Event)
                    .ToListAsync();
                ViewBag.Bookings = bookings;

                return View(venue);
            }

            return View(venue);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var venue = await _context.Venues.FindAsync(id);
            if (venue == null)
            {
                return NotFound();
            }

            // Double-check for existing bookings before deletion
            bool hasBookings = await _context.Bookings.AnyAsync(b => b.VenueId == id);
            if (hasBookings)
            {
                TempData["ErrorMessage"] = "Cannot delete venue as it has existing bookings.";
                return RedirectToAction(nameof(Index));
            }

            // Delete the image from Azure Blob Storage
            if (!string.IsNullOrEmpty(venue.ImageFile))
            {
                await _blobStorageService.DeleteFileAsync(venue.ImageFile);
            }

            _context.Venues.Remove(venue);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Venue deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        private bool VenueExists(int id)
        {
            return _context.Venues.Any(e => e.VenueId == id);
        }
    }
}