using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using DondeComemos.Data;
using DondeComemos.Models;
using DondeComemos.Services;
using System.Text;

namespace DondeComemos.Controllers
{
    public class RestaurantesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IFileService _fileService;
        private readonly IPdfService _pdfService;

        public RestaurantesController(
            ApplicationDbContext context, 
            IFileService fileService,
            IPdfService pdfService)
        {
            _context = context;
            _fileService = fileService;
            _pdfService = pdfService;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            var restaurantes = await _context.Restaurantes
                .OrderByDescending(r => r.Rating)
                .ToListAsync();

            return View(restaurantes);
        }

        [AllowAnonymous]
        public async Task<IActionResult> Search(string? q, string? tipo, string? precio, double? rating)
        {
            var query = _context.Restaurantes.AsQueryable();

            // Buscar por texto general
            if (!string.IsNullOrEmpty(q))
            {
                query = query.Where(r => r.Nombre.Contains(q) ||
                                         r.Direccion.Contains(q) ||
                                         r.Descripcion.Contains(q) ||
                                         r.TipoCocina.Contains(q));
            }

            // Filtrar por tipo de cocina
            if (!string.IsNullOrEmpty(tipo))
            {
                query = query.Where(r => r.TipoCocina == tipo);
            }

            // Filtrar por rango de precios
            if (!string.IsNullOrEmpty(precio))
            {
                query = query.Where(r => r.RangoPrecios == precio);
            }

            // Filtrar por rating mínimo
            if (rating.HasValue)
            {
                query = query.Where(r => r.Rating >= rating.Value);
            }

            var restaurantes = await query.OrderByDescending(r => r.Rating).ToListAsync();

            // Pasar los parámetros a ViewBag para mantener los filtros
            ViewBag.SearchQuery = q;
            ViewBag.TipoFiltro = tipo;
            ViewBag.PrecioFiltro = precio;
            ViewBag.RatingFiltro = rating;

            return View(restaurantes);
        }

        [AllowAnonymous]
        public async Task<IActionResult> Details(int id)
        {
            var restaurante = await _context.Restaurantes
                .Include(r => r.Productos.Where(p => p.Disponible))
                .Include(r => r.Resenas)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (restaurante == null)
                return NotFound();

            return View(restaurante);
        }

        [AllowAnonymous]
        public async Task<IActionResult> ExportarMenuPdf(int id)
        {
            var restaurante = await _context.Restaurantes
                .Include(r => r.Productos.Where(p => p.Disponible))
                .FirstOrDefaultAsync(r => r.Id == id);

            if (restaurante == null)
                return NotFound();

            var html = _pdfService.GenerarMenuHtml(restaurante, restaurante.Productos.ToList());
            
            return Content(html, "text/html", Encoding.UTF8);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Restaurante restaurante)
        {
            if (ModelState.IsValid)
            {
                if (restaurante.ImagenArchivo != null)
                {
                    try
                    {
                        restaurante.ImagenUrl = await _fileService.SaveImageAsync(
                            restaurante.ImagenArchivo, "restaurantes");
                    }
                    catch (InvalidOperationException ex)
                    {
                        ModelState.AddModelError("ImagenArchivo", ex.Message);
                        return View(restaurante);
                    }
                }
                else
                {
                    restaurante.ImagenUrl = "https://images.unsplash.com/photo-1517248135467-4c7edcad34c4?w=600";
                }

                _context.Add(restaurante);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Restaurante creado exitosamente";
                return RedirectToAction(nameof(Index));
            }
            return View(restaurante);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var restaurante = await _context.Restaurantes.FindAsync(id);
            if (restaurante == null)
            {
                return NotFound();
            }
            return View(restaurante);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Restaurante restaurante)
        {
            if (id != restaurante.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var restauranteExistente = await _context.Restaurantes.AsNoTracking()
                        .FirstOrDefaultAsync(r => r.Id == id);

                    if (restaurante.ImagenArchivo != null)
                    {
                        try
                        {
                            if (!string.IsNullOrEmpty(restauranteExistente?.ImagenUrl) &&
                                !restauranteExistente.ImagenUrl.StartsWith("http"))
                            {
                                _fileService.DeleteImage(restauranteExistente.ImagenUrl);
                            }

                            restaurante.ImagenUrl = await _fileService.SaveImageAsync(
                                restaurante.ImagenArchivo, "restaurantes");
                        }
                        catch (InvalidOperationException ex)
                        {
                            ModelState.AddModelError("ImagenArchivo", ex.Message);
                            return View(restaurante);
                        }
                    }
                    else
                    {
                        restaurante.ImagenUrl = restauranteExistente?.ImagenUrl ?? string.Empty;
                    }

                    _context.Update(restaurante);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Restaurante actualizado exitosamente";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RestauranteExists(restaurante.Id))
                        return NotFound();
                    else
                        throw;
                }
            }
            return View(restaurante);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var restaurante = await _context.Restaurantes.FindAsync(id);
            if (restaurante == null)
            {
                return NotFound();
            }
            return View(restaurante);
        }

        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var restaurante = await _context.Restaurantes
                    .Include(r => r.Productos)
                    .Include(r => r.Resenas)
                    .FirstOrDefaultAsync(r => r.Id == id);
                
                if (restaurante != null)
                {
                    foreach (var producto in restaurante.Productos)
                    {
                        if (!string.IsNullOrEmpty(producto.ImagenUrl) &&
                            !producto.ImagenUrl.StartsWith("http"))
                        {
                            _fileService.DeleteImage(producto.ImagenUrl);
                        }
                    }

                    if (!string.IsNullOrEmpty(restaurante.ImagenUrl) &&
                        !restaurante.ImagenUrl.StartsWith("http"))
                    {
                        _fileService.DeleteImage(restaurante.ImagenUrl);
                    }

                    _context.Restaurantes.Remove(restaurante);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Restaurante eliminado exitosamente";
                }
                
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al eliminar: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        private bool RestauranteExists(int id)
        {
            return _context.Restaurantes.Any(e => e.Id == id);
        }
    }
}