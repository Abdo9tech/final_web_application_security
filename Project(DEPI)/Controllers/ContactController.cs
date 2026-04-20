using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using LuxuryHaven.Models;
using System.Threading.Tasks;
using BookifyHotel.Data;

namespace LuxuryHaven.Controllers
{
    // SECURITY [Default Deny Access Strategy]: Contact page is intentionally public.
    // [AllowAnonymous] overrides the global FallbackPolicy that requires authentication.
    [AllowAnonymous]
    public class ContactController : Controller
    {
        private readonly BookifyHotelDbContext _context;

        public ContactController(BookifyHotelDbContext context)
        {
            _context = context;
        }

        // GET: Contact
        public IActionResult Index()
        {
            return View();
        }

        // SECURITY [CSRF Protection]: [ValidateAntiForgeryToken] ensures only legitimate
        // form submissions from our own site are processed — prevents cross-site form submission.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SendMessage([FromForm] Contact msg)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    // Add current date
                    msg.CreatedAt = DateTime.Now;
                    msg.IsRead = false;

                    // Save to database
                    _context.Contacts.Add(msg);
                    _context.SaveChanges();

                    // Redirect with success message
                    TempData["SuccessMessage"] = "Thank you! Your message has been sent successfully.";
                    return RedirectToAction("Index", "Contact");
                }
                else
                {
                    // Return to form with errors
                    foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                    {
                        TempData["ErrorMessage"] = error.ErrorMessage;
                    }
                    return View("Index", msg);
                }
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "An error occurred. Please try again later.";
                return RedirectToAction("Index", "Contact");
            }
        }

        // SECURITY [CSRF Protection]: Ajax endpoint also requires anti-forgery token.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SendMessageAjax([FromBody] Contact msg)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    // Add current date
                    msg.CreatedAt = DateTime.Now;
                    msg.IsRead = false;

                    // Save to database
                    _context.Contacts.Add(msg);
                    _context.SaveChanges();

                    return Json(new
                    {
                        success = true,
                        message = "Thank you! Your message has been sent successfully."
                    });
                }
                else
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    return Json(new
                    {
                        success = false,
                        message = "Please correct the errors.",
                        errors = errors
                    });
                }
            }
            catch (Exception)
            {
                return Json(new
                {
                    success = false,
                    message = "An error occurred. Please try again."
                });
            }
        }
    }
}