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
    public class ReservasController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IEmailService _emailService;

        public ReservasController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            IEmailService emailService)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
        }

        // GET: Mis Reservas
        public async Task<IActionResult> Index()
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

        // GET: Nueva Reserva
        public async Task<IActionResult> Create(int restauranteId)
        {
            var restaurante = await _context.Restaurantes
                .Include(r => r.Productos.Where(p => p.Disponible))
                .FirstOrDefaultAsync(r => r.Id == restauranteId);

            if (restaurante == null)
                return NotFound();

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

        // POST: Nueva Reserva
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Reserva reserva, string productosJson)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null)
                return Unauthorized();

            var user = await _userManager.GetUserAsync(User);

            // Validar fecha
            var fechaHoraReserva = reserva.FechaReserva.Date + reserva.HoraReserva;
            if (fechaHoraReserva <= DateTime.Now)
            {
                TempData["Error"] = "La fecha y hora de la reserva debe ser futura";
                return RedirectToAction("Create", new { restauranteId = reserva.RestauranteId });
            }

            if (ModelState.IsValid)
            {
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
                        var productos = System.Text.Json.JsonSerializer.Deserialize<List<ProductoReservaDto>>(productosJson);
                        if (productos != null)
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
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error procesando productos: {ex.Message}");
                    }
                }

                // Enviar notificación
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
                    Console.WriteLine($"Error enviando email: {ex.Message}");
                }

                TempData["Success"] = $"¡Reserva creada exitosamente! Código: {reserva.CodigoReserva}";
                return RedirectToAction(nameof(Details), new { id = reserva.Id });
            }

            var rest = await _context.Restaurantes
                .Include(r => r.Productos.Where(p => p.Disponible))
                .FirstOrDefaultAsync(r => r.Id == reserva.RestauranteId);

            ViewBag.Restaurante = rest;
            ViewBag.Productos = rest?.Productos;

            return View(reserva);
        }

        // GET: Detalles de Reserva
        public async Task<IActionResult> Details(int id)
        {
            var userId = _userManager.GetUserId(User);
            var user = await _userManager.GetUserAsync(User);

            var reserva = await _context.Reservas
                .Include(r => r.Restaurante)
                .Include(r => r.ProductosReservados)
                    .ThenInclude(rp => rp.Producto)
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

            if (reserva == null)
                return NotFound();

            reserva.NombreUsuario = user?.UserName ?? "Usuario";
            reserva.EmailUsuario = user?.Email ?? "";

            return View(reserva);
        }

        // GET: Editar Reserva
        public async Task<IActionResult> Edit(int id)
        {
            var userId = _userManager.GetUserId(User);

            var reserva = await _context.Reservas
                .Include(r => r.Restaurante)
                    .ThenInclude(r => r.Productos.Where(p => p.Disponible))
                .Include(r => r.ProductosReservados)
                    .ThenInclude(rp => rp.Producto)
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

            if (reserva == null)
                return NotFound();

            if (reserva.Estado == "Cancelada" || reserva.Estado == "Completada")
            {
                TempData["Error"] = "No puedes editar una reserva cancelada o completada";
                return RedirectToAction(nameof(Details), new { id });
            }

            ViewBag.Restaurante = reserva.Restaurante;
            ViewBag.Productos = reserva.Restaurante?.Productos;

            return View(reserva);
        }

        // POST: Editar Reserva
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Reserva reserva, string productosJson)
        {
            if (id != reserva.Id)
                return NotFound();

            var userId = _userManager.GetUserId(User);
            var reservaExistente = await _context.Reservas
                .Include(r => r.ProductosReservados)
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

            if (reservaExistente == null)
                return NotFound();

            // Validar fecha
            var fechaHoraReserva = reserva.FechaReserva.Date + reserva.HoraReserva;
            if (fechaHoraReserva <= DateTime.Now)
            {
                TempData["Error"] = "La fecha y hora de la reserva debe ser futura";
                return RedirectToAction("Edit", new { id });
            }

            if (ModelState.IsValid)
            {
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
                        var productos = System.Text.Json.JsonSerializer.Deserialize<List<ProductoReservaDto>>(productosJson);
                        if (productos != null)
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
                        Console.WriteLine($"Error procesando productos: {ex.Message}");
                    }
                }

                await _context.SaveChangesAsync();

                TempData["Success"] = "Reserva actualizada exitosamente";
                return RedirectToAction(nameof(Details), new { id });
            }

            var rest = await _context.Restaurantes
                .Include(r => r.Productos.Where(p => p.Disponible))
                .FirstOrDefaultAsync(r => r.Id == reserva.RestauranteId);

            ViewBag.Restaurante = rest;
            ViewBag.Productos = rest?.Productos;

            return View(reserva);
        }

        // POST: Cancelar Reserva
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var userId = _userManager.GetUserId(User);
            var reserva = await _context.Reservas
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

            if (reserva == null)
                return NotFound();

            reserva.Estado = "Cancelada";
            await _context.SaveChangesAsync();

            TempData["Success"] = "Reserva cancelada exitosamente";
            return RedirectToAction(nameof(Index));
        }

        private string GenerarCodigoReserva()
        {
            var random = new Random();
            var codigo = $"RES{DateTime.Now:yyyyMMdd}{random.Next(1000, 9999)}";
            return codigo;
        }
    }

    // DTO para productos
    public class ProductoReservaDto
    {
        public int ProductoId { get; set; }
        public int Cantidad { get; set; }
        public string? Notas { get; set; }
    }
}