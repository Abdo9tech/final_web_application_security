using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Project_DEPI_.Controllers
{
    // SECURITY [Default Deny Access Strategy]: The base/landing page is intentionally public.
    // [AllowAnonymous] overrides the global FallbackPolicy that requires authentication,
    // making this controller accessible to unauthenticated visitors.
    [AllowAnonymous]
    public class BasePageController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
