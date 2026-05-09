# 🐈 Nanobot C# - .NET 10 Migration

A C# migration of the Python-based [nanobot](https://github.com/HKUDS/nanobot) project, rewritten to run on .NET 10.

> ⚠️ **Migration Status: In Progress** - Not all features from the original Python version have been migrated yet.

## ✅ Completed Features

### Core
- Configuration system (JSON-based)
- Session management with persistence
- Tool registry with function calling
- MCP (Model Context Protocol) basic support

### LLM Providers
| Provider | Status |
|----------|--------|
| OpenAI | ✅ |
| Anthropic | ✅ |
| DeepSeek | ✅ |
| OpenRouter | ✅ |
| Azure OpenAI | ✅ |
| Qwen (DashScope) | ✅ |
| Kimi (Moonshot) | ✅ |
| Mistral | ✅ |
| Ollama (Local) | ✅ |
| VolcEngine | ✅ |
| StepFun | ✅ |

### Channels
- WebSocket - ✅
- Telegram - ✅
- Discord - ✅

### Tools & Skills
- `execute_shell` - Execute shell commands
- `read_file` - Read file contents
- `write_file` - Write to files
- `list_files` - List directory contents
- `web_search` - Web search (DuckDuckGo)

### Scheduling
- Cron service with standard cron expressions
- `@hourly`, `@daily`, `@weekly`, `@monthly` shortcuts
- CLI commands: `cron add`, `cron list`, `cron run`, `cron remove`

## ✅ WebUI Added (ASP.NET Core)

```bash
# Start WebUI
dotnet run --project src/Nanobot.WebUI

# Open in browser: http://localhost:5000
```

Features:
- Real-time chat with SignalR WebSockets
- Message history per session
- Clean, responsive UI
- Dark mode support

## ❌ Remaining to Migrate

### High Priority
- [ ] **API Server** - OpenAI-compatible REST API with SSE streaming
- [ ] **Event Bus** - Inter-component messaging system
- [ ] **Heartbeat** - Health monitoring for long-running agents

### Channels (More needed)
- [ ] Slack
- [ ] Feishu/Lark
- [ ] WhatsApp/WeChat
- [ ] DingTalk
- [ ] QQ
- [ ] WeCom
- [ ] Matrix
- [ ] Microsoft Teams
- [ ] Email

### Features
- [ ] Memory system (Dream - two-stage memory)
- [ ] Skills discovery and installation
- [ ] Document reading (PDF, DOCX, XLSX, PPTX)
- [ ] Security sandbox
- [ ] Langfuse observability
- [ ] LangSmith integration

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- OpenAI or Anthropic API key (for cloud providers)
- Ollama installed (for local models)

## Installation

```bash
# Clone the repository
git clone https://github.com/santoshbhor/nanobot4net.git
cd nanobot4net

# Build
dotnet build
```

## Configuration

Create `~/.nanobot/config.json`:

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

## Usage

```bash
# Initialize configuration
dotnet run --project src/Nanobot.CLI -- onboard

# Start interactive chat
dotnet run --project src/Nanobot.CLI -- agent

# Start WebSocket gateway
dotnet run --project src/Nanobot.CLI -- gateway

# Manage cron jobs
dotnet run --project src/Nanobot.CLI -- cron list
dotnet run --project src/Nanobot.CLI -- cron add myjob "0 9 * * *" "shell:echo morning"
```

## Project Structure

```
Nanobot/
├── src/
│   ├── Nanobot.Core/          # Core models, interfaces, services
│   ├── Nanobot.Providers/     # LLM provider implementations
│   ├── Nanobot.Agent/         # Agent core loop
│   ├── Nanobot.CLI/           # Command-line interface
│   ├── Nanobot.Channels/      # Communication channels
│   └── Nanobot.Skills/        # Built-in skills
├── tests/
│   └── Nanobot.Tests/         # Unit tests
└── Nanobot.sln
```

## License

MIT License - See original [nanobot](https://github.com/HKUDS/nanobot) project.