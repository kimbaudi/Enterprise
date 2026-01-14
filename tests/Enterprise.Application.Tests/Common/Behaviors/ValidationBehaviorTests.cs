using Enterprise.Application.Common.Behaviors;
using FluentAssertions;
using FluentValidation;
using MediatR;
using ValidationException = Enterprise.Application.Common.Exceptions.ValidationException;

namespace Enterprise.Application.Tests.Common.Behaviors;

public class ValidationBehaviorTests
{
    public class TestCommand : IRequest<string>
    {
        public string Name { get; set; } = string.Empty;
    }

    public class TestCommandValidator : AbstractValidator<TestCommand>
    {
        public TestCommandValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required");
        }
    }

    [Fact]
    public async Task Handle_NoValidators_ShouldContinuePipeline()
    {
        // Arrange
        var validators = new List<IValidator<TestCommand>>();
        var behavior = new ValidationBehavior<TestCommand, string>(validators);

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
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldContinuePipeline()
    {
        // Arrange
        var validators = new List<IValidator<TestCommand>>
        {
            new TestCommandValidator()
        };
        var behavior = new ValidationBehavior<TestCommand, string>(validators);

        var request = new TestCommand { Name = "Valid Name" };
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
    }

    [Fact]
    public async Task Handle_InvalidRequest_ShouldThrowValidationException()
    {
        // Arrange
        var validators = new List<IValidator<TestCommand>>
        {
            new TestCommandValidator()
        };
        var behavior = new ValidationBehavior<TestCommand, string>(validators);

        var request = new TestCommand { Name = "" }; // Invalid

        Task<string> Next(CancellationToken ct)
        {
            return Task.FromResult("Success");
        }

        // Act
        var act = async () => await behavior.Handle(request, Next, CancellationToken.None);

        // Assert
        var exception = await act.Should().ThrowAsync<ValidationException>();
        exception.Which.Errors.Should().ContainKey("Name");
        exception.Which.Errors["Name"].Should().Contain("Name is required");
    }

    [Fact]
    public async Task Handle_MultipleValidationErrors_ShouldThrowWithAllErrors()
    {
        // Arrange
        var validator = new InlineValidator<TestCommand>();
        validator.RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required");
        validator.RuleFor(x => x.Name).MinimumLength(3).WithMessage("Name must be at least 3 characters");

        var validators = new List<IValidator<TestCommand>> { validator };
        var behavior = new ValidationBehavior<TestCommand, string>(validators);

        var request = new TestCommand { Name = "" };

        Task<string> Next(CancellationToken ct) => Task.FromResult("Success");

        // Act
        var act = async () => await behavior.Handle(request, Next, CancellationToken.None);

        // Assert
        var exception = await act.Should().ThrowAsync<ValidationException>();
        exception.Which.Errors.Should().HaveCount(1); // One property with errors
        exception.Which.Errors["Name"].Should().HaveCount(2); // Two errors for the Name property
    }
}
