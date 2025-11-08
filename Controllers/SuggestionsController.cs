using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DondeComemos.Data;
using DondeComemos.Models;

namespace DondeComemos.Controllers
{
    public class SuggestionsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SuggestionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var sugerenciasAprobadas = await _context.Sugerencias
                .Where(s => s.Aprobado)
                .OrderByDescending(s => s.FechaCreacion)
                .Take(20)
                .ToListAsync();

            return View(sugerenciasAprobadas);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Sugerencia sugerencia)
        {
            if (ModelState.IsValid)
            {
                sugerencia.FechaCreacion = DateTime.Now;
                sugerencia.Aprobado = false;
                
                _context.Sugerencias.Add(sugerencia);
                await _context.SaveChangesAsync();

                TempData["Success"] = "¡Gracias por tu sugerencia! Será revisada pronto.";
                return RedirectToAction(nameof(Index));
            }

            return View("Index", await _context.Sugerencias
                .Where(s => s.Aprobado)
                .OrderByDescending(s => s.FechaCreacion)
                .ToListAsync());
        }
    }
}