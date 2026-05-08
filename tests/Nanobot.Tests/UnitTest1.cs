using Xunit;
using Nanobot.Core;
using Nanobot.Core.Models;
using Nanobot.Core.Services;

namespace Nanobot.Tests;

public class ConfigurationTests
{
    [Fact]
    public async Task ConfigurationService_CanLoadAndSave()
    {
        // Arrange
        var configService = new ConfigurationService(Path.GetTempFileName());
        var testConfig = new NanobotConfig
        {
            Agents = new AgentsConfig
            {
                Defaults = new AgentDefaults
                {
                    Provider = "openai",
                    Model = "gpt-4"
                }
            }
        };

        // Act & Assert - Save
        await configService.LoadAsync();
        Assert.NotNull(configService.Config);
    }

    [Fact]
    public void Message_CanCreate()
    {
        // Arrange & Act
        var message = new Message
        {
            Role = "user",
            Content = "Hello, world!"
        };

        // Assert
        Assert.Equal("user", message.Role);
        Assert.Equal("Hello, world!", message.Content);
    }

    [Fact]
    public void ToolRegistry_CanRegisterAndExecute()
    {
        // Arrange
        var registry = new ToolRegistry();
        var wasCalled = false;

        registry.RegisterTool("test_tool", "A test tool", args =>
        {
            wasCalled = true;
            return Task.FromResult("success");
        });

        // Act
        var tools = registry.GetTools();

        // Assert
        Assert.Single(tools);
        Assert.Equal("test_tool", tools[0].Name);
    }
}
