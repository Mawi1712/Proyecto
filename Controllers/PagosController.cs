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
    public class PagosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IPaymentService _paymentService;

        public PagosController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            IPaymentService paymentService)
        {
            _context = context;
            _userManager = userManager;
            _paymentService = paymentService;
        }

        [HttpPost]
        public async Task<IActionResult> IniciarPago(int reservaId, decimal monto)
        {
            var userId = _userManager.GetUserId(User);
            var user = await _userManager.GetUserAsync(User);
            
            var reserva = await _context.Reservas
                .FirstOrDefaultAsync(r => r.Id == reservaId && r.UserId == userId);

            if (reserva == null)
                return NotFound();

            try
            {
                // Crear registro de pago
                var pago = new Pago
                {
                    ReservaId = reservaId,
                    UserId = userId!,
                    Monto = monto,
                    Estado = "Pendiente",
                    Descripcion = "Pago adelantado de reserva"
                };

                _context.Pagos.Add(pago);
                await _context.SaveChangesAsync();

                // Crear sesión de Stripe
                var checkoutUrl = await _paymentService.CreateCheckoutSessionAsync(
                    reservaId,
                    user?.Email ?? "",
                    monto
                );

                return Redirect(checkoutUrl);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al procesar el pago: " + ex.Message;
                return RedirectToAction("Details", "Reservas", new { id = reservaId });
            }
        }

        public async Task<IActionResult> Success(string session_id)
        {
            try
            {
                var isPaid = await _paymentService.VerifyPaymentAsync(session_id);
                
                if (isPaid)
                {
                    var pago = await _context.Pagos
                        .FirstOrDefaultAsync(p => p.StripeSessionId == session_id);

                    if (pago != null)
                    {
                        pago.Estado = "Completado";
                        pago.FechaCompletado = DateTime.Now;
                        await _context.SaveChangesAsync();
                    }

                    TempData["Success"] = "¡Pago completado exitosamente!";
                }

                return View();
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error verificando el pago: " + ex.Message;
                return RedirectToAction("Index", "Reservas");
            }
        }

        public IActionResult Cancel()
        {
            TempData["Warning"] = "El pago fue cancelado";
            return View();
        }

        public async Task<IActionResult> MisPagos()
        {
            var userId = _userManager.GetUserId(User);
            
            var pagos = await _context.Pagos
                .Include(p => p.Reserva)
                    .ThenInclude(r => r!.Restaurante)
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.FechaCreacion)
                .ToListAsync();

            return View(pagos);
        }
    }
}