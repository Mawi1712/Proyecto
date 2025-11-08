using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using DondeComemos.Data;
using DondeComemos.Plugins;
using System.Text.Json;

namespace DondeComemos.Services
{
    public interface ISemanticKernelService
    {
        Task<string> GetChatResponseAsync(string userMessage, List<ChatMessageContent> history);
        Task<string> GetResponseWithPlanningAsync(string userMessage);
    }

    public class SemanticKernelService : ISemanticKernelService
    {
        private readonly Kernel _kernel;
        private readonly IChatCompletionService _chatService;
        private readonly ILogger<SemanticKernelService> _logger;

        public SemanticKernelService(
            IConfiguration configuration,
            ILogger<SemanticKernelService> logger,
            ApplicationDbContext context)
        {
            _logger = logger;

            // Crear el Kernel Builder
            var builder = Kernel.CreateBuilder();

            // Configurar el servicio de chat (usando endpoint de Anthropic compatible)
            builder.AddOpenAIChatCompletion(
                modelId: "claude-sonnet-4-20250514",
                apiKey: "not-needed", // El sistema ya maneja la autenticación
                endpoint: new Uri("https://api.anthropic.com/v1")
            );

            // Registrar los plugins
            builder.Plugins.AddFromObject(new RestaurantPlugin(context), "RestaurantPlugin");
            builder.Plugins.AddFromObject(new ReservationPlugin(context), "ReservationPlugin");
            builder.Plugins.AddFromObject(new MenuPlugin(context), "MenuPlugin");

            _kernel = builder.Build();
            _chatService = _kernel.GetRequiredService<IChatCompletionService>();
        }

        public async Task<string> GetChatResponseAsync(
            string userMessage, 
            List<ChatMessageContent> history)
        {
            try
            {
                // Crear el historial de chat
                var chatHistory = new ChatHistory();
                
                // Agregar mensaje de sistema
                chatHistory.AddSystemMessage(@"Eres un asistente virtual experto de DondeComemos, una plataforma para descubrir restaurantes en Arequipa, Perú.

CAPACIDADES:
- Puedes buscar restaurantes por tipo de cocina, precio, servicios
- Puedes obtener información detallada de restaurantes
- Puedes verificar disponibilidad para reservas
- Puedes buscar platos específicos en el menú
- Puedes dar recomendaciones personalizadas

INSTRUCCIONES:
1. Usa las funciones/herramientas disponibles cuando el usuario haga preguntas específicas
2. Sé conversacional y amigable
3. Si encuentras información relevante en las herramientas, úsala para dar respuestas precisas
4. Si no puedes encontrar algo, sugiere alternativas
5. Siempre menciona el nombre del restaurante cuando des recomendaciones
6. Incluye precios cuando sea relevante

Responde en español de forma natural y útil.");

                // Agregar historial previo
                foreach (var message in history)
                {
                    chatHistory.Add(message);
                }

                // Agregar mensaje del usuario
                chatHistory.AddUserMessage(userMessage);

                // Configurar opciones para usar funciones automáticamente
                var executionSettings = new OpenAIPromptExecutionSettings
                {
                    ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
                    Temperature = 0.7,
                    MaxTokens = 1000
                };

                // Obtener respuesta
                var response = await _chatService.GetChatMessageContentAsync(
                    chatHistory,
                    executionSettings,
                    _kernel
                );

                return response.Content ?? "Lo siento, no pude procesar tu solicitud.";
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error en SemanticKernel: {ex.Message}");
                return "Lo siento, ocurrió un error procesando tu mensaje. Por favor, intenta de nuevo.";
            }
        }

        public async Task<string> GetResponseWithPlanningAsync(string userMessage)
        {
            try
            {
                // Esta función usaría el Planner para tareas complejas
                // Por ahora, delegamos a la función principal
                return await GetChatResponseAsync(userMessage, new List<ChatMessageContent>());
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error en Planning: {ex.Message}");
                return "Lo siento, no pude crear un plan para tu solicitud.";
            }
        }
    }
}