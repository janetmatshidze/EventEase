using EventEase.Data;
using EventEase.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace EventEase.Controllers
{
    public class AccountController : Controller
    {
        private readonly EventEaseDBContext _context;
        private const string BookingSpecialistEmail = "specialist@eventease.com";
        private const string BookingSpecialistPassword = "Specialist123!";
        public AccountController(EventEaseDBContext context)
        {
            _context = context;
        }

        // Register GET
        public IActionResult Register()
        {
            return View();
        }

        // Register POST
        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var existingUser = await _context.Users.AnyAsync(u => u.Email == model.Email);
                if (existingUser)
                {
                    ModelState.AddModelError("", "Email is already in use.");
                    return View(model);
                }

                var user = new User
                {
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Email = model.Email,
                    PhoneNumber = model.PhoneNumber,
                    PasswordHash = HashPassword(model.Password)
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Store user email in session
                HttpContext.Session.SetString("UserEmail", user.Email);
                return RedirectToAction("Index", "Home");
            }

            return View(model);
        }

        // Login GET
        public IActionResult Login()
        {
            return View();
        }

        // Login POST
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Check for the booking specialist login without hashing
                if (model.Email == BookingSpecialistEmail && model.Password == BookingSpecialistPassword)
                {
                    // Store specialist email in session
                    HttpContext.Session.SetString("UserEmail", BookingSpecialistEmail);
                    return RedirectToAction("Manage", "Booking"); // Redirect to Manage view for booking specialist
                }

                // Regular user login process
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);

                if (user == null || user.PasswordHash != HashPassword(model.Password))
                {
                    ModelState.AddModelError("", "Invalid login attempt.");
                    return View(model);
                }

                // Store user email in session
                HttpContext.Session.SetString("UserEmail", user.Email);
                return RedirectToAction("Index", "Booking");
            }

            return View(model);
        }


        // Logout
        public IActionResult Logout()
        {
            // Clear the session
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        // Password hashing method
        private string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}