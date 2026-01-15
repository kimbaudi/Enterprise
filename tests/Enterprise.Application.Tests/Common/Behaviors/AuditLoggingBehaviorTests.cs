using Enterprise.Application.Common.Behaviors;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Domain.Entities;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;

namespace Enterprise.Application.Tests.Common.Behaviors;

public class AuditLoggingBehaviorTests
{
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<IAuditLogQueue> _auditLogQueueMock;
    private readonly Mock<ILogger<AuditLoggingBehavior<TestCommand, TestResponse>>> _loggerMock;

    public AuditLoggingBehaviorTests()
    {
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _auditLogQueueMock = new Mock<IAuditLogQueue>();
        _loggerMock = new Mock<ILogger<AuditLoggingBehavior<TestCommand, TestResponse>>>();

        _currentUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid().ToString());
        _currentUserServiceMock.Setup(x => x.Username).Returns("testuser");
        _currentUserServiceMock.Setup(x => x.IpAddress).Returns("127.0.0.1");
    }

    public class TestCommand : IRequest<TestResponse>
    {
        public string Name { get; set; } = string.Empty;
    }

    public class TestQuery : IRequest<TestResponse>
    {
        public string Name { get; set; } = string.Empty;
    }

    public class SimpleCommand : IRequest<string>
    {
        public string Name { get; set; } = string.Empty;
    }

    public class TestResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    [Fact]
    public async Task Handle_Command_ShouldEnqueueAuditLog()
    {
        // Arrange
        _auditLogQueueMock.Setup(x => x.EnqueueAsync(It.IsAny<AuditLog>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var behavior = new AuditLoggingBehavior<TestCommand, TestResponse>(
            _currentUserServiceMock.Object,
            _auditLogQueueMock.Object,
            _loggerMock.Object);

        var request = new TestCommand { Name = "Test" };
        var response = new TestResponse { Id = Guid.NewGuid(), Name = "Test" };

        Task<TestResponse> Next(CancellationToken ct) => Task.FromResult(response);

        // Act
        var result = await behavior.Handle(request, Next, CancellationToken.None);

        // Assert
        result.Should().Be(response);

        _auditLogQueueMock.Verify(
            x => x.EnqueueAsync(
                It.Is<AuditLog>(a =>
                    a.Username == "testuser" &&
                    a.IpAddress == "127.0.0.1" &&
                    a.Action == "Execute" &&
                    a.EntityName == "Test"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_Query_ShouldNotEnqueueAuditLog()
    {
        // Arrange
        var behavior = new AuditLoggingBehavior<TestQuery, TestResponse>(
            _currentUserServiceMock.Object,
            _auditLogQueueMock.Object,
            Mock.Of<ILogger<AuditLoggingBehavior<TestQuery, TestResponse>>>());

        var request = new TestQuery { Name = "Test" };
        var response = new TestResponse { Id = Guid.NewGuid(), Name = "Test" };

        Task<TestResponse> Next(CancellationToken ct) => Task.FromResult(response);

        // Act
        var result = await behavior.Handle(request, Next, CancellationToken.None);

        // Assert
        result.Should().Be(response);

        // Verify audit log was NOT enqueued for queries
        _auditLogQueueMock.Verify(
            x => x.EnqueueAsync(It.IsAny<AuditLog>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_CreateCommand_ShouldSetActionToCreate()
    {
        // Arrange
        _auditLogQueueMock.Setup(x => x.EnqueueAsync(It.IsAny<AuditLog>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var behavior = new AuditLoggingBehavior<CreateProductCommand, TestResponse>(
            _currentUserServiceMock.Object,
            _auditLogQueueMock.Object,
            Mock.Of<ILogger<AuditLoggingBehavior<CreateProductCommand, TestResponse>>>());

        var request = new CreateProductCommand { Name = "Test Product" };
        var response = new TestResponse { Id = Guid.NewGuid(), Name = "Test Product" };

        Task<TestResponse> Next(CancellationToken ct) => Task.FromResult(response);

        // Act
        await behavior.Handle(request, Next, CancellationToken.None);

        // Assert
        _auditLogQueueMock.Verify(
            x => x.EnqueueAsync(
                It.Is<AuditLog>(a => a.Action == "Create" && a.EntityName == "Product"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_UpdateCommand_ShouldSetActionToUpdate()
    {
        // Arrange
        _auditLogQueueMock.Setup(x => x.EnqueueAsync(It.IsAny<AuditLog>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var behavior = new AuditLoggingBehavior<UpdateUserCommand, TestResponse>(
            _currentUserServiceMock.Object,
            _auditLogQueueMock.Object,
            Mock.Of<ILogger<AuditLoggingBehavior<UpdateUserCommand, TestResponse>>>());

        var request = new UpdateUserCommand { Name = "Updated User" };
        var response = new TestResponse { Id = Guid.NewGuid(), Name = "Updated User" };

        Task<TestResponse> Next(CancellationToken ct) => Task.FromResult(response);

        // Act
        await behavior.Handle(request, Next, CancellationToken.None);

        // Assert
        _auditLogQueueMock.Verify(
            x => x.EnqueueAsync(
                It.Is<AuditLog>(a => a.Action == "Update" && a.EntityName == "User"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_DeleteCommand_ShouldSetActionToDelete()
    {
        // Arrange
        _auditLogQueueMock.Setup(x => x.EnqueueAsync(It.IsAny<AuditLog>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var behavior = new AuditLoggingBehavior<DeleteProductCommand, TestResponse>(
            _currentUserServiceMock.Object,
            _auditLogQueueMock.Object,
            Mock.Of<ILogger<AuditLoggingBehavior<DeleteProductCommand, TestResponse>>>());

        var request = new DeleteProductCommand { Id = Guid.NewGuid() };
        var response = new TestResponse { Id = Guid.NewGuid(), Name = "Deleted" };

        Task<TestResponse> Next(CancellationToken ct) => Task.FromResult(response);

        // Act
        await behavior.Handle(request, Next, CancellationToken.None);

        // Assert
        _auditLogQueueMock.Verify(
            x => x.EnqueueAsync(
                It.Is<AuditLog>(a => a.Action == "Delete" && a.EntityName == "Product"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_AuditLogCommand_ShouldNotCreateInfiniteLoop()
    {
        // Arrange
        var behavior = new AuditLoggingBehavior<CreateAuditLogCommand, TestResponse>(
            _currentUserServiceMock.Object,
            _auditLogQueueMock.Object,
            Mock.Of<ILogger<AuditLoggingBehavior<CreateAuditLogCommand, TestResponse>>>());

        var request = new CreateAuditLogCommand { Action = "Test" };
        var response = new TestResponse { Id = Guid.NewGuid(), Name = "Test" };

        Task<TestResponse> Next(CancellationToken ct) => Task.FromResult(response);

        // Act
        await behavior.Handle(request, Next, CancellationToken.None);

        // Assert - Should NOT enqueue audit log to prevent infinite loop
        _auditLogQueueMock.Verify(
            x => x.EnqueueAsync(It.IsAny<AuditLog>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_EnqueueFails_ShouldLogWarningButNotThrow()
    {
        // Arrange
        _auditLogQueueMock.Setup(x => x.EnqueueAsync(It.IsAny<AuditLog>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false); // Simulate queue full

        var behavior = new AuditLoggingBehavior<TestCommand, TestResponse>(
            _currentUserServiceMock.Object,
            _auditLogQueueMock.Object,
            _loggerMock.Object);

        var request = new TestCommand { Name = "Test" };
        var response = new TestResponse { Id = Guid.NewGuid(), Name = "Test" };

        Task<TestResponse> Next(CancellationToken ct) => Task.FromResult(response);

        // Act
        var result = await behavior.Handle(request, Next, CancellationToken.None);

        // Assert
        result.Should().Be(response);

        // Verify warning was logged
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to enqueue audit log")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_EnqueueThrowsException_ShouldLogErrorButNotThrow()
    {
        // Arrange
        _auditLogQueueMock.Setup(x => x.EnqueueAsync(It.IsAny<AuditLog>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Queue error"));

        var behavior = new AuditLoggingBehavior<TestCommand, TestResponse>(
            _currentUserServiceMock.Object,
            _auditLogQueueMock.Object,
            _loggerMock.Object);

        var request = new TestCommand { Name = "Test" };
        var response = new TestResponse { Id = Guid.NewGuid(), Name = "Test" };

        Task<TestResponse> Next(CancellationToken ct) => Task.FromResult(response);

        // Act
        var result = await behavior.Handle(request, Next, CancellationToken.None);

        // Assert
        result.Should().Be(response); // Request should still succeed

        // Verify error was logged
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error enqueueing audit log")),
                It.IsAny<InvalidOperationException>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldSerializeRequestAndResponse()
    {
        // Arrange
        AuditLog? capturedAuditLog = null;
        _auditLogQueueMock.Setup(x => x.EnqueueAsync(It.IsAny<AuditLog>(), It.IsAny<CancellationToken>()))
            .Callback<AuditLog, CancellationToken>((log, ct) => capturedAuditLog = log)
            .ReturnsAsync(true);

        var behavior = new AuditLoggingBehavior<TestCommand, TestResponse>(
            _currentUserServiceMock.Object,
            _auditLogQueueMock.Object,
            _loggerMock.Object);

        var request = new TestCommand { Name = "Test Request" };
        var response = new TestResponse { Id = Guid.NewGuid(), Name = "Test Response" };

        Task<TestResponse> Next(CancellationToken ct) => Task.FromResult(response);

        // Act
        await behavior.Handle(request, Next, CancellationToken.None);

        // Assert
        capturedAuditLog.Should().NotBeNull();
        capturedAuditLog!.OldValues.Should().Contain("Test Request");
        capturedAuditLog.NewValues.Should().Contain("Test Response");
    }

    [Fact]
    public async Task Handle_AnonymousUser_ShouldSetUsernameToAnonymous()
    {
        // Arrange
        _currentUserServiceMock.Setup(x => x.Username).Returns((string?)null);
        _auditLogQueueMock.Setup(x => x.EnqueueAsync(It.IsAny<AuditLog>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var behavior = new AuditLoggingBehavior<TestCommand, TestResponse>(
            _currentUserServiceMock.Object,
            _auditLogQueueMock.Object,
            _loggerMock.Object);

        var request = new TestCommand { Name = "Test" };
        var response = new TestResponse { Id = Guid.NewGuid(), Name = "Test" };

        Task<TestResponse> Next(CancellationToken ct) => Task.FromResult(response);

        // Act
        await behavior.Handle(request, Next, CancellationToken.None);

        // Assert
        _auditLogQueueMock.Verify(
            x => x.EnqueueAsync(
                It.Is<AuditLog>(a => a.Username == "Anonymous"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ResponseWithoutIdProperty_ShouldHandleGracefully()
    {
        // Arrange
        _auditLogQueueMock.Setup(x => x.EnqueueAsync(It.IsAny<AuditLog>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var behavior = new AuditLoggingBehavior<SimpleCommand, string>(
            _currentUserServiceMock.Object,
            _auditLogQueueMock.Object,
            Mock.Of<ILogger<AuditLoggingBehavior<SimpleCommand, string>>>());

        var request = new SimpleCommand { Name = "Test" };
        var response = "Simple string response"; // No Id property

        Task<string> Next(CancellationToken ct) => Task.FromResult(response);

        // Act
        await behavior.Handle(request, Next, CancellationToken.None);

        // Assert - Should not throw, just log with null EntityId
        _auditLogQueueMock.Verify(
            x => x.EnqueueAsync(
                It.Is<AuditLog>(a => a.EntityId == null),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ExtractEntityIdFromResponse_ShouldFindIdProperty()
    {
        // Arrange
        var expectedId = Guid.NewGuid();
        _auditLogQueueMock.Setup(x => x.EnqueueAsync(It.IsAny<AuditLog>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var behavior = new AuditLoggingBehavior<TestCommand, TestResponse>(
            _currentUserServiceMock.Object,
            _auditLogQueueMock.Object,
            _loggerMock.Object);

        var request = new TestCommand { Name = "Test" };
        var response = new TestResponse { Id = expectedId, Name = "Test" };

        Task<TestResponse> Next(CancellationToken ct) => Task.FromResult(response);

        // Act
        await behavior.Handle(request, Next, CancellationToken.None);

        // Assert
        _auditLogQueueMock.Verify(
            x => x.EnqueueAsync(
                It.Is<AuditLog>(a => a.EntityId == expectedId.ToString()),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ImportCommand_ShouldSetActionToImport()
    {
        // Arrange
        _auditLogQueueMock.Setup(x => x.EnqueueAsync(It.IsAny<AuditLog>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var behavior = new AuditLoggingBehavior<ImportDataCommand, TestResponse>(
            _currentUserServiceMock.Object,
            _auditLogQueueMock.Object,
            Mock.Of<ILogger<AuditLoggingBehavior<ImportDataCommand, TestResponse>>>());

        var request = new ImportDataCommand { FileName = "test.csv" };
        var response = new TestResponse { Id = Guid.NewGuid(), Name = "Imported" };

        Task<TestResponse> Next(CancellationToken ct) => Task.FromResult(response);

        // Act
        await behavior.Handle(request, Next, CancellationToken.None);

        // Assert
        _auditLogQueueMock.Verify(
            x => x.EnqueueAsync(
                It.Is<AuditLog>(a => a.Action == "Import" && a.EntityName == "Data"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldIncludeTimestamp()
    {
        // Arrange
        var beforeTimestamp = DateTime.UtcNow;
        AuditLog? capturedAuditLog = null;

        _auditLogQueueMock.Setup(x => x.EnqueueAsync(It.IsAny<AuditLog>(), It.IsAny<CancellationToken>()))
            .Callback<AuditLog, CancellationToken>((log, ct) => capturedAuditLog = log)
            .ReturnsAsync(true);

        var behavior = new AuditLoggingBehavior<TestCommand, TestResponse>(
            _currentUserServiceMock.Object,
            _auditLogQueueMock.Object,
            _loggerMock.Object);

        var request = new TestCommand { Name = "Test" };
        var response = new TestResponse { Id = Guid.NewGuid(), Name = "Test" };

        Task<TestResponse> Next(CancellationToken ct) => Task.FromResult(response);

        // Act
        await behavior.Handle(request, Next, CancellationToken.None);
        var afterTimestamp = DateTime.UtcNow;

        // Assert
        capturedAuditLog.Should().NotBeNull();
        capturedAuditLog!.Timestamp.Should().BeOnOrAfter(beforeTimestamp);
        capturedAuditLog.Timestamp.Should().BeOnOrBefore(afterTimestamp);
    }

    // Test command classes for action detection
    public class CreateProductCommand : IRequest<TestResponse>
    {
        public string Name { get; set; } = string.Empty;
    }

    public class UpdateUserCommand : IRequest<TestResponse>
    {
        public string Name { get; set; } = string.Empty;
    }

    public class DeleteProductCommand : IRequest<TestResponse>
    {
        public Guid Id { get; set; }
    }

    public class CreateAuditLogCommand : IRequest<TestResponse>
    {
        public string Action { get; set; } = string.Empty;
    }

    public class ImportDataCommand : IRequest<TestResponse>
    {
        public string FileName { get; set; } = string.Empty;
    }
}
