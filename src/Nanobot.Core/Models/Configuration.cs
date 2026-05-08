namespace Nanobot.Core.Models;

public record NanobotConfig
{
    public Dictionary<string, ProviderConfig> Providers { get; init; } = new();
    public AgentsConfig Agents { get; init; } = new();
    public ChannelsConfig Channels { get; init; } = new();
    public SessionConfig Session { get; init; } = new();
    public SkillsConfig Skills { get; init; } = new();
    public CronConfig? Cron { get; init; }
    public SecurityConfig? Security { get; init; }
}

public record ProviderConfig
{
    public string? ApiKey { get; init; }
    public string? BaseUrl { get; init; }
    public Dictionary<string, object>? AdditionalProperties { get; init; }
}

public record AgentsConfig
{
    public AgentDefaults Defaults { get; set; } = new();
    public Dictionary<string, AgentConfig> Named { get; set; } = new();
}

public record AgentDefaults
{
    public string? Provider { get; set; }
    public string? Model { get; set; }
    public double Temperature { get; set; } = 0.7;
    public int MaxTokens { get; set; } = 4096;
}

public record AgentConfig : AgentDefaults
{
    public string? SystemPrompt { get; init; }
}

public record ChannelsConfig
{
    public WebSocketChannelConfig? WebSocket { get; init; }
    public Dictionary<string, object> AdditionalChannels { get; init; } = new();
}

public record WebSocketChannelConfig
{
    public bool Enabled { get; init; }
    public int Port { get; init; } = 8765;
    public string Host { get; init; } = "localhost";
}

public record SessionConfig
{
    public string Workspace { get; init; } = "~/.nanobot/workspace";
    public string StoragePath { get; init; } = "~/.nanobot/sessions";
    public int MaxHistory { get; init; } = 50;
}

public record SkillsConfig
{
    public List<string> DisabledSkills { get; init; } = new();
    public string SkillsPath { get; init; } = "~/.nanobot/skills";
}

public record CronConfig
{
    public bool Enabled { get; init; } = true;
    public string StoragePath { get; init; } = "~/.nanobot/cron";
}

public record SecurityConfig
{
    public List<string> AllowedPaths { get; init; } = new();
    public List<string> BlockedCommands { get; init; } = new();
    public bool SandboxEnabled { get; init; } = true;
}
