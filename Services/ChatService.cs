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
        private readonly string _apiKey;

        public ChatService(
            IConfiguration configuration, 
            ILogger<ChatService> logger,
            ApplicationDbContext context)
        {
            _configuration = configuration;
            _logger = logger;
            _context = context;
            
            // Obtener la API Key de la configuración
            _apiKey = configuration["Anthropic:ApiKey"] ?? "";
            
            if (string.IsNullOrEmpty(_apiKey) || _apiKey == "TU_API_KEY_AQUI")
            {
                _logger.LogWarning("Anthropic API Key no configurada correctamente");
            }
        }

        public async Task<string> GetChatResponseAsync(string userMessage, string conversationHistory)
        {
            try
            {
                // Verificar que la API Key esté configurada
                if (string.IsNullOrEmpty(_apiKey) || _apiKey == "TU_API_KEY_AQUI")
                {
                    return "⚠️ El chatbot no está configurado. Por favor, contacta al administrador para configurar la API Key de Anthropic.";
                }

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
                    new { role = "user", content = userMessage }
                };

                // Agregar historial de conversación si existe
                if (!string.IsNullOrEmpty(conversationHistory))
                {
                    try
                    {
                        var history = JsonSerializer.Deserialize<List<Dictionary<string, string>>>(conversationHistory);
                        if (history != null && history.Count > 0)
                        {
                            var historyMessages = new List<object>();
                            foreach (var msg in history)
                            {
                                historyMessages.Add(new { role = msg["role"], content = msg["content"] });
                            }
                            historyMessages.Add(new { role = "user", content = userMessage });
                            messages = historyMessages;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"Error procesando historial: {ex.Message}");
                    }
                }

                var requestBody = new
                {
                    model = "claude-sonnet-4-20250514",
                    max_tokens = 1000,
                    system = systemPrompt,
                    messages = messages
                };

                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("x-api-key", _apiKey);
                httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
                
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
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Error en API de Claude: {response.StatusCode} - {errorContent}");
                    
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        return "⚠️ Error de autenticación con Anthropic API. Por favor verifica la configuración de la API Key.";
                    }
                    
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