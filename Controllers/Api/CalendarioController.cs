using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DondeComemos.Data;

namespace DondeComemos.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class CalendarioController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CalendarioController> _logger;

        public CalendarioController(ApplicationDbContext context, ILogger<CalendarioController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("disponibilidad/{restauranteId}")]
        public async Task<IActionResult> GetDisponibilidad(int restauranteId, [FromQuery] DateTime fecha)
        {
            try
            {
                var restaurante = await _context.Restaurantes.FindAsync(restauranteId);
                if (restaurante == null)
                    return NotFound(new { error = "Restaurante no encontrado" });

                // Validar que la fecha no sea muy antigua
                if (fecha.Date < DateTime.Now.Date.AddDays(-1))
                {
                    return BadRequest(new { error = "No se puede consultar disponibilidad de fechas antiguas" });
                }

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
                    var capacidadDisponible = 10 - reservasEnHora; // Capacidad de 10 reservas por hora

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
                _logger.LogError($"Error en GetDisponibilidad: {ex.Message}");
                return StatusCode(500, new { error = "Error al obtener disponibilidad", details = ex.Message });
            }
        }

        [HttpGet("eventos/{restauranteId}")]
        public async Task<IActionResult> GetEventos(int restauranteId, [FromQuery] DateTime start, [FromQuery] DateTime end)
        {
            try
            {
                var query = _context.Reservas
                    .Include(r => r.Restaurante)
                    .Where(r => r.FechaReserva >= start.Date
                        && r.FechaReserva <= end.Date
                        && r.Estado != "Cancelada");

                // Filtrar por restaurante si se especifica (0 = todos)
                if (restauranteId > 0)
                {
                    query = query.Where(r => r.RestauranteId == restauranteId);
                }

                var reservas = await query.ToListAsync();

                var eventos = reservas.Select(r => new
                {
                    id = r.Id,
                    title = $"{r.NumeroPersonas} personas - {r.CodigoReserva}",
                    start = r.FechaReserva.Date.Add(r.HoraReserva).ToString("yyyy-MM-ddTHH:mm:ss"),
                    backgroundColor = r.Estado == "Confirmada" ? "#28a745" : 
                                    r.Estado == "Pendiente" ? "#ffc107" : "#6c757d",
                    borderColor = r.Estado == "Confirmada" ? "#28a745" : 
                                 r.Estado == "Pendiente" ? "#ffc107" : "#6c757d",
                    textColor = "#fff",
                    extendedProps = new
                    {
                        numeroPersonas = r.NumeroPersonas,
                        estado = r.Estado,
                        codigoReserva = r.CodigoReserva,
                        notasEspeciales = r.NotasEspeciales,
                        restaurante = r.Restaurante?.Nombre
                    }
                }).ToList();

                return Ok(eventos);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error en GetEventos: {ex.Message}");
                return StatusCode(500, new { error = "Error al obtener eventos", details = ex.Message });
            }
        }

        [HttpGet("estadisticas/{restauranteId}")]
        public async Task<IActionResult> GetEstadisticas(int restauranteId, [FromQuery] DateTime? fecha)
        {
            try
            {
                var fechaConsulta = fecha ?? DateTime.Today;
                
                var query = _context.Reservas.Where(r => r.Estado != "Cancelada");

                // Filtrar por restaurante si se especifica
                if (restauranteId > 0)
                {
                    query = query.Where(r => r.RestauranteId == restauranteId);
                }

                var reservasDelMes = await query
                    .Where(r => r.FechaReserva.Year == fechaConsulta.Year
                        && r.FechaReserva.Month == fechaConsulta.Month)
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
                _logger.LogError($"Error en GetEstadisticas: {ex.Message}");
                return StatusCode(500, new { error = "Error al obtener estadísticas", details = ex.Message });
            }
        }
    }
}