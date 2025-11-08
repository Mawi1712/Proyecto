using System.Text;
using System.Text.Json;
using DondeComemos.Data;
using Microsoft.EntityFrameworkCore;

namespace DondeComemos.Services
{
    public interface IChatService
    {
        Task<string> GetChatResponseAsync(string userMessage, string conversationHistory);
    }

    public class ChatService : IChatService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ChatService> _logger;
        private readonly ApplicationDbContext _context;

        public ChatService(
            IConfiguration configuration, 
            ILogger<ChatService> logger,
            ApplicationDbContext context)
        {
            _configuration = configuration;
            _logger = logger;
            _context = context;
        }

        public async Task<string> GetChatResponseAsync(string userMessage, string conversationHistory)
        {
            try
            {
                // Obtener información del contexto sobre restaurantes
                var restaurantesInfo = await GetRestaurantContextAsync();
                
                var systemPrompt = $@"Eres un asistente virtual amigable de DondeComemos, una plataforma para descubrir restaurantes en Arequipa, Perú.

Tu objetivo es ayudar a los usuarios a:
- Encontrar restaurantes según sus preferencias
- Responder preguntas sobre tipos de cocina
- Dar recomendaciones basadas en precio, ubicación o rating
- Explicar cómo usar la plataforma

INFORMACIÓN DE RESTAURANTES DISPONIBLES:
{restaurantesInfo}

INSTRUCCIONES:
- Sé breve, amigable y útil
- Si te preguntan sobre un restaurante específico, proporciona información relevante
- Si no tienes información exacta, sugiere que busquen en la plataforma
- Usa emojis ocasionalmente para ser más amigable
- Responde en español
- Si te preguntan sobre hacer reservas, menciona que deben iniciar sesión";

                var messages = new List<object>
                {
                    new { role = "system", content = systemPrompt }
                };

                // Agregar historial de conversación si existe
                if (!string.IsNullOrEmpty(conversationHistory))
                {
                    var history = JsonSerializer.Deserialize<List<Dictionary<string, string>>>(conversationHistory);
                    if (history != null)
                    {
                        foreach (var msg in history)
                        {
                            messages.Add(new { role = msg["role"], content = msg["content"] });
                        }
                    }
                }

                // Agregar mensaje del usuario
                messages.Add(new { role = "user", content = userMessage });

                var requestBody = new
                {
                    model = "claude-sonnet-4-20250514",
                    max_tokens = 1000,
                    messages = messages
                };

                using var httpClient = new HttpClient();
                var jsonContent = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync(
                    "https://api.anthropic.com/v1/messages",
                    content
                );

                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    var responseData = JsonSerializer.Deserialize<JsonElement>(responseJson);
                    
                    var textContent = responseData
                        .GetProperty("content")[0]
                        .GetProperty("text")
                        .GetString();

                    return textContent ?? "Lo siento, no pude procesar tu solicitud.";
                }
                else
                {
                    _logger.LogError($"Error en API de Claude: {response.StatusCode}");
                    return "Lo siento, estoy teniendo problemas técnicos. Por favor, intenta de nuevo en un momento.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error en ChatService: {ex.Message}");
                return "Lo siento, ocurrió un error. Por favor, intenta de nuevo.";
            }
        }

        private async Task<string> GetRestaurantContextAsync()
        {
            try
            {
                var restaurantes = await _context.Restaurantes
                    .Select(r => new
                    {
                        r.Nombre,
                        r.TipoCocina,
                        r.RangoPrecios,
                        r.Rating,
                        r.Direccion,
                        TotalResenas = r.Resenas.Count
                    })
                    .Take(20)
                    .ToListAsync();

                var sb = new StringBuilder();
                sb.AppendLine("Restaurantes disponibles:");
                
                foreach (var r in restaurantes)
                {
                    sb.AppendLine($"- {r.Nombre}: {r.TipoCocina}, {r.RangoPrecios}, Rating: {r.Rating:F1}, Ubicación: {r.Direccion}");
                }

                var tiposCocina = restaurantes.Select(r => r.TipoCocina).Distinct().ToList();
                sb.AppendLine($"\nTipos de cocina disponibles: {string.Join(", ", tiposCocina)}");

                return sb.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error obteniendo contexto: {ex.Message}");
                return "No se pudo cargar la información de restaurantes.";
            }
        }
    }
}