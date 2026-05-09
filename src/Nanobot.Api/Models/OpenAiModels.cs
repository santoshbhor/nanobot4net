namespace Nanobot.Api.Models;

// OpenAI Chat Completion compatible models

public class ChatCompletionRequest
{
    public string Model { get; set; } = "gpt-3.5-turbo";
    public List<ChatMessage> Messages { get; set; } = new();
    public double? Temperature { get; set; }
    public int? MaxTokens { get; set; }
    public float? TopP { get; set; }
    public int? N { get; set; }
    public bool Stream { get; set; }
    public List<ChatTool>? Tools { get; set; }
    public string? ToolChoice { get; set; }
    public string? User { get; set; }
}

public class ChatMessage
{
    public string Role { get; set; } = "user";
    public string Content { get; set; } = "";
    public string? Name { get; set; }
    public List<ChatToolCall>? ToolCalls { get; set; }
    public string? ToolCallId { get; set; }
}

public class ChatTool
{
    public string Type { get; set; } = "function";
    public ChatFunction Function { get; set; } = new();
}

public class ChatFunction
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public object Parameters { get; set; } = new { };
}

public class ChatToolCall
{
    public string Id { get; set; } = "";
    public string Type { get; set; } = "function";
    public ChatFunctionCall Function { get; set; } = new();
}

public class ChatFunctionCall
{
    public string Name { get; set; } = "";
    public string Arguments { get; set; } = "";
}

public class ChatCompletionResponse
{
    public string Id { get; set; } = "";
    public string Object { get; set; } = "chat.completion";
    public long Created { get; set; }
    public string Model { get; set; } = "";
    public List<ChatChoice> Choices { get; set; } = new();
    public Usage? Usage { get; set; }
}

public class ChatChoice
{
    public int Index { get; set; }
    public ChatMessage Message { get; set; } = new();
    public string FinishReason { get; set; } = "stop";
}

public class Usage
{
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public int TotalTokens { get; set; }
}

// Streaming response
public class ChatCompletionStreamResponse
{
    public string Id { get; set; } = "";
    public string Object { get; set; } = "chat.completion.chunk";
    public long Created { get; set; }
    public string Model { get; set; } = "";
    public List<ChatStreamChoice> Choices { get; set; } = new();
}

public class ChatStreamChoice
{
    public int Index { get; set; }
    public ChatMessageDelta? Delta { get; set; }
    public string FinishReason { get; set; } = "";
}

public class ChatMessageDelta
{
    public string? Role { get; set; }
    public string? Content { get; set; }
    public List<ChatToolCall>? ToolCalls { get; set; }
}

// Models
public class ModelsResponse
{
    public string Object { get; set; } = "list";
    public List<ModelData> Data { get; set; } = new();
}

public class ModelData
{
    public string Id { get; set; } = "";
    public string Object { get; set; } = "model";
    public long Created { get; set; }
    public string OwnedBy { get; set; } = "";
}

// Embeddings
public class EmbeddingsRequest
{
    public string Model { get; set; } = "text-embedding-ada-002";
    public string Input { get; set; } = "";
    public List<string>? InputArray { get; set; }
}

public class EmbeddingsResponse
{
    public string Object { get; set; } = "list";
    public string Model { get; set; } = "";
    public List<EmbeddingData> Data { get; set; } = new();
    public Usage? Usage { get; set; }
}

public class EmbeddingData
{
    public int Index { get; set; }
    public string Object { get; set; } = "embedding";
    public List<float> Embedding { get; set; } = new();
}