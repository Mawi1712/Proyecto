using Microsoft.AspNetCore.Mvc;
using DondeComemos.Models;
using DondeComemos.Data;

namespace DondeComemos.Controllers
{
    public class ContactController : Controller
    {
        private readonly ILogger<ContactController> _logger;
        private readonly ApplicationDbContext _context;
        
        public ContactController(ILogger<ContactController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Registrar(Contacto obj)
        {
            _logger.LogInformation("Registro de contacto recibido. {@Contacto}", obj);
            if (ModelState.IsValid)
            {
                _context.Contactos.Add(obj);
                _context.SaveChanges();
                ViewData["Mensaje"] = "Gracias por contactarnos. Pronto nos comunicaremos contigo.";
            }
            return View("Index");
        }
    }
}