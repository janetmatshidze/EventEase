using EventEase.Data;
using EventEase.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Threading.Tasks;

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
                    _context.Update(booking);
                    await _context.SaveChangesAsync();
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

            _context.Bookings.Remove(booking);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Booking deleted successfully!";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult ManageAsSpecialist()

        {
            // This method is for the booking specialist without any login check
            return RedirectToAction("Manage"); // Redirect to the manage method with full access
        }


        [HttpGet]
        public async Task<IActionResult> Manage(string searchTerm)

        {
            var userEmail = HttpContext.Session.GetString("UserEmail");

            // Check for the booking specialist access

            if (userEmail == BookingSpecialistEmail)
            {
                // Continue to fetch bookings

                var query = _context.BookingDetails.AsQueryable();
                if (!string.IsNullOrEmpty(searchTerm))

                {

                    if (int.TryParse(searchTerm, out int bookingId))
                    {
                        query = query.Where(b => b.BookingId == bookingId || b.EventName.Contains(searchTerm));
                    }
                    else
                    {
                        query = query.Where(b => b.EventName.Contains(searchTerm));
                    }
                    ViewBag.SearchTerm = searchTerm;
                }
                var bookings = await query.ToListAsync();
                return View(bookings);
            }
            // If the user is not the booking specialist, show an unauthorized message or redirect
            return Unauthorized("You do not have access to manage bookings.");

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
