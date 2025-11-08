using System.ComponentModel;
using Microsoft.SemanticKernel;
using Microsoft.EntityFrameworkCore;
using DondeComemos.Data;
using System.Text.Json;

namespace DondeComemos.Plugins
{
    public class ReservationPlugin
    {
        private readonly ApplicationDbContext _context;

        public ReservationPlugin(ApplicationDbContext context)
        {
            _context = context;
        }

        [KernelFunction, Description("Verifica disponibilidad de un restaurante en una fecha específica")]
        public async Task<string> VerificarDisponibilidad(
            [Description("Nombre del restaurante")] string nombreRestaurante,
            [Description("Fecha en formato YYYY-MM-DD")] string fecha)
        {
            if (!DateTime.TryParse(fecha, out DateTime fechaParsed))
                return "Formato de fecha inválido. Use YYYY-MM-DD";

            var restaurante = await _context.Restaurantes
                .FirstOrDefaultAsync(r => r.Nombre.ToLower().Contains(nombreRestaurante.ToLower()));

            if (restaurante == null)
                return $"No se encontró el restaurante '{nombreRestaurante}'";

            var reservasDelDia = await _context.Reservas
                .Where(r => r.RestauranteId == restaurante.Id 
                    && r.FechaReserva.Date == fechaParsed.Date
                    && r.Estado != "Cancelada")
                .ToListAsync();

            var horariosDisponibles = new List<string>();
            var horaInicio = new TimeSpan(12, 0, 0);
            var horaFin = new TimeSpan(22, 0, 0);
            var intervalo = TimeSpan.FromMinutes(30);

            for (var hora = horaInicio; hora <= horaFin; hora += intervalo)
            {
                var reservasEnHora = reservasDelDia.Count(r => r.HoraReserva == hora);
                if (reservasEnHora < 10) // Capacidad de 10 por hora
                {
                    horariosDisponibles.Add(hora.ToString(@"hh\:mm"));
                }
            }

            var resultado = new
            {
                Restaurante = restaurante.Nombre,
                Fecha = fechaParsed.ToString("dd/MM/yyyy"),
                HorariosDisponibles = horariosDisponibles,
                Mensaje = horariosDisponibles.Any() 
                    ? "Hay disponibilidad en los siguientes horarios" 
                    : "No hay disponibilidad para esa fecha"
            };

            return JsonSerializer.Serialize(resultado, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
        }

        [KernelFunction, Description("Obtiene información sobre cómo hacer una reserva")]
        public string ObtenerInformacionReservas()
        {
            return @"Para hacer una reserva en DondeComemos:

1. Debes estar registrado e iniciar sesión
2. Busca el restaurante que te interesa
3. Ve a los detalles del restaurante
4. Haz clic en 'Hacer Reserva'
5. Selecciona fecha, hora y número de personas
6. Opcionalmente, pre-ordena platos del menú
7. Confirma tu reserva

Recibirás un código de reserva y una confirmación por correo electrónico.";
        }

        [KernelFunction, Description("Obtiene estadísticas de reservas de un restaurante")]
        public async Task<string> ObtenerEstadisticasReservas(
            [Description("Nombre del restaurante")] string nombreRestaurante)
        {
            var restaurante = await _context.Restaurantes
                .FirstOrDefaultAsync(r => r.Nombre.ToLower().Contains(nombreRestaurante.ToLower()));

            if (restaurante == null)
                return $"No se encontró el restaurante '{nombreRestaurante}'";

            var reservas = await _context.Reservas
                .Where(r => r.RestauranteId == restaurante.Id)
                .ToListAsync();

            var stats = new
            {
                Restaurante = restaurante.Nombre,
                TotalReservas = reservas.Count,
                ReservasActivas = reservas.Count(r => r.Estado == "Confirmada" || r.Estado == "Pendiente"),
                ReservasCanceladas = reservas.Count(r => r.Estado == "Cancelada"),
                PromedioPersonas = reservas.Any() ? reservas.Average(r => r.NumeroPersonas) : 0,
                HoraMasPopular = reservas
                    .GroupBy(r => r.HoraReserva)
                    .OrderByDescending(g => g.Count())
                    .Select(g => g.Key.ToString(@"hh\:mm"))
                    .FirstOrDefault()
            };

            return JsonSerializer.Serialize(stats, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
        }
    }
}