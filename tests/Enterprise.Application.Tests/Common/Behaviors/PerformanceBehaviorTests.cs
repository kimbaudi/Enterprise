using Enterprise.Application.Common.Behaviors;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;

namespace Enterprise.Application.Tests.Common.Behaviors;

public class PerformanceBehaviorTests
{
    private readonly Mock<ILogger<PerformanceBehavior<TestCommand, string>>> _loggerMock;

    public PerformanceBehaviorTests()
    {
        _loggerMock = new Mock<ILogger<PerformanceBehavior<TestCommand, string>>>();
    }

    public class TestCommand : IRequest<string>
    {
        public string Name { get; set; } = string.Empty;
    }

    [Fact]
    public async Task Handle_FastRequest_ShouldNotLogWarning()
    {
        // Arrange
        var behavior = new PerformanceBehavior<TestCommand, string>(_loggerMock.Object);
        var request = new TestCommand { Name = "Test" };

        Task<string> Next(CancellationToken ct)
        {
            return Task.FromResult("Success");
        }

        // Act
        var result = await behavior.Handle(request, Next, CancellationToken.None);

        // Assert
        result.Should().Be("Success");

        // Verify no warning was logged
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_SlowRequest_ShouldLogWarning()
    {
        // Arrange
        var behavior = new PerformanceBehavior<TestCommand, string>(_loggerMock.Object);
        var request = new TestCommand { Name = "Test" };

        async Task<string> Next(CancellationToken ct)
        {
            await Task.Delay(550, ct); // Delay over 500ms threshold
            return "Success";
        }

        // Act
        var result = await behavior.Handle(request, Next, CancellationToken.None);

        // Assert
        result.Should().Be("Success");

        // Verify warning was logged with request name
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Long Running Request") && v.ToString()!.Contains("TestCommand")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_SlowRequest_ShouldLogElapsedMilliseconds()
    {
        // Arrange
        var behavior = new PerformanceBehavior<TestCommand, string>(_loggerMock.Object);
        var request = new TestCommand { Name = "Test" };

        async Task<string> Next(CancellationToken ct)
        {
            await Task.Delay(550, ct);
            return "Success";
        }

        // Act
        await behavior.Handle(request, Next, CancellationToken.None);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("milliseconds")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_RequestUnderThreshold_ShouldNotLogWarning()
    {
        // Arrange
        var behavior = new PerformanceBehavior<TestCommand, string>(_loggerMock.Object);
        var request = new TestCommand { Name = "Test" };

        async Task<string> Next(CancellationToken ct)
        {
            await Task.Delay(100, ct); // Well under 500ms threshold
            return "Success";
        }

        // Act
        var result = await behavior.Handle(request, Next, CancellationToken.None);

        // Assert
        result.Should().Be("Success");

        // Verify no warning was logged (threshold is > 500ms)
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ExceptionThrown_ShouldPropagateException()
    {
        // Arrange
        var behavior = new PerformanceBehavior<TestCommand, string>(_loggerMock.Object);
        var request = new TestCommand { Name = "Test" };

        Task<string> Next(CancellationToken ct)
        {
            throw new InvalidOperationException("Test exception");
        }

        // Act
        var act = async () => await behavior.Handle(request, Next, CancellationToken.None);

        // Assert - Exception should propagate (PerformanceBehavior doesn't catch exceptions)
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Test exception");
    }
}
