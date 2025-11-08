using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DondeComemos.Data;
using DondeComemos.Models;

namespace DondeComemos.Controllers
{
    [Authorize]
    public class FavoritosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public FavoritosController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            
            var favoritos = await _context.Favoritos
                .Include(f => f.Restaurante)
                .Where(f => f.UserId == userId)
                .OrderByDescending(f => f.FechaAgregado)
                .Select(f => f.Restaurante!)
                .ToListAsync();

            return View(favoritos);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Agregar(int restauranteId)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null)
                return Unauthorized();

            var yaExiste = await _context.Favoritos
                .AnyAsync(f => f.UserId == userId && f.RestauranteId == restauranteId);

            if (yaExiste)
            {
                TempData["Info"] = "Este restaurante ya está en tus favoritos";
                return RedirectToAction("Details", "Restaurantes", new { id = restauranteId });
            }

            var favorito = new Favorito
            {
                UserId = userId,
                RestauranteId = restauranteId,
                FechaAgregado = DateTime.Now
            };

            _context.Favoritos.Add(favorito);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Restaurante agregado a favoritos";
            return RedirectToAction("Details", "Restaurantes", new { id = restauranteId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Eliminar(int restauranteId)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null)
                return Unauthorized();

            var favorito = await _context.Favoritos
                .FirstOrDefaultAsync(f => f.UserId == userId && f.RestauranteId == restauranteId);

            if (favorito != null)
            {
                _context.Favoritos.Remove(favorito);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Restaurante eliminado de favoritos";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> EsFavorito(int restauranteId)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null)
                return Json(new { esFavorito = false });

            var esFavorito = await _context.Favoritos
                .AnyAsync(f => f.UserId == userId && f.RestauranteId == restauranteId);

            return Json(new { esFavorito });
        }
    }
}