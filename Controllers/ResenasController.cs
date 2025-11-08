using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DondeComemos.Data;
using DondeComemos.Models;
using DondeComemos.Services;

namespace DondeComemos.Controllers
{
    [Authorize]
    public class ResenasController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IEmailService _emailService;

        public ResenasController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            IEmailService emailService)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Resena resena)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null)
                return Unauthorized();

            var yaReseno = await _context.Resenas
                .AnyAsync(r => r.UserId == userId && r.RestauranteId == resena.RestauranteId);

            if (yaReseno)
            {
                TempData["Error"] = "Ya has publicado una reseña para este restaurante";
                return RedirectToAction("Details", "Restaurantes", new { id = resena.RestauranteId });
            }

            if (ModelState.IsValid)
            {
                resena.UserId = userId;
                resena.FechaCreacion = DateTime.Now;
                resena.Aprobado = true;

                _context.Resenas.Add(resena);
                await _context.SaveChangesAsync();

                await ActualizarRatingRestaurante(resena.RestauranteId);

                var notificacion = new Notificacion
                {
                    UserId = userId,
                    Titulo = "Reseña Publicada",
                    Mensaje = "Tu reseña ha sido publicada exitosamente",
                    Tipo = "Success",
                    Url = $"/Restaurantes/Details/{resena.RestauranteId}"
                };
                _context.Notificaciones.Add(notificacion);
                await _context.SaveChangesAsync();

                try
                {
                    var user = await _userManager.GetUserAsync(User);
                    var restaurante = await _context.Restaurantes.FindAsync(resena.RestauranteId);
                    if (user?.Email != null && restaurante != null)
                    {
                        await _emailService.SendReviewNotificationAsync(
                            user.Email,
                            user.UserName ?? "Usuario",
                            restaurante.Nombre
                        );
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error enviando email: {ex.Message}");
                }

                TempData["Success"] = "¡Gracias por tu reseña!";
                return RedirectToAction("Details", "Restaurantes", new { id = resena.RestauranteId });
            }

            return RedirectToAction("Details", "Restaurantes", new { id = resena.RestauranteId });
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var resena = await _context.Resenas.FindAsync(id);
            if (resena == null)
                return NotFound();

            var restauranteId = resena.RestauranteId;

            _context.Resenas.Remove(resena);
            await _context.SaveChangesAsync();

            await ActualizarRatingRestaurante(restauranteId);

            TempData["Success"] = "Reseña eliminada exitosamente";
            return RedirectToAction("Details", "Restaurantes", new { id = restauranteId });
        }

        private async Task ActualizarRatingRestaurante(int restauranteId)
        {
            var restaurante = await _context.Restaurantes
                .Include(r => r.Resenas)
                .FirstOrDefaultAsync(r => r.Id == restauranteId);

            if (restaurante != null && restaurante.Resenas.Any())
            {
                restaurante.Rating = restaurante.Resenas.Average(r => r.Calificacion);
                await _context.SaveChangesAsync();
            }
        }
    }
}