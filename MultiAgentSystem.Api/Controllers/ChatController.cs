using Microsoft.AspNetCore.Mvc;
using MultiAgentSystem.Api.Agents;

namespace MultiAgentSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IOrchestratorAgent _orchestrator;
    private readonly ILogger<ChatController> _logger;

    public ChatController(IOrchestratorAgent orchestrator, ILogger<ChatController> logger)
    {
        _orchestrator = orchestrator;
        _logger = logger;
    }

    [HttpPost("query")]
    public async Task<IActionResult> Query([FromBody] ChatRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(new { message = "Message is required" });
        }

        try
        {
            // Extract bearer token from Authorization header
            string? authToken = null;
            if (Request.Headers.TryGetValue("Authorization", out var authHeader))
            {
                var headerValue = authHeader.ToString();
                if (headerValue.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    authToken = headerValue.Substring("Bearer ".Length).Trim();
                }
            }

            _logger.LogInformation("Processing chat query: {Message}, Has Auth: {HasAuth}", 
                request.Message, !string.IsNullOrEmpty(authToken));

            var response = await _orchestrator.ProcessQueryAsync(
                request.Message, 
                authToken, 
                cancellationToken);

            return Ok(new ChatResponse
            {
                Message = response.SynthesizedResponse,
                Timestamp = response.Timestamp,
                AgentsCalled = response.AgentsCalled,
                HasUserContext = response.HasAuthToken,
                Success = response.Success,
                Errors = response.Errors
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat query");
            return StatusCode(500, new 
            { 
                message = "An error occurred processing your request",
                error = ex.Message 
            });
        }
    }

    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new 
        { 
            status = "healthy",
            timestamp = DateTime.UtcNow,
            service = "Multi-Agent Chat System"
        });
    }
}

public class ChatRequest
{
    public string Message { get; set; } = string.Empty;
}

public class ChatResponse
{
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public List<string> AgentsCalled { get; set; } = new();
    public bool HasUserContext { get; set; }
    public bool Success { get; set; }
    public List<string> Errors { get; set; } = new();
}
