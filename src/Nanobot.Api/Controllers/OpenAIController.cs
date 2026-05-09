using Microsoft.AspNetCore.Mvc;
using Nanobot.Api.Models;
using Nanobot.Api.Services;

namespace Nanobot.Api.Controllers;

[ApiController]
[Route("v1")]
public class OpenAIController : ControllerBase
{
    private readonly ApiService _apiService;
    private readonly ILogger<OpenAIController> _logger;

    public OpenAIController(ApiService apiService, ILogger<OpenAIController> logger)
    {
        _apiService = apiService;
        _logger = logger;
    }

    [HttpPost("chat/completions")]
    public async Task<IActionResult> CreateChatCompletion([FromBody] ChatCompletionRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await _apiService.EnsureInitializedAsync();
            
            if (request.Stream)
            {
                Response.Headers.Add("Content-Type", "text/event-stream");
                Response.Headers.Add("Cache-Control", "no-cache");
                Response.Headers.Add("Connection", "keep-alive");
                
                await foreach (var chunk in _apiService.CreateCompletionStreamAsync(request, cancellationToken))
                {
                    var json = System.Text.Json.JsonSerializer.Serialize(chunk);
                    await Response.WriteAsync($"data: {json}\n\n", cancellationToken);
                    await Response.Body.FlushAsync(cancellationToken);
                }
                
                return new EmptyResult();
            }
            else
            {
                var response = await _apiService.CreateCompletionAsync(request, cancellationToken);
                return Ok(response);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating chat completion");
            return StatusCode(500, new { error = new { message = ex.Message } });
        }
    }

    [HttpGet("models")]
    public IActionResult ListModels()
    {
        try
        {
            var models = _apiService.GetModels();
            return Ok(models);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing models");
            return StatusCode(500, new { error = new { message = ex.Message } });
        }
    }

    [HttpGet("models/{modelId}")]
    public IActionResult GetModel(string modelId)
    {
        try
        {
            var models = _apiService.GetModels();
            var model = models.Data.FirstOrDefault(m => m.Id == modelId);
            
            if (model == null)
            {
                return NotFound(new { error = new { message = $"Model '{modelId}' not found" } });
            }
            
            return Ok(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting model");
            return StatusCode(500, new { error = new { message = ex.Message } });
        }
    }

    [HttpPost("embeddings")]
    public async Task<IActionResult> CreateEmbedding([FromBody] EmbeddingsRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await _apiService.EnsureInitializedAsync();
            
            var response = await _apiService.CreateEmbeddingAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating embedding");
            return StatusCode(500, new { error = new { message = ex.Message } });
        }
    }
}

// Health check endpoint
[ApiController]
[Route("")]
public class HealthController : ControllerBase
{
    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { status = "ok", timestamp = DateTime.UtcNow });
    }

    [HttpGet("")]
    public IActionResult Root()
    {
        return Ok(new
        {
            name = "Nanobot API",
            version = "0.1.5",
            docs = "/docs",
            endpoints = new[]
            {
                "POST /v1/chat/completions",
                "GET /v1/models",
                "POST /v1/embeddings"
            }
        });
    }
}