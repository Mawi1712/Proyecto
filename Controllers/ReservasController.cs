using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DondeComemos.Data;
using DondeComemos.Models;
using DondeComemos.Services;
using System.Text.Json;

namespace DondeComemos.Controllers
{
    [Authorize]
    public class ReservasController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly ILogger<ReservasController> _logger;

        public ReservasController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            IEmailService emailService,
            ILogger<ReservasController> logger)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
            _logger = logger;
        }

        // GET: Mis Reservas
        public async Task<IActionResult> Index()
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                var user = await _userManager.GetUserAsync(User);

                var reservas = await _context.Reservas
                    .Include(r => r.Restaurante)
                    .Include(r => r.ProductosReservados)
                        .ThenInclude(rp => rp.Producto)
                    .Where(r => r.UserId == userId)
                    .OrderByDescending(r => r.FechaReserva)
                    .ToListAsync();

                foreach (var reserva in reservas)
                {
                    reserva.NombreUsuario = user?.UserName ?? "Usuario";
                    reserva.EmailUsuario = user?.Email ?? "";
                }

                return View(reservas);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error en Index: {ex.Message}");
                TempData["Error"] = "Error al cargar las reservas";
                return View(new List<Reserva>());
            }
        }

        // GET: Nueva Reserva
        public async Task<IActionResult> Create(int restauranteId)
        {
            try
            {
                var restaurante = await _context.Restaurantes
                    .Include(r => r.Productos.Where(p => p.Disponible))
                    .FirstOrDefaultAsync(r => r.Id == restauranteId);

                if (restaurante == null)
                {
                    TempData["Error"] = "Restaurante no encontrado";
                    return RedirectToAction("Search", "Restaurantes");
                }

                ViewBag.Restaurante = restaurante;
                ViewBag.Productos = restaurante.Productos;

                var reserva = new Reserva
                {
                    RestauranteId = restauranteId,
                    FechaReserva = DateTime.Now.Date.AddDays(1),
                    HoraReserva = new TimeSpan(19, 0, 0),
                    NumeroPersonas = 2
                };

                return View(reserva);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error en Create GET: {ex.Message}");
                TempData["Error"] = "Error al cargar el formulario de reserva";
                return RedirectToAction("Search", "Restaurantes");
            }
        }

        // POST: Nueva Reserva
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Reserva reserva, string productosJson)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (userId == null)
                {
                    TempData["Error"] = "Debes iniciar sesión para hacer una reserva";
                    return RedirectToAction("Login", "Account", new { area = "Identity" });
                }

                var user = await _userManager.GetUserAsync(User);

                // Validar fecha y hora
                var fechaHoraReserva = reserva.FechaReserva.Date + reserva.HoraReserva;
                if (fechaHoraReserva <= DateTime.Now)
                {
                    TempData["Error"] = "La fecha y hora de la reserva debe ser futura";
                    return await RecargarVistaCreate(reserva.RestauranteId, reserva);
                }

                // Validar capacidad del restaurante
                var reservasEnHora = await _context.Reservas
                    .Where(r => r.RestauranteId == reserva.RestauranteId
                        && r.FechaReserva.Date == reserva.FechaReserva.Date
                        && r.HoraReserva == reserva.HoraReserva
                        && r.Estado != "Cancelada")
                    .CountAsync();

                if (reservasEnHora >= 10)
                {
                    TempData["Error"] = "No hay disponibilidad en ese horario. Por favor, selecciona otro.";
                    return await RecargarVistaCreate(reserva.RestauranteId, reserva);
                }

                // Crear la reserva
                reserva.UserId = userId;
                reserva.Estado = "Pendiente";
                reserva.FechaCreacion = DateTime.Now;
                reserva.CodigoReserva = GenerarCodigoReserva();

                _context.Reservas.Add(reserva);
                await _context.SaveChangesAsync();

                // Procesar productos seleccionados
                if (!string.IsNullOrEmpty(productosJson))
                {
                    try
                    {
                        var productos = JsonSerializer.Deserialize<List<ProductoReservaDto>>(productosJson);
                        if (productos != null && productos.Any())
                        {
                            foreach (var prod in productos)
                            {
                                var reservaProducto = new ReservaProducto
                                {
                                    ReservaId = reserva.Id,
                                    ProductoId = prod.ProductoId,
                                    Cantidad = prod.Cantidad,
                                    NotasProducto = prod.Notas
                                };
                                _context.ReservaProductos.Add(reservaProducto);
                            }
                            await _context.SaveChangesAsync();
                            _logger.LogInformation($"Se agregaron {productos.Count} productos a la reserva {reserva.Id}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error procesando productos: {ex.Message}");
                    }
                }

                // Crear notificación
                var notificacion = new Notificacion
                {
                    UserId = userId,
                    Titulo = "Reserva Creada",
                    Mensaje = $"Tu reserva en {reserva.Restaurante?.Nombre} ha sido creada. Código: {reserva.CodigoReserva}",
                    Tipo = "Success",
                    Url = $"/Reservas/Details/{reserva.Id}"
                };
                _context.Notificaciones.Add(notificacion);
                await _context.SaveChangesAsync();

                // Enviar email
                try
                {
                    if (user?.Email != null)
                    {
                        var restaurante = await _context.Restaurantes.FindAsync(reserva.RestauranteId);
                        await _emailService.SendReservationConfirmationAsync(
                            user.Email,
                            user.UserName ?? "Usuario",
                            restaurante?.Nombre ?? "Restaurante",
                            fechaHoraReserva,
                            reserva.CodigoReserva
                        );
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Error enviando email: {ex.Message}");
                }

                TempData["Success"] = $"¡Reserva creada exitosamente! Código: {reserva.CodigoReserva}";
                return RedirectToAction(nameof(Details), new { id = reserva.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error en Create POST: {ex.Message}");
                TempData["Error"] = "Error al crear la reserva. Por favor, intenta de nuevo.";
                return await RecargarVistaCreate(reserva.RestauranteId, reserva);
            }
        }

        // GET: Detalles de Reserva
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                var user = await _userManager.GetUserAsync(User);

                var reserva = await _context.Reservas
                    .Include(r => r.Restaurante)
                    .Include(r => r.ProductosReservados)
                        .ThenInclude(rp => rp.Producto)
                    .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

                if (reserva == null)
                {
                    TempData["Error"] = "Reserva no encontrada";
                    return RedirectToAction(nameof(Index));
                }

                reserva.NombreUsuario = user?.UserName ?? "Usuario";
                reserva.EmailUsuario = user?.Email ?? "";

                return View(reserva);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error en Details: {ex.Message}");
                TempData["Error"] = "Error al cargar los detalles de la reserva";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Editar Reserva
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var userId = _userManager.GetUserId(User);

                var reserva = await _context.Reservas
                    .Include(r => r.Restaurante)
                        .ThenInclude(r => r.Productos.Where(p => p.Disponible))
                    .Include(r => r.ProductosReservados)
                        .ThenInclude(rp => rp.Producto)
                    .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

                if (reserva == null)
                {
                    TempData["Error"] = "Reserva no encontrada";
                    return RedirectToAction(nameof(Index));
                }

                if (reserva.Estado == "Cancelada" || reserva.Estado == "Completada")
                {
                    TempData["Error"] = "No puedes editar una reserva cancelada o completada";
                    return RedirectToAction(nameof(Details), new { id });
                }

                ViewBag.Restaurante = reserva.Restaurante;
                ViewBag.Productos = reserva.Restaurante?.Productos;

                return View(reserva);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error en Edit GET: {ex.Message}");
                TempData["Error"] = "Error al cargar el formulario de edición";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Editar Reserva
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Reserva reserva, string productosJson)
        {
            if (id != reserva.Id)
            {
                TempData["Error"] = "Error en los datos de la reserva";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var userId = _userManager.GetUserId(User);
                var reservaExistente = await _context.Reservas
                    .Include(r => r.ProductosReservados)
                    .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

                if (reservaExistente == null)
                {
                    TempData["Error"] = "Reserva no encontrada";
                    return RedirectToAction(nameof(Index));
                }

                // Validar fecha
                var fechaHoraReserva = reserva.FechaReserva.Date + reserva.HoraReserva;
                if (fechaHoraReserva <= DateTime.Now)
                {
                    TempData["Error"] = "La fecha y hora de la reserva debe ser futura";
                    return RedirectToAction("Edit", new { id });
                }

                // Actualizar campos
                reservaExistente.FechaReserva = reserva.FechaReserva;
                reservaExistente.HoraReserva = reserva.HoraReserva;
                reservaExistente.NumeroPersonas = reserva.NumeroPersonas;
                reservaExistente.NotasEspeciales = reserva.NotasEspeciales;

                // Eliminar productos anteriores
                _context.ReservaProductos.RemoveRange(reservaExistente.ProductosReservados);

                // Agregar nuevos productos
                if (!string.IsNullOrEmpty(productosJson))
                {
                    try
                    {
                        var productos = JsonSerializer.Deserialize<List<ProductoReservaDto>>(productosJson);
                        if (productos != null && productos.Any())
                        {
                            foreach (var prod in productos)
                            {
                                var reservaProducto = new ReservaProducto
                                {
                                    ReservaId = reservaExistente.Id,
                                    ProductoId = prod.ProductoId,
                                    Cantidad = prod.Cantidad,
                                    NotasProducto = prod.Notas
                                };
                                _context.ReservaProductos.Add(reservaProducto);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error procesando productos: {ex.Message}");
                    }
                }

                await _context.SaveChangesAsync();

                TempData["Success"] = "Reserva actualizada exitosamente";
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error en Edit POST: {ex.Message}");
                TempData["Error"] = "Error al actualizar la reserva";
                return RedirectToAction("Edit", new { id });
            }
        }

        // POST: Cancelar Reserva
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                var reserva = await _context.Reservas
                    .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

                if (reserva == null)
                {
                    TempData["Error"] = "Reserva no encontrada";
                    return RedirectToAction(nameof(Index));
                }

                reserva.Estado = "Cancelada";
                await _context.SaveChangesAsync();

                TempData["Success"] = "Reserva cancelada exitosamente";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error en Cancel: {ex.Message}");
                TempData["Error"] = "Error al cancelar la reserva";
                return RedirectToAction(nameof(Index));
            }
        }

        // Método auxiliar para recargar la vista Create
        private async Task<IActionResult> RecargarVistaCreate(int restauranteId, Reserva reserva)
        {
            var restaurante = await _context.Restaurantes
                .Include(r => r.Productos.Where(p => p.Disponible))
                .FirstOrDefaultAsync(r => r.Id == restauranteId);

            ViewBag.Restaurante = restaurante;
            ViewBag.Productos = restaurante?.Productos;
            return View("Create", reserva);
        }

        // Generar código de reserva único
        private string GenerarCodigoReserva()
        {
            var random = new Random();
            var codigo = $"RES{DateTime.Now:yyyyMMdd}{random.Next(1000, 9999)}";
            return codigo;
        }
    }

    // DTO para productos en reservas
    public class ProductoReservaDto
    {
        public int ProductoId { get; set; }
        public int Cantidad { get; set; }
        public string? Notas { get; set; }
    }
}