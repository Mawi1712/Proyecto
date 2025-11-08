using System.ComponentModel;
using Microsoft.SemanticKernel;
using Microsoft.EntityFrameworkCore;
using DondeComemos.Data;
using System.Text.Json;

namespace DondeComemos.Plugins
{
    public class MenuPlugin
    {
        private readonly ApplicationDbContext _context;

        public MenuPlugin(ApplicationDbContext context)
        {
            _context = context;
        }

        [KernelFunction, Description("Busca platos por nombre o ingredientes")]
        public async Task<string> BuscarPlatos(
            [Description("Nombre del plato o ingrediente a buscar")] string busqueda)
        {
            var productos = await _context.Productos
                .Include(p => p.Restaurante)
                .Where(p => p.Disponible && 
                    (p.Nombre.ToLower().Contains(busqueda.ToLower()) ||
                     (p.Descripcion != null && p.Descripcion.ToLower().Contains(busqueda.ToLower())) ||
                     (p.Ingredientes != null && p.Ingredientes.ToLower().Contains(busqueda.ToLower()))))
                .Take(10)
                .Select(p => new
                {
                    p.Nombre,
                    p.Descripcion,
                    p.Precio,
                    p.Categoria,
                    Restaurante = p.Restaurante!.Nombre,
                    p.RecomendacionChef,
                    p.EsVegetariano,
                    p.EsVegano,
                    p.Picante
                })
                .ToListAsync();

            if (!productos.Any())
                return $"No se encontraron platos que coincidan con '{busqueda}'";

            return JsonSerializer.Serialize(productos, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
        }

        [KernelFunction, Description("Obtiene el menú completo de un restaurante")]
        public async Task<string> ObtenerMenu(
            [Description("Nombre del restaurante")] string nombreRestaurante)
        {
            var restaurante = await _context.Restaurantes
                .Include(r => r.Productos.Where(p => p.Disponible))
                .FirstOrDefaultAsync(r => r.Nombre.ToLower().Contains(nombreRestaurante.ToLower()));

            if (restaurante == null)
                return $"No se encontró el restaurante '{nombreRestaurante}'";

            var menu = restaurante.Productos
                .GroupBy(p => p.Categoria)
                .Select(g => new
                {
                    Categoria = g.Key,
                    Platos = g.Select(p => new
                    {
                        p.Nombre,
                        p.Descripcion,
                        p.Precio,
                        p.RecomendacionChef,
                        p.EsVegetariano,
                        p.EsVegano,
                        p.Picante
                    }).ToList()
                })
                .ToList();

            var resultado = new
            {
                Restaurante = restaurante.Nombre,
                TotalPlatos = restaurante.Productos.Count,
                Menu = menu
            };

            return JsonSerializer.Serialize(resultado, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
        }

        [KernelFunction, Description("Busca platos vegetarianos o veganos")]
        public async Task<string> BuscarPlatosVegetarianos(
            [Description("Tipo: vegetariano o vegano")] string tipo)
        {
            var esVegano = tipo.ToLower().Contains("vegan");
            
            var productos = await _context.Productos
                .Include(p => p.Restaurante)
                .Where(p => p.Disponible && (esVegano ? p.EsVegano : p.EsVegetariano))
                .Take(10)
                .Select(p => new
                {
                    p.Nombre,
                    p.Descripcion,
                    p.Precio,
                    Restaurante = p.Restaurante!.Nombre,
                    p.EsVegano,
                    p.EsVegetariano
                })
                .ToListAsync();

            if (!productos.Any())
                return $"No se encontraron platos {tipo}";

            return JsonSerializer.Serialize(productos, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
        }

        [KernelFunction, Description("Obtiene recomendaciones del chef")]
        public async Task<string> ObtenerRecomendacionesChef(
            [Description("Nombre del restaurante (opcional)")] string? nombreRestaurante = null)
        {
            var query = _context.Productos
                .Include(p => p.Restaurante)
                .Where(p => p.Disponible && p.RecomendacionChef);

            if (!string.IsNullOrEmpty(nombreRestaurante))
            {
                query = query.Where(p => p.Restaurante!.Nombre.ToLower().Contains(nombreRestaurante.ToLower()));
            }

            var productos = await query
                .Take(10)
                .Select(p => new
                {
                    p.Nombre,
                    p.Descripcion,
                    p.Precio,
                    Restaurante = p.Restaurante!.Nombre,
                    p.Categoria
                })
                .ToListAsync();

            if (!productos.Any())
                return "No se encontraron recomendaciones del chef";

            return JsonSerializer.Serialize(productos, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
        }
    }
}