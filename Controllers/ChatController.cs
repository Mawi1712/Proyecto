using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel.ChatCompletion;
using DondeComemos.Services;
using System.Text.Json;
using Microsoft.SemanticKernel;

namespace DondeComemos.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly ISemanticKernelService _semanticKernelService;
        private readonly ILogger<ChatController> _logger;

        public ChatController(
            ISemanticKernelService semanticKernelService, 
            ILogger<ChatController> logger)
        {
            _semanticKernelService = semanticKernelService;
            _logger = logger;
        }

        [HttpPost("message")]
        public async Task<IActionResult> SendMessage([FromBody] ChatRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Message))
                {
                    return BadRequest(new { error = "El mensaje no puede estar vacío" });
                }

                // Convertir historial a formato de Semantic Kernel
                var history = new List<ChatMessageContent>();
                
                if (!string.IsNullOrEmpty(request.ConversationHistory))
                {
                    try
                    {
                        var historyData = JsonSerializer.Deserialize<List<Dictionary<string, string>>>(
                            request.ConversationHistory);
                        
                        if (historyData != null)
                        {
                            foreach (var msg in historyData)
                            {
                                if (msg["role"] == "user")
                                {
                                    history.Add(new ChatMessageContent(
                                        AuthorRole.User, 
                                        msg["content"]
                                    ));
                                }
                                else if (msg["role"] == "assistant")
                                {
                                    history.Add(new ChatMessageContent(
                                        AuthorRole.Assistant, 
                                        msg["content"]
                                    ));
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"Error procesando historial: {ex.Message}");
                    }
                }

                // Obtener respuesta usando Semantic Kernel
                var response = await _semanticKernelService.GetChatResponseAsync(
                    request.Message,
                    history
                );

                return Ok(new { response });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error en chat: {ex.Message}");
                return StatusCode(500, new { error = "Error procesando el mensaje" });
            }
        }

        [HttpPost("complex")]
        public async Task<IActionResult> ComplexQuery([FromBody] ChatRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Message))
                {
                    return BadRequest(new { error = "El mensaje no puede estar vacío" });
                }

                // Usar planning para consultas complejas
                var response = await _semanticKernelService.GetResponseWithPlanningAsync(
                    request.Message
                );

                return Ok(new { response });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error en consulta compleja: {ex.Message}");
                return StatusCode(500, new { error = "Error procesando la consulta" });
            }
        }
    }

    public class ChatRequest
    {
        public string Message { get; set; } = string.Empty;
        public string? ConversationHistory { get; set; }
    }
}