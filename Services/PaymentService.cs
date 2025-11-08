using Stripe;
using Stripe.Checkout;

namespace DondeComemos.Services
{
    public interface IPaymentService
    {
        Task<string> CreateCheckoutSessionAsync(int reservaId, string userEmail, decimal amount);
        Task<bool> VerifyPaymentAsync(string sessionId);
    }

    public class PaymentService : IPaymentService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<PaymentService> _logger;

        public PaymentService(IConfiguration configuration, ILogger<PaymentService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            
            // Configurar Stripe con tu clave secreta
            StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"] ?? "sk_test_51234567890";
        }

        public async Task<string> CreateCheckoutSessionAsync(int reservaId, string userEmail, decimal amount)
        {
            try
            {
                var options = new SessionCreateOptions
                {
                    PaymentMethodTypes = new List<string> { "card" },
                    LineItems = new List<SessionLineItemOptions>
                    {
                        new SessionLineItemOptions
                        {
                            PriceData = new SessionLineItemPriceDataOptions
                            {
                                Currency = "pen",
                                UnitAmount = (long)(amount * 100), // Convertir a centavos
                                ProductData = new SessionLineItemPriceDataProductDataOptions
                                {
                                    Name = "Reserva de Restaurante",
                                    Description = $"Pago adelantado para reserva #{reservaId}",
                                },
                            },
                            Quantity = 1,
                        },
                    },
                    Mode = "payment",
                    SuccessUrl = $"{_configuration["AppUrl"]}/Pagos/Success?session_id={{CHECKOUT_SESSION_ID}}",
                    CancelUrl = $"{_configuration["AppUrl"]}/Pagos/Cancel",
                    CustomerEmail = userEmail,
                    Metadata = new Dictionary<string, string>
                    {
                        { "reserva_id", reservaId.ToString() }
                    }
                };

                var service = new SessionService();
                var session = await service.CreateAsync(options);

                return session.Url;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creando sesión de pago: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> VerifyPaymentAsync(string sessionId)
        {
            try
            {
                var service = new SessionService();
                var session = await service.GetAsync(sessionId);
                
                return session.PaymentStatus == "paid";
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error verificando pago: {ex.Message}");
                return false;
            }
        }
    }
}