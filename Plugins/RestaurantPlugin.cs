using System.ComponentModel;
using Microsoft.SemanticKernel;
using Microsoft.EntityFrameworkCore;
using DondeComemos.Data;
using System.Text.Json;

namespace DondeComemos.Plugins
{
    public class RestaurantPlugin
    {
        private readonly ApplicationDbContext _context;

        public RestaurantPlugin(ApplicationDbContext context)
        {
            _context = context;
        }

        [KernelFunction, Description("Busca restaurantes por tipo de cocina")]
        public async Task<string> BuscarPorTipoCocina(
            [Description("Tipo de cocina a buscar, ej: Peruana, Italiana, China")] string tipoCocina)
        {
            var restaurantes = await _context.Restaurantes
                .Where(r => r.TipoCocina.ToLower().Contains(tipoCocina.ToLower()))
                .Take(5)
                .Select(r => new
                {
                    r.Nombre,
                    r.Direccion,
                    r.TipoCocina,
                    r.RangoPrecios,
                    r.Rating,
                    r.Telefono
                })
                .ToListAsync();

            if (!restaurantes.Any())
                return $"No se encontraron restaurantes de cocina {tipoCocina}";

            return JsonSerializer.Serialize(restaurantes, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
        }

        [KernelFunction, Description("Busca restaurantes por rango de precios")]
        public async Task<string> BuscarPorPrecio(
            [Description("Rango de precios: Económico, Medio, Alto, Premium")] string rangoPrecio)
        {
            var restaurantes = await _context.Restaurantes
                .Where(r => r.RangoPrecios.ToLower() == rangoPrecio.ToLower())
                .Take(5)
                .Select(r => new
                {
                    r.Nombre,
                    r.Direccion,
                    r.TipoCocina,
                    r.RangoPrecios,
                    r.Rating
                })
                .ToListAsync();

            if (!restaurantes.Any())
                return $"No se encontraron restaurantes en rango {rangoPrecio}";

            return JsonSerializer.Serialize(restaurantes, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
        }

        [KernelFunction, Description("Obtiene información detallada de un restaurante específico")]
        public async Task<string> ObtenerDetallesRestaurante(
            [Description("Nombre del restaurante")] string nombreRestaurante)
        {
            var restaurante = await _context.Restaurantes
                .Include(r => r.Productos.Where(p => p.Disponible))
                .Include(r => r.Resenas)
                .FirstOrDefaultAsync(r => r.Nombre.ToLower().Contains(nombreRestaurante.ToLower()));

            if (restaurante == null)
                return $"No se encontró el restaurante '{nombreRestaurante}'";

            var detalles = new
            {
                restaurante.Nombre,
                restaurante.Descripcion,
                restaurante.Direccion,
                restaurante.Telefono,
                restaurante.Horario,
                restaurante.TipoCocina,
                restaurante.RangoPrecios,
                restaurante.Rating,
                TotalResenas = restaurante.Resenas.Count,
                Servicios = new
                {
                    restaurante.DeliveryDisponible,
                    restaurante.AceptaReservas,
                    restaurante.WifiGratis,
                    restaurante.TieneEstacionamiento
                },
                ProductosDestacados = restaurante.Productos
                    .Where(p => p.RecomendacionChef)
                    .Take(3)
                    .Select(p => new { p.Nombre, p.Precio, p.Descripcion })
            };

            return JsonSerializer.Serialize(detalles, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
        }

        [KernelFunction, Description("Busca restaurantes con servicios específicos")]
        public async Task<string> BuscarPorServicios(
            [Description("Servicios deseados: delivery, reservas, wifi, estacionamiento, vegetariano")] 
            string servicios)
        {
            var query = _context.Restaurantes.AsQueryable();

            if (servicios.ToLower().Contains("delivery"))
                query = query.Where(r => r.DeliveryDisponible);
            
            if (servicios.ToLower().Contains("reservas"))
                query = query.Where(r => r.AceptaReservas);
            
            if (servicios.ToLower().Contains("wifi"))
                query = query.Where(r => r.WifiGratis);
            
            if (servicios.ToLower().Contains("estacionamiento"))
                query = query.Where(r => r.TieneEstacionamiento);
            
            if (servicios.ToLower().Contains("vegetariano"))
                query = query.Where(r => r.OpcionesVegetarianas);

            var restaurantes = await query
                .Take(5)
                .Select(r => new
                {
                    r.Nombre,
                    r.Direccion,
                    r.TipoCocina,
                    r.Rating,
                    Servicios = new
                    {
                        r.DeliveryDisponible,
                        r.AceptaReservas,
                        r.WifiGratis,
                        r.TieneEstacionamiento,
                        r.OpcionesVegetarianas
                    }
                })
                .ToListAsync();

            if (!restaurantes.Any())
                return "No se encontraron restaurantes con esos servicios";

            return JsonSerializer.Serialize(restaurantes, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
        }

        [KernelFunction, Description("Obtiene los restaurantes mejor calificados")]
        public async Task<string> ObtenerMejorCalificados(
            [Description("Número de restaurantes a retornar")] int cantidad = 5)
        {
            var restaurantes = await _context.Restaurantes
                .OrderByDescending(r => r.Rating)
                .Take(cantidad)
                .Select(r => new
                {
                    r.Nombre,
                    r.TipoCocina,
                    r.Rating,
                    r.RangoPrecios,
                    r.Direccion,
                    TotalResenas = r.Resenas.Count
                })
                .ToListAsync();

            return JsonSerializer.Serialize(restaurantes, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
        }
    }
}