using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using DondeComemos.Data;
using DondeComemos.Plugins;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace DondeComemos.Services
{
    public interface ISemanticKernelService
    {
        Task<string> GetChatResponseAsync(string userMessage, List<ChatMessageContent> history);
        Task<string> GetResponseWithPlanningAsync(string userMessage);
    }

    public class SemanticKernelService : ISemanticKernelService
    {
        private readonly ILogger<SemanticKernelService> _logger;
        private readonly ApplicationDbContext _context;
        private readonly string _apiKey;
        private readonly RestaurantPlugin _restaurantPlugin;
        private readonly ReservationPlugin _reservationPlugin;
        private readonly MenuPlugin _menuPlugin;

        public SemanticKernelService(
            IConfiguration configuration,
            ILogger<SemanticKernelService> logger,
            ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
            
            // Obtener la API Key
            _apiKey = configuration["Anthropic:ApiKey"] ?? "";
            
            if (string.IsNullOrEmpty(_apiKey) || _apiKey == "TU_API_KEY_AQUI")
            {
                _logger.LogError("Anthropic API Key no configurada");
            }

            // Inicializar plugins
            _restaurantPlugin = new RestaurantPlugin(context);
            _reservationPlugin = new ReservationPlugin(context);
            _menuPlugin = new MenuPlugin(context);
        }

        public async Task<string> GetChatResponseAsync(
            string userMessage, 
            List<ChatMessageContent> history)
        {
            try
            {
                if (string.IsNullOrEmpty(_apiKey) || _apiKey == "TU_API_KEY_AQUI")
                {
                    return "⚠️ El sistema de IA no está configurado. Por favor, contacta al administrador para configurar la API Key de Anthropic en appsettings.json";
                }

                // Obtener contexto de la base de datos
                var restaurantesInfo = await GetRestaurantContextAsync();

                // Construir el historial de mensajes
                var messages = new List<object>();
                
                // Agregar historial previo
                foreach (var msg in history)
                {
                    messages.Add(new { 
                        role = msg.Role.ToString().ToLower(), 
                        content = msg.Content 
                    });
                }
                
                // Agregar mensaje actual del usuario
                messages.Add(new { role = "user", content = userMessage });

                var systemPrompt = $@"Eres un asistente virtual experto de DondeComemos, una plataforma para descubrir restaurantes en Arequipa, Perú.

INFORMACIÓN DE RESTAURANTES DISPONIBLES:
{restaurantesInfo}

CAPACIDADES:
- Buscar restaurantes por tipo de cocina, precio, servicios
- Obtener información detallada de restaurantes
- Verificar disponibilidad para reservas
- Buscar platos específicos en el menú
- Dar recomendaciones personalizadas

INSTRUCCIONES:
1. Responde de forma conversacional y amigable en español
2. Si te preguntan sobre restaurantes específicos, proporciona información detallada
3. Usa la información de la base de datos que te he proporcionado
4. Si no encuentras información específica, sugiere alternativas
5. Menciona siempre el nombre del restaurante cuando des recomendaciones
6. Incluye precios y ratings cuando sea relevante

Responde de forma natural, útil y amigable.";

                var requestBody = new
                {
                    model = "claude-sonnet-4-20250514",
                    max_tokens = 1500,
                    temperature = 0.7,
                    system = systemPrompt,
                    messages = messages
                };

                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("x-api-key", _apiKey);
                httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
                httpClient.Timeout = TimeSpan.FromSeconds(30);
                
                var jsonContent = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                _logger.LogInformation("Enviando petición a Anthropic API");

                var response = await httpClient.PostAsync(
                    "https://api.anthropic.com/v1/messages",
                    content
                );

                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Respuesta exitosa de Anthropic API");
                    
                    var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
                    if (responseData.TryGetProperty("content", out var contentArray) && 
                        contentArray.GetArrayLength() > 0)
                    {
                        var textContent = contentArray[0].GetProperty("text").GetString();
                        return textContent ?? "Lo siento, no pude generar una respuesta.";
                    }
                    
                    return "Lo siento, no pude procesar la respuesta del servidor.";
                }
                else
                {
                    _logger.LogError($"Error en API: {response.StatusCode} - {responseContent}");
                    
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        return "⚠️ Error de autenticación. La API Key configurada no es válida. Por favor verifica la configuración.";
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                    {
                        return "⚠️ Se ha excedido el límite de peticiones. Por favor espera un momento e intenta de nuevo.";
                    }
                    
                    return $"Lo siento, hubo un error en el servidor ({response.StatusCode}). Por favor intenta de nuevo.";
                }
            }
            catch (TaskCanceledException)
            {
                _logger.LogError("Timeout en la petición a Anthropic API");
                return "La petición tomó demasiado tiempo. Por favor intenta de nuevo.";
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError($"Error de conexión: {ex.Message}");
                return "No se pudo conectar con el servidor de IA. Por favor verifica tu conexión a internet.";
            }
            catch (JsonException ex)
            {
                _logger.LogError($"Error parseando JSON: {ex.Message}");
                return "Error procesando la respuesta del servidor. Por favor intenta de nuevo.";
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error inesperado en SemanticKernel: {ex.GetType().Name} - {ex.Message}");
                return "Ocurrió un error inesperado. Por favor intenta de nuevo.";
            }
        }

        public async Task<string> GetResponseWithPlanningAsync(string userMessage)
        {
            try
            {
                return await GetChatResponseAsync(userMessage, new List<ChatMessageContent>());
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error en Planning: {ex.Message}");
                return "Lo siento, no pude procesar tu consulta.";
            }
        }

        private async Task<string> GetRestaurantContextAsync()
        {
            try
            {
                var restaurantes = await _context.Restaurantes
                    .Include(r => r.Productos.Where(p => p.Disponible))
                    .Select(r => new
                    {
                        r.Nombre,
                        r.TipoCocina,
                        r.RangoPrecios,
                        r.Rating,
                        r.Direccion,
                        r.Telefono,
                        r.DeliveryDisponible,
                        r.AceptaReservas,
                        r.OpcionesVegetarianas,
                        r.OpcionesVeganas,
                        TotalResenas = r.Resenas.Count,
                        ProductosDestacados = r.Productos
                            .Where(p => p.Disponible && p.RecomendacionChef)
                            .Take(3)
                            .Select(p => new { p.Nombre, p.Precio })
                            .ToList()
                    })
                    .Take(15)
                    .ToListAsync();

                if (!restaurantes.Any())
                {
                    return "No hay restaurantes registrados en el sistema actualmente.";
                }

                var sb = new StringBuilder();
                sb.AppendLine($"Total de restaurantes: {restaurantes.Count}\n");
                
                foreach (var r in restaurantes)
                {
                    sb.AppendLine($"📍 {r.Nombre}");
                    sb.AppendLine($"   - Cocina: {r.TipoCocina}");
                    sb.AppendLine($"   - Precio: {r.RangoPrecios}");
                    sb.AppendLine($"   - Rating: {r.Rating:F1}/5.0 ({r.TotalResenas} reseñas)");
                    sb.AppendLine($"   - Ubicación: {r.Direccion}");
                    
                    if (!string.IsNullOrEmpty(r.Telefono))
                        sb.AppendLine($"   - Teléfono: {r.Telefono}");
                    
                    var servicios = new List<string>();
                    if (r.DeliveryDisponible) servicios.Add("Delivery");
                    if (r.AceptaReservas) servicios.Add("Reservas");
                    if (r.OpcionesVegetarianas) servicios.Add("Vegetariano");
                    if (r.OpcionesVeganas) servicios.Add("Vegano");
                    
                    if (servicios.Any())
                        sb.AppendLine($"   - Servicios: {string.Join(", ", servicios)}");
                    
                    if (r.ProductosDestacados.Any())
                    {
                        sb.AppendLine($"   - Platos destacados:");
                        foreach (var p in r.ProductosDestacados)
                        {
                            sb.AppendLine($"     • {p.Nombre} - S/ {p.Precio:F2}");
                        }
                    }
                    
                    sb.AppendLine();
                }

                var tiposCocina = restaurantes.Select(r => r.TipoCocina).Distinct().ToList();
                sb.AppendLine($"Tipos de cocina disponibles: {string.Join(", ", tiposCocina)}");

                return sb.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error obteniendo contexto: {ex.Message}");
                return "Error al cargar información de restaurantes.";
            }
        }
    }
}