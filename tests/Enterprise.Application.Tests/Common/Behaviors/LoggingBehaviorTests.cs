using Enterprise.Application.Common.Behaviors;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;

namespace Enterprise.Application.Tests.Common.Behaviors;

public class LoggingBehaviorTests
{
    private readonly Mock<ILogger<LoggingBehavior<TestCommand, string>>> _loggerMock;

    public LoggingBehaviorTests()
    {
        _loggerMock = new Mock<ILogger<LoggingBehavior<TestCommand, string>>>();
    }

    public class TestCommand : IRequest<string>
    {
        public string Name { get; set; } = string.Empty;
    }

    [Fact]
    public async Task Handle_ShouldLogRequestHandling()
    {
        // Arrange
        var behavior = new LoggingBehavior<TestCommand, string>(_loggerMock.Object);
        var request = new TestCommand { Name = "Test" };
        var handlerCalled = false;

        Task<string> Next(CancellationToken ct)
        {
            handlerCalled = true;
            return Task.FromResult("Success");
        }

        // Act
        var result = await behavior.Handle(request, Next, CancellationToken.None);

        // Assert
        handlerCalled.Should().BeTrue();
        result.Should().Be("Success");

        // Verify logging started
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Handling TestCommand")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        // Verify logging completed
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Handled TestCommand")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenExceptionThrown_ShouldLogError()
    {
        // Arrange
        var behavior = new LoggingBehavior<TestCommand, string>(_loggerMock.Object);
        var request = new TestCommand { Name = "Test" };
        var expectedException = new InvalidOperationException("Test exception");

        Task<string> Next(CancellationToken ct)
        {
            throw expectedException;
        }

        // Act
        var act = async () => await behavior.Handle(request, Next, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Test exception");

        // Verify error logging
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error handling TestCommand")),
                expectedException,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldLogElapsedTime()
    {
        // Arrange
        var behavior = new LoggingBehavior<TestCommand, string>(_loggerMock.Object);
        var request = new TestCommand { Name = "Test" };

        async Task<string> Next(CancellationToken ct)
        {
            await Task.Delay(10, ct); // Small delay to ensure elapsed time > 0
            return "Success";
        }

        // Act
        await behavior.Handle(request, Next, CancellationToken.None);

        // Assert
        // Verify completion log includes elapsed time
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Handled TestCommand in") && v.ToString()!.Contains("ms")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenExceptionThrown_ShouldLogElapsedTimeBeforeRethrowing()
    {
        // Arrange
        var behavior = new LoggingBehavior<TestCommand, string>(_loggerMock.Object);
        var request = new TestCommand { Name = "Test" };
        var expectedException = new InvalidOperationException("Test exception");

        async Task<string> Next(CancellationToken ct)
        {
            await Task.Delay(10, ct);
            throw expectedException;
        }

        // Act
        var act = async () => await behavior.Handle(request, Next, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();

        // Verify error log includes elapsed time
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error handling TestCommand after") && v.ToString()!.Contains("ms")),
                expectedException,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldLogRequestName()
    {
        // Arrange
        var behavior = new LoggingBehavior<TestCommand, string>(_loggerMock.Object);
        var request = new TestCommand { Name = "Test" };

        Task<string> Next(CancellationToken ct) => Task.FromResult("Success");

        // Act
        await behavior.Handle(request, Next, CancellationToken.None);

        // Assert - Verify the exact request type name is logged
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("TestCommand")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeast(2)); // At least start and completion logs
    }
}
