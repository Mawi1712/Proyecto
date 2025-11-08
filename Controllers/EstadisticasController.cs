using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DondeComemos.Data;

namespace DondeComemos.Controllers
{
    [Authorize(Roles = "Admin")]
    public class EstadisticasController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EstadisticasController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var totalRestaurantes = await _context.Restaurantes.CountAsync();
                var totalProductos = await _context.Productos.CountAsync();
                var totalResenas = await _context.Resenas.CountAsync();
                var totalUsuarios = await _context.Users.CountAsync();
                var totalContactos = await _context.Contactos.CountAsync();
                var totalSugerencias = await _context.Sugerencias.CountAsync();
                
                var ratingPromedio = await _context.Restaurantes.AnyAsync() 
                    ? await _context.Restaurantes.AverageAsync(r => r.Rating) 
                    : 0;
                
                var restaurantesMejorCalificados = await _context.Restaurantes
                    .OrderByDescending(r => r.Rating)
                    .Take(5)
                    .Select(r => new RestauranteStats
                    {
                        Nombre = r.Nombre,
                        Rating = r.Rating,
                        TotalResenas = r.Resenas.Count,
                        ImagenUrl = r.ImagenUrl
                    })
                    .ToListAsync();
                
                var restaurantesMasResenados = await _context.Restaurantes
                    .OrderByDescending(r => r.Resenas.Count)
                    .Take(5)
                    .Select(r => new RestauranteStats
                    {
                        Nombre = r.Nombre,
                        Rating = r.Rating,
                        TotalResenas = r.Resenas.Count,
                        ImagenUrl = r.ImagenUrl
                    })
                    .ToListAsync();
                
                var tiposCocinaPopulares = await _context.Restaurantes
                    .GroupBy(r => r.TipoCocina)
                    .Select(g => new TipoCocinaStats
                    {
                        Tipo = g.Key,
                        Cantidad = g.Count()
                    })
                    .OrderByDescending(t => t.Cantidad)
                    .Take(6)
                    .ToListAsync();
                
                var rangoPreciosDistribucion = await _context.Restaurantes
                    .GroupBy(r => r.RangoPrecios)
                    .Select(g => new RangoPrecioStats
                    {
                        Rango = g.Key,
                        Cantidad = g.Count()
                    })
                    .ToListAsync();
                
                // Cargar todos los productos y ordenar en memoria (solución para SQLite)
                var todosProductos = await _context.Productos
                    .Include(p => p.Restaurante)
                    .ToListAsync();
                
                var productosMasCaros = todosProductos
                    .OrderByDescending(p => p.Precio)
                    .Take(5)
                    .Select(p => new ProductoStats
                    {
                        Nombre = p.Nombre,
                        Precio = p.Precio,
                        Restaurante = p.Restaurante?.Nombre ?? "Sin restaurante",
                        ImagenUrl = p.ImagenUrl
                    })
                    .ToList();
                
                // Reseñas por mes - cargar primero y formatear en memoria
                var resenasRaw = await _context.Resenas
                    .Where(r => r.FechaCreacion >= DateTime.Now.AddMonths(-6))
                    .Select(r => new { r.FechaCreacion.Year, r.FechaCreacion.Month })
                    .ToListAsync();
                
                var resenasPorMes = resenasRaw
                    .GroupBy(r => new { r.Year, r.Month })
                    .Select(g => new ResenasMesStats
                    {
                        Mes = $"{g.Key.Month}/{g.Key.Year}",
                        Cantidad = g.Count()
                    })
                    .OrderBy(r => r.Mes)
                    .ToList();
                
                var ultimasResenas = await _context.Resenas
                    .Include(r => r.Restaurante)
                    .OrderByDescending(r => r.FechaCreacion)
                    .Take(5)
                    .Select(r => new ResenaReciente
                    {
                        Titulo = r.Titulo,
                        Comentario = r.Comentario,
                        Calificacion = r.Calificacion,
                        RestauranteNombre = r.Restaurante != null ? r.Restaurante.Nombre : "Sin restaurante",
                        Fecha = r.FechaCreacion
                    })
                    .ToListAsync();
                
