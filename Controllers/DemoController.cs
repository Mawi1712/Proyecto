using Microsoft.AspNetCore.Mvc;

namespace DondeComemos.Controllers
{
    public class DemoController : Controller
    {
        public IActionResult SemanticKernel()
        {
            return View();
        }
    }
}