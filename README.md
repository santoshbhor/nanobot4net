# 🐈 Nanobot C# - .NET 10 Migration

This is a C# migration of the Python-based [nanobot](https://github.com/HKUDS/nanobot) project, rewritten to run on .NET 10.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (Preview or later)
- OpenAI or Anthropic API key

## Project Structure

```
Nanobot/
├── src/
│   ├── Nanobot.Core/          # Core models, interfaces, and services
│   ├── Nanobot.Providers/     # LLM provider implementations
│   ├── Nanobot.Agent/         # Agent core loop
│   ├── Nanobot.CLI/          # Command-line interface
│   ├── Nanobot.Channels/      # Communication channels (WebSocket, etc.)
│   └── Nanobot.Skills/       # Built-in skills and tool registry
├── tests/
│   └── Nanobot.Tests/        # Unit tests
└── Nanobot.sln                # Solution file
```

## Building

```bash
cd Nanobot
dotnet restore
dotnet build
```

## Running

### Initialize Configuration

```bash
dotnet run --project src/Nanobot.CLI -- onboard
```

### Start Interactive Agent

```bash
dotnet run --project src/Nanobot.CLI -- agent
```

### Start Gateway (WebSocket)

```bash
dotnet run --project src/Nanobot.CLI -- gateway
```

## Configuration

Configuration is stored at `~/.nanobot/config.json`. Example:

```json
{
  "providers": {
    "openai": {
      "apiKey": "sk-..."
    }
  },
  "agents": {
    "defaults": {
      "provider": "openai",
      "model": "gpt-4-turbo-preview"
    }
  }
}
```

## NuGet Packages Used

- `OpenAI` - OpenAI API client
- `Anthropic.SDK` - Anthropic API client  
- `System.CommandLine` - CLI parsing
- `System.Text.Json` - JSON serialization
- `Microsoft.Extensions.*` - Dependency injection, logging, configuration

## Migration Status

- [x] Core models and interfaces
- [x] Configuration system
- [x] OpenAI provider
- [x] Anthropic provider
- [x] Agent core loop
- [x] CLI commands (onboard, agent, gateway)
- [x] WebSocket channel
- [x] Built-in skills (shell, file operations)
- [x] Tool registry
- [ ] Session management (partial)
- [ ] Additional channels (Telegram, Discord, etc.)
- [ ] Cron/scheduling
- [ ] Memory system
- [ ] MCP (Model Context Protocol) support
- [ ] Full test coverage

## License

MIT License - See original [nanobot](https://github.com/HKUDS/nanobot) project for details.
