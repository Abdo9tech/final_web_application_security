using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Project_DEPI_.Controllers
{
    // SECURITY [Default Deny Access Strategy]: [AllowAnonymous] at controller level makes
    // About page publicly accessible — required because the global FallbackPolicy in
    // Program.cs requires authentication for all endpoints by default.
    [AllowAnonymous]
    public class AboutController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