                var contactosPorSatisfaccion = await _context.Contactos
                    .GroupBy(c => c.Satisfaccion)
                    .Select(g => new SatisfaccionStats
                    {
                        Nivel = g.Key ?? "0",
                        Cantidad = g.Count()
                    })
                    .OrderBy(s => s.Nivel)
                    .ToListAsync();

                var stats = new EstadisticasViewModel
                {
                    TotalRestaurantes = totalRestaurantes,
                    TotalProductos = totalProductos,
                    TotalResenas = totalResenas,
                    TotalUsuarios = totalUsuarios,
                    TotalContactos = totalContactos,
                    TotalSugerencias = totalSugerencias,
                    RatingPromedio = ratingPromedio,
                    RestaurantesMejorCalificados = restaurantesMejorCalificados,
                    RestaurantesMasResenados = restaurantesMasResenados,
                    TiposCocinaPopulares = tiposCocinaPopulares,
                    RangoPreciosDistribucion = rangoPreciosDistribucion,
                    ProductosMasCaros = productosMasCaros,
                    ResenasPorMes = resenasPorMes,
                    UltimasResenas = ultimasResenas,
                    ContactosPorSatisfaccion = contactosPorSatisfaccion
                };

                return View(stats);
            }
            catch (Exception ex)
            {
                // Log del error
                Console.WriteLine($"Error en Estadísticas: {ex.Message}");
                
                // Retornar vista con datos vacíos
                var statsVacio = new EstadisticasViewModel();
                TempData["Error"] = "Error al cargar estadísticas: " + ex.Message;
                return View(statsVacio);
            }
        }

        public async Task<IActionResult> GetResenasChart()
        {
            var data = await _context.Resenas
                .Where(r => r.FechaCreacion >= DateTime.Now.AddMonths(-6))
                .GroupBy(r => new { r.FechaCreacion.Year, r.FechaCreacion.Month })
                .Select(g => new 
                {
                    mes = $"{g.Key.Month}/{g.Key.Year}",
                    cantidad = g.Count()
                })
                .OrderBy(r => r.mes)
                .ToListAsync();

            return Json(data);
        }
    }

    // ViewModels
    public class EstadisticasViewModel
    {
        public int TotalRestaurantes { get; set; }
        public int TotalProductos { get; set; }
        public int TotalResenas { get; set; }
        public int TotalUsuarios { get; set; }
        public int TotalContactos { get; set; }
        public int TotalSugerencias { get; set; }
        public double RatingPromedio { get; set; }
        public List<RestauranteStats> RestaurantesMejorCalificados { get; set; } = new();
        public List<RestauranteStats> RestaurantesMasResenados { get; set; } = new();
        public List<TipoCocinaStats> TiposCocinaPopulares { get; set; } = new();
        public List<RangoPrecioStats> RangoPreciosDistribucion { get; set; } = new();
        public List<ProductoStats> ProductosMasCaros { get; set; } = new();
        public List<ResenasMesStats> ResenasPorMes { get; set; } = new();
        public List<ResenaReciente> UltimasResenas { get; set; } = new();
        public List<SatisfaccionStats> ContactosPorSatisfaccion { get; set; } = new();
    }

    public class RestauranteStats
    {
        public string Nombre { get; set; } = string.Empty;
        public double Rating { get; set; }
        public int TotalResenas { get; set; }
        public string ImagenUrl { get; set; } = string.Empty;
    }

    public class TipoCocinaStats
    {
        public string Tipo { get; set; } = string.Empty;
        public int Cantidad { get; set; }
    }

    public class RangoPrecioStats
    {
        public string Rango { get; set; } = string.Empty;
        public int Cantidad { get; set; }
    }

    public class ProductoStats
    {
        public string Nombre { get; set; } = string.Empty;
        public decimal Precio { get; set; }
        public string Restaurante { get; set; } = string.Empty;
        public string? ImagenUrl { get; set; }
    }

    public class ResenasMesStats
    {
        public string Mes { get; set; } = string.Empty;
        public int Cantidad { get; set; }
    }

    public class ResenaReciente
    {
        public string Titulo { get; set; } = string.Empty;
        public string Comentario { get; set; } = string.Empty;
        public double Calificacion { get; set; }
        public string RestauranteNombre { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
    }

    public class SatisfaccionStats
    {
        public string Nivel { get; set; } = string.Empty;
        public int Cantidad { get; set; }
    }
}