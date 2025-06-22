using EventEase.Data;
using EventEase.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EventEase.Controllers
{
    public class BookingController : Controller
    {
        private readonly EventEaseDBContext _context;

        private const string BookingSpecialistEmail = "specialist@eventease.com";
        private const string BookingSpecialistPassword = "Specialist123!";
        public BookingController(EventEaseDBContext context)
        {
            _context = context;
        }

        // GET: Booking
        public async Task<IActionResult> Index()
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");

            if (string.IsNullOrEmpty(userEmail))
            {
                return RedirectToAction("Login", "Account");
            }

            var bookings = await _context.Bookings
                .Include(b => b.Event)
                .Include(b => b.Venue)
                .Where(b => b.Email == userEmail)
                .ToListAsync();

            return View(bookings);
        }

        // GET: Booking/Create
        public async Task<IActionResult> Create()
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");

            if (string.IsNullOrEmpty(userEmail))
            {
                return RedirectToAction("Login", "Account");
            }

            ViewBag.Events = await _context.Events
                .Where(e => e.EventDate >= DateTime.Now.Date)
                .ToListAsync();

            ViewBag.Venues = await _context.Venues.ToListAsync();

            return View(new Booking
            {
                BookingDate = DateTime.Now.Date,
                Email = userEmail
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Booking booking)
        {
            if (ModelState.ContainsKey("EventName"))
                ModelState.Remove("EventName");

            if (ModelState.ContainsKey("VenueName"))
                ModelState.Remove("VenueName");

            var userEmail = HttpContext.Session.GetString("UserEmail");

            if (string.IsNullOrEmpty(userEmail))
            {
                return RedirectToAction("Login", "Account");
            }

            booking.Email = userEmail;

            // Fetch user ID
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
            if (user != null)
            {
                booking.UserId = user.UserId;
            }

            // Fetch Event
            var selectedEvent = await _context.Events.FirstOrDefaultAsync(e => e.EventId == booking.EventId);
            if (selectedEvent == null)
            {
                ModelState.AddModelError("EventId", "The selected event does not exist.");
            }
            else
            {
                booking.EventName = selectedEvent.EventName;
            }

            // Fetch Venue
            var selectedVenue = await _context.Venues.FirstOrDefaultAsync(v => v.VenueId == booking.VenueId);
            if (selectedVenue == null)
            {
                ModelState.AddModelError("VenueId", "The selected venue does not exist.");
            }
            else
            {
                booking.VenueName = selectedVenue.VenueName;
            }

            // Validate Booking Date
            if (booking.BookingDate.Date < DateTime.Now.Date)
            {
                ModelState.AddModelError("BookingDate", "Booking date cannot be in the past.");
            }

            if (selectedEvent != null && selectedEvent.EventDate.Date < booking.BookingDate.Date)
            {
                ModelState.AddModelError("BookingDate", "Booking date cannot be after the event date.");
            }

            // Ensure venue is not double-booked for the same date
            var existingBooking = await _context.Bookings
                .AnyAsync(b => b.VenueId == booking.VenueId && b.BookingDate == booking.BookingDate);

            if (existingBooking)
            {
                ModelState.AddModelError("VenueId", "This venue is already booked for the selected date.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Bookings.Add(booking);
                    await _context.SaveChangesAsync();

                    // Auto-sync venue availability after creating booking
                    await SyncVenueAvailabilityForVenue(booking.VenueId.Value);

                    TempData["SuccessMessage"] = "Booking created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "An error occurred while saving the booking: " + ex.Message);
                }
            }

            // Reload dropdowns on error
            ViewBag.Events = await _context.Events.Where(e => e.EventDate >= DateTime.Now.Date).ToListAsync();
            ViewBag.Venues = await _context.Venues.ToListAsync();

            return View(booking);
        }
        // GET: Booking/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");

            if (string.IsNullOrEmpty(userEmail))
            {
                return RedirectToAction("Login", "Account");
            }

            if (id == null)
            {
                return NotFound();
            }

            var booking = await _context.Bookings
                .Include(b => b.Event)
                .Include(b => b.Venue)
                .FirstOrDefaultAsync(m => m.BookingId == id && m.Email == userEmail);

            if (booking == null)
            {
                return NotFound();
            }

            // Load dropdown data
            ViewBag.Events = await _context.Events
                .Where(e => e.EventDate >= DateTime.Now.Date)
                .ToListAsync();
            ViewBag.Venues = await _context.Venues.ToListAsync();

            return View(booking);
        }

        // POST: Booking/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Booking booking)
        {
            if (id != booking.BookingId)
            {
                return NotFound();
            }

            var userEmail = HttpContext.Session.GetString("UserEmail");

            if (string.IsNullOrEmpty(userEmail))
            {
                return RedirectToAction("Login", "Account");
            }

            // Ensure the booking belongs to current user
            var existingBooking = await _context.Bookings
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.BookingId == id);

            if (existingBooking == null || existingBooking.Email != userEmail)
            {
                return NotFound();
            }

            // Remove automatically populated properties from validation
            if (ModelState.ContainsKey("EventName"))
                ModelState.Remove("EventName");

            if (ModelState.ContainsKey("VenueName"))
                ModelState.Remove("VenueName");

            booking.Email = userEmail;

            // Fetch user ID
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
            if (user != null)
            {
                booking.UserId = user.UserId;
            }

            // Fetch Event
            var selectedEvent = await _context.Events.FirstOrDefaultAsync(e => e.EventId == booking.EventId);
            if (selectedEvent == null)
            {
                ModelState.AddModelError("EventId", "The selected event does not exist.");
            }
            else
            {
                booking.EventName = selectedEvent.EventName;
            }

            // Fetch Venue
            var selectedVenue = await _context.Venues.FirstOrDefaultAsync(v => v.VenueId == booking.VenueId);
            if (selectedVenue == null)
            {
                ModelState.AddModelError("VenueId", "The selected venue does not exist.");
            }
            else
            {
                booking.VenueName = selectedVenue.VenueName;
            }

            // Validate Booking Date
            if (booking.BookingDate.Date < DateTime.Now.Date)
            {
                ModelState.AddModelError("BookingDate", "Booking date cannot be in the past.");
            }

            if (selectedEvent != null && selectedEvent.EventDate.Date < booking.BookingDate.Date)
            {
                ModelState.AddModelError("BookingDate", "Booking date cannot be after the event date.");
            }

            // Check for double-booking, excluding the current booking
            var venueDoubleBooked = await _context.Bookings
                .AnyAsync(b => b.VenueId == booking.VenueId &&
                               b.BookingDate.Date == booking.BookingDate.Date &&
                               b.BookingId != booking.BookingId);

            if (venueDoubleBooked)
            {
                ModelState.AddModelError("VenueId", "This venue is already booked for the selected date.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Store the old venue ID in case it changed
                    var oldBooking = await _context.Bookings.AsNoTracking().FirstOrDefaultAsync(b => b.BookingId == id);
                    int? oldVenueId = oldBooking?.VenueId;

                    _context.Update(booking);
                    await _context.SaveChangesAsync();

                    // Auto-sync venue availability for both old and new venues
                    if (oldVenueId.HasValue && oldVenueId != booking.VenueId)
                    {
                        await SyncVenueAvailabilityForVenue(oldVenueId.Value);
                    }
                    await SyncVenueAvailabilityForVenue(booking.VenueId.Value);

                    TempData["SuccessMessage"] = "Booking updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BookingExists(booking.BookingId))
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
                    ModelState.AddModelError("", "An error occurred while updating the booking: " + ex.Message);
                }
            
        }

            // Reload dropdowns on error
            ViewBag.Events = await _context.Events.Where(e => e.EventDate >= DateTime.Now.Date).ToListAsync();
            ViewBag.Venues = await _context.Venues.ToListAsync();

            return View(booking);
        }

        private bool BookingExists(int id)
        {
            return _context.Bookings.Any(e => e.BookingId == id);
        }
        // GET: Booking/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");

            if (string.IsNullOrEmpty(userEmail))
            {
                return RedirectToAction("Login", "Account");
            }

            if (id == null)
            {
                return NotFound();
            }

            var booking = await _context.Bookings
                .Include(b => b.Event)
                .Include(b => b.Venue)
                .FirstOrDefaultAsync(m => m.BookingId == id && m.Email == userEmail);

            if (booking == null)
            {
                return NotFound();
            }

            return View(booking);
        }

        // POST: Booking/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");

            if (string.IsNullOrEmpty(userEmail))
            {
                return RedirectToAction("Login", "Account");
            }

            var booking = await _context.Bookings.FindAsync(id);

            if (booking == null || booking.Email != userEmail)
            {
                return NotFound();
            }

            // Store venue ID before deletion for auto-sync
            int? venueIdToSync = booking.VenueId;

            _context.Bookings.Remove(booking);
            await _context.SaveChangesAsync();

            // Auto-sync venue availability after deleting booking
            if (venueIdToSync.HasValue)
            {
                await SyncVenueAvailabilityForVenue(venueIdToSync.Value);
            }

            TempData["SuccessMessage"] = "Booking deleted successfully!";
            return RedirectToAction(nameof(Index));
        }
        [HttpGet]
        public async Task<IActionResult> Manage(string searchTerm, int? eventTypeId, DateTime? startDate, DateTime? endDate, int? venueId, string availability)
        {
            var query = _context.BookingDetails.AsQueryable();

            // Filter by search term
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                ViewBag.SearchTerm = searchTerm;

                if (int.TryParse(searchTerm, out int bookingId))
                {
                    query = query.Where(b => b.BookingId == bookingId || b.EventName.Contains(searchTerm));
                }
                else
                {
                    query = query.Where(b => b.EventName.Contains(searchTerm));
                }
            }

            // Filter by event type
            if (eventTypeId.HasValue && eventTypeId.Value > 0)
            {
                ViewBag.SelectedEventTypeId = eventTypeId.Value;

                query = from bd in query
                        join e in _context.Events on bd.EventId equals e.EventId
                        where e.EventTypeId == eventTypeId.Value
                        select bd;
            }

            // Filter by venue
            if (venueId.HasValue && venueId.Value > 0)
            {
                ViewBag.SelectedVenueId = venueId.Value;
                query = query.Where(b => b.VenueId == venueId.Value);
            }

            // Filter by date range
            if (startDate.HasValue)
            {
                ViewBag.StartDate = startDate.Value.ToString("yyyy-MM-dd");
                query = query.Where(b => b.BookingDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                ViewBag.EndDate = endDate.Value.ToString("yyyy-MM-dd");
                query = query.Where(b => b.BookingDate <= endDate.Value);
            }

            // Execute the query first to get the filtered bookings
            var bookings = await query.ToListAsync();

            // Filter by venue availability AFTER getting the bookings
            if (!string.IsNullOrWhiteSpace(availability))
            {
                ViewBag.SelectedAvailability = availability;

                if (availability == "Available")
                {
                    // Show bookings for venues that have the "Available" status in the database
                    var availableVenueIds = await _context.Venues
                        .Where(v => v.Availability == "Available")
                        .Select(v => v.VenueId)
                        .ToListAsync();

                    bookings = bookings.Where(b => availableVenueIds.Contains((int)b.VenueId)).ToList();
                }
                else if (availability == "Unavailable")
                {
                    // Show bookings for venues that have the "Unavailable" status in the database
                    var unavailableVenueIds = await _context.Venues
                        .Where(v => v.Availability == "Unavailable")
                        .Select(v => v.VenueId)
                        .ToListAsync();

                    bookings = bookings.Where(b => unavailableVenueIds.Contains((int)b.VenueId)).ToList();
                }
            }

            // Load dropdown data
            ViewBag.EventTypes = await _context.EventTypes
                .OrderBy(et => et.Name)
                .ToListAsync();

            ViewBag.Venues = await _context.Venues
                .OrderBy(v => v.VenueName)
                .ToListAsync();

            return View(bookings);
        }
        // Helper method to get available venues for a specific date
        private async Task<List<Venue>> GetAvailableVenuesForDate(DateTime date)
        {
            var bookedVenueIds = await _context.Bookings
                .Where(b => b.BookingDate.Date == date.Date)
                .Select(b => b.VenueId)
                .ToListAsync();

            return await _context.Venues
                .Where(v => !bookedVenueIds.Contains(v.VenueId))
                .OrderBy(v => v.VenueName)
                .ToListAsync();
        }

     
        // Method to sync venue availability based on current bookings
        public async Task<IActionResult> SyncVenueAvailability()
        {
            var venues = await _context.Venues.ToListAsync();
            int updatedCount = 0;

            foreach (var venue in venues)
            {
                // Check if venue has any future bookings (including today)
                var hasFutureBookings = await _context.Bookings
                    .AnyAsync(b => b.VenueId == venue.VenueId && b.BookingDate >= DateTime.Now.Date);

                string newAvailability = hasFutureBookings ? "Unavailable" : "Available";

                // Only update if the status has changed
                if (venue.Availability != newAvailability)
                {
                    venue.Availability = newAvailability;
                    updatedCount++;
                }
            }

            if (updatedCount > 0)
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Venue availability synchronized! {updatedCount} venue(s) updated.";
            }
            else
            {
                TempData["SuccessMessage"] = "Venue availability is already up to date.";
            }

            return RedirectToAction("Manage");
        }

        // Method to sync venue availability for a specific date
        public async Task<IActionResult> SyncVenueAvailabilityForDate(DateTime? date)
        {
            DateTime targetDate = date ?? DateTime.Now.Date;
            var venues = await _context.Venues.ToListAsync();
            int updatedCount = 0;

            foreach (var venue in venues)
            {
                // Check if venue has bookings for the specific date
                var hasBookingsOnDate = await _context.Bookings
                    .AnyAsync(b => b.VenueId == venue.VenueId && b.BookingDate.Date == targetDate.Date);

                string newAvailability = hasBookingsOnDate ? "Unavailable" : "Available";

                // Only update if the status has changed
                if (venue.Availability != newAvailability)
                {
                    venue.Availability = newAvailability;
                    updatedCount++;
                }
            }

            if (updatedCount > 0)
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Venue availability synchronized for {targetDate.ToString("MM/dd/yyyy")}! {updatedCount} venue(s) updated.";
            }
            else
            {
                TempData["SuccessMessage"] = $"Venue availability is already up to date for {targetDate.ToString("MM/dd/yyyy")}.";
            }

            return RedirectToAction("Manage");
        }

        // Helper method to automatically sync availability when bookings are created/updated/deleted
        private async Task SyncVenueAvailabilityForVenue(int venueId)
        {
            var venue = await _context.Venues.FindAsync(venueId);
            if (venue != null)
            {
                // Check if venue has any future bookings
                var hasFutureBookings = await _context.Bookings
                    .AnyAsync(b => b.VenueId == venueId && b.BookingDate >= DateTime.Now.Date);

                string newAvailability = hasFutureBookings ? "Unavailable" : "Available";

                if (venue.Availability != newAvailability)
                {
                    venue.Availability = newAvailability;
                    await _context.SaveChangesAsync();
                }
            }
        }

        // GET: Booking/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // If this is for booking specialists (staff), no need to filter by email
            var bookingDetails = await _context.BookingDetails
                .FirstOrDefaultAsync(m => m.BookingId == id);

            if (bookingDetails == null)
            {
                return NotFound();
            }

            return View(bookingDetails);
        }
        
    }
}
