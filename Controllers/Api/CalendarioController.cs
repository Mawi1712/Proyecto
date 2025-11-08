using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DondeComemos.Data;
using System.Globalization;

namespace DondeComemos.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class CalendarioController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CalendarioController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("disponibilidad/{restauranteId}")]
        public async Task<IActionResult> GetDisponibilidad(int restauranteId, [FromQuery] DateTime fecha)
        {
            try
            {
                var restaurante = await _context.Restaurantes.FindAsync(restauranteId);
                if (restaurante == null)
                    return NotFound(new { error = "Restaurante no encontrado" });

                // Obtener reservas para esa fecha
                var reservasDelDia = await _context.Reservas
                    .Where(r => r.RestauranteId == restauranteId 
                        && r.FechaReserva.Date == fecha.Date
                        && r.Estado != "Cancelada")
                    .ToListAsync();

                // Generar franjas horarias disponibles
                var horariosDisponibles = new List<object>();
                var horaInicio = new TimeSpan(12, 0, 0); // 12:00 PM
                var horaFin = new TimeSpan(22, 0, 0);    // 10:00 PM
                var intervalo = TimeSpan.FromMinutes(30);

                for (var hora = horaInicio; hora <= horaFin; hora += intervalo)
                {
                    var reservasEnHora = reservasDelDia.Count(r => r.HoraReserva == hora);
                    var capacidadDisponible = 10 - reservasEnHora; // Asumimos capacidad de 10 reservas por hora

                    horariosDisponibles.Add(new
                    {
                        hora = hora.ToString(@"hh\:mm"),
                        disponible = capacidadDisponible > 0,
                        capacidadDisponible = capacidadDisponible,
                        reservasActuales = reservasEnHora
                    });
                }

                return Ok(new
                {
                    fecha = fecha.ToString("yyyy-MM-dd"),
                    restaurante = restaurante.Nombre,
                    horarios = horariosDisponibles
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("eventos/{restauranteId}")]
        public async Task<IActionResult> GetEventos(int restauranteId, [FromQuery] DateTime start, [FromQuery] DateTime end)
        {
            try
            {
                var reservas = await _context.Reservas
                    .Include(r => r.Restaurante)
                    .Where(r => r.RestauranteId == restauranteId
                        && r.FechaReserva >= start.Date
                        && r.FechaReserva <= end.Date
                        && r.Estado != "Cancelada")
                    .ToListAsync();

                var eventos = reservas.Select(r => new
                {
                    id = r.Id,
                    title = $"{r.NumeroPersonas} personas - {r.CodigoReserva}",
                    start = r.FechaReserva.Date.Add(r.HoraReserva).ToString("yyyy-MM-ddTHH:mm:ss"),
                    backgroundColor = r.Estado == "Confirmada" ? "#28a745" : "#ffc107",
                    borderColor = r.Estado == "Confirmada" ? "#28a745" : "#ffc107",
                    textColor = "#fff",
                    extendedProps = new
                    {
                        numeroPersonas = r.NumeroPersonas,
                        estado = r.Estado,
                        codigoReserva = r.CodigoReserva,
                        notasEspeciales = r.NotasEspeciales
                    }
                }).ToList();

                return Ok(eventos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("estadisticas/{restauranteId}")]
        public async Task<IActionResult> GetEstadisticas(int restauranteId, [FromQuery] DateTime? fecha)
        {
            try
            {
                var fechaConsulta = fecha ?? DateTime.Today;
                
                var reservasDelMes = await _context.Reservas
                    .Where(r => r.RestauranteId == restauranteId
                        && r.FechaReserva.Year == fechaConsulta.Year
                        && r.FechaReserva.Month == fechaConsulta.Month
                        && r.Estado != "Cancelada")
                    .ToListAsync();

                var reservasDelDia = reservasDelMes
                    .Where(r => r.FechaReserva.Date == fechaConsulta.Date)
                    .ToList();

                var stats = new
                {
                    totalReservasHoy = reservasDelDia.Count,
                    personasHoy = reservasDelDia.Sum(r => r.NumeroPersonas),
                    totalReservasMes = reservasDelMes.Count,
                    personasMes = reservasDelMes.Sum(r => r.NumeroPersonas),
                    reservasPorDia = reservasDelMes
                        .GroupBy(r => r.FechaReserva.Date)
                        .Select(g => new
                        {
                            fecha = g.Key.ToString("yyyy-MM-dd"),
                            cantidad = g.Count(),
                            personas = g.Sum(r => r.NumeroPersonas)
                        })
                        .OrderBy(x => x.fecha)
                        .ToList(),
                    horasMasPopulares = reservasDelMes
                        .GroupBy(r => r.HoraReserva.Hours)
                        .Select(g => new
                        {
                            hora = $"{g.Key:00}:00",
                            cantidad = g.Count()
                        })
                        .OrderByDescending(x => x.cantidad)
                        .Take(5)
                        .ToList()
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}