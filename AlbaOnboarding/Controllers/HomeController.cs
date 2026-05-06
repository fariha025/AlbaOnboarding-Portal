using System.Diagnostics;
using AlbaOnboarding.Models;
using Microsoft.AspNetCore.Mvc;

namespace AlbaOnboarding.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            if (User.IsInRole("Admin"))
                return RedirectToAction("Index", "Admin");
            if (User.IsInRole("HR"))
                return RedirectToAction("Dashboard", "HR");
            if (User.IsInRole("Employee"))
                return RedirectToAction("Dashboard", "Employee");

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        [Route("Home/Error/{code}")]
        public IActionResult Error(int code)
        {
            if (code == 403) return View("Error403");
            return View("Error");
        }
    }
}
