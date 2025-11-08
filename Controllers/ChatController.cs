using Microsoft.AspNetCore.Mvc;
using DondeComemos.Services;

namespace DondeComemos.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;
        private readonly ILogger<ChatController> _logger;

        public ChatController(IChatService chatService, ILogger<ChatController> logger)
        {
            _chatService = chatService;
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

                var response = await _chatService.GetChatResponseAsync(
                    request.Message, 
                    request.ConversationHistory ?? ""
                );

                return Ok(new { response });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error en chat: {ex.Message}");
                return StatusCode(500, new { error = "Error procesando el mensaje" });
            }
        }
    }

    public class ChatRequest
    {
        public string Message { get; set; } = string.Empty;
        public string? ConversationHistory { get; set; }
    }
}