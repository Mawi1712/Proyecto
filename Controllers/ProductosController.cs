using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using DondeComemos.Data;
using DondeComemos.Models;
using DondeComemos.Services;

namespace DondeComemos.Controllers
{
    public class ProductosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IFileService _fileService;

        public ProductosController(ApplicationDbContext context, IFileService fileService)
        {
            _context = context;
            _fileService = fileService;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Menu(int restauranteId)
        {
            var restaurante = await _context.Restaurantes
                .Include(r => r.Productos)
                .FirstOrDefaultAsync(r => r.Id == restauranteId);

            if (restaurante == null)
                return NotFound();

            ViewBag.RestauranteNombre = restaurante.Nombre;
            return View(restaurante.Productos.Where(p => p.Disponible).ToList());
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index(int restauranteId)
        {
            var restaurante = await _context.Restaurantes.FindAsync(restauranteId);
            if (restaurante == null)
                return NotFound();

            var productos = await _context.Productos
                .Where(p => p.RestauranteId == restauranteId)
                .OrderBy(p => p.Orden)
                .ThenBy(p => p.Categoria)
                .ToListAsync();

            ViewBag.RestauranteId = restauranteId;
            ViewBag.RestauranteNombre = restaurante.Nombre;
            return View(productos);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Create(int restauranteId)
        {
            ViewBag.RestauranteId = restauranteId;
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Producto producto)
        {
            if (ModelState.IsValid)
            {
                if (producto.ImagenArchivo != null)
                {
                    try
                    {
                        producto.ImagenUrl = await _fileService.SaveImageAsync(
                            producto.ImagenArchivo, "productos");
                    }
                    catch (InvalidOperationException ex)
                    {
                        ModelState.AddModelError("ImagenArchivo", ex.Message);
                        ViewBag.RestauranteId = producto.RestauranteId;
                        return View(producto);
                    }
                }
                else if (!string.IsNullOrEmpty(producto.ImagenUrlExterna))
                {
                    producto.ImagenUrl = producto.ImagenUrlExterna;
                }

                _context.Add(producto);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Producto creado exitosamente";
                return RedirectToAction(nameof(Index), new { restauranteId = producto.RestauranteId });
            }

            ViewBag.RestauranteId = producto.RestauranteId;
            return View(producto);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var producto = await _context.Productos.FindAsync(id);
            if (producto == null)
                return NotFound();

            return View(producto);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Producto producto)
        {
            if (id != producto.Id)
                return NotFound();

            if (ModelState.IsValid)
            {
                var productoExistente = await _context.Productos.AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (producto.ImagenArchivo != null)
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(productoExistente?.ImagenUrl) && 
                            !productoExistente.ImagenUrl.StartsWith("http"))
                        {
                            _fileService.DeleteImage(productoExistente.ImagenUrl);
                        }

                        producto.ImagenUrl = await _fileService.SaveImageAsync(
                            producto.ImagenArchivo, "productos");
                    }
                    catch (InvalidOperationException ex)
                    {
                        ModelState.AddModelError("ImagenArchivo", ex.Message);
                        return View(producto);
                    }
                }
                else if (!string.IsNullOrEmpty(producto.ImagenUrlExterna))
                {
                    if (!string.IsNullOrEmpty(productoExistente?.ImagenUrl) && 
                        !productoExistente.ImagenUrl.StartsWith("http"))
                    {
                        _fileService.DeleteImage(productoExistente.ImagenUrl);
                    }
                    producto.ImagenUrl = producto.ImagenUrlExterna;
                }
                else
                {
                    producto.ImagenUrl = productoExistente?.ImagenUrl ?? string.Empty;
                }

                _context.Update(producto);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Producto actualizado exitosamente";
                return RedirectToAction(nameof(Index), new { restauranteId = producto.RestauranteId });
            }

            return View(producto);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var producto = await _context.Productos
                .Include(p => p.Restaurante)
                .FirstOrDefaultAsync(p => p.Id == id);
            
            if (producto == null)
                return NotFound();

            return View(producto);
        }

        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var producto = await _context.Productos.FindAsync(id);
            if (producto != null)
            {
                if (!string.IsNullOrEmpty(producto.ImagenUrl) && 
                    !producto.ImagenUrl.StartsWith("http"))
                {
                    _fileService.DeleteImage(producto.ImagenUrl);
                }

                var restauranteId = producto.RestauranteId;
                _context.Productos.Remove(producto);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Producto eliminado exitosamente";
                return RedirectToAction(nameof(Index), new { restauranteId });
            }

            return NotFound();
        }
    }
}