using FluentAssertions;
using NetArchTest.Rules;

namespace Enterprise.Architecture.Tests;

/// <summary>
/// Architecture fitness tests to enforce Clean Architecture dependency rules.
/// These tests prevent architectural drift and ensure layers remain properly isolated.
/// </summary>
public class ArchitectureTests
{
    private const string DomainNamespace = "Enterprise.Domain";
    private const string ApplicationNamespace = "Enterprise.Application";
    private const string InfrastructureNamespace = "Enterprise.Infrastructure";
    private const string WebApiNamespace = "Enterprise.WebApi";

    [Fact]
    public void Domain_Should_Not_HaveDependencyOnOtherLayers()
    {
        // Arrange
        var assembly = typeof(Domain.Entities.Product).Assembly;

        // Act
        var result = Types.InAssembly(assembly)
            .ShouldNot()
            .HaveDependencyOnAny(ApplicationNamespace, InfrastructureNamespace, WebApiNamespace)
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            "Domain layer should not depend on Application, Infrastructure, or WebApi layers. " +
            $"Violations: {string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>())}");
    }

    [Fact]
    public void Application_Should_Not_HaveDependencyOnInfrastructureOrWebApi()
    {
        // Arrange
        var assembly = typeof(Application.DependencyInjection).Assembly;

        // Act
        var result = Types.InAssembly(assembly)
            .ShouldNot()
            .HaveDependencyOnAny(InfrastructureNamespace, WebApiNamespace)
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            "Application layer should not depend on Infrastructure or WebApi layers. " +
            $"Violations: {string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>())}");
    }

    [Fact]
    public void Infrastructure_Should_Not_HaveDependencyOnWebApi()
    {
        // Arrange
        var assembly = typeof(Infrastructure.DependencyInjection).Assembly;

        // Act
        var result = Types.InAssembly(assembly)
            .ShouldNot()
            .HaveDependencyOn(WebApiNamespace)
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            "Infrastructure layer should not depend on WebApi layer. " +
            $"Violations: {string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>())}");
    }

    [Fact]
    public void Handlers_Should_Be_InApplicationLayer()
    {
        // Arrange
        var assembly = typeof(Application.DependencyInjection).Assembly;

        // Act
        var result = Types.InAssembly(assembly)
            .That()
            .ImplementInterface(typeof(MediatR.IRequestHandler<,>))
            .Should()
            .ResideInNamespace(ApplicationNamespace)
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            "All MediatR handlers should reside in Application layer. " +
            $"Violations: {string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>())}");
    }

    [Fact]
    public void Commands_Should_EndWith_Command_Suffix()
    {
        // Arrange
        var assembly = typeof(Application.DependencyInjection).Assembly;

        // Act - Only check types that implement IRequest (actual commands)
        var result = Types.InAssembly(assembly)
            .That()
            .ImplementInterface(typeof(MediatR.IRequest<>))
            .And()
            .ResideInNamespace($"{ApplicationNamespace}.Features")
            .And()
            .HaveNameMatching(".*Command.*") // Must have Command in the name
            .Should()
            .HaveNameEndingWith("Command")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            "All command types (IRequest implementers) should end with 'Command' suffix. " +
            $"Violations: {string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>())}");
    }

    [Fact]
    public void Queries_Should_EndWith_Query_Suffix()
    {
        // Arrange
        var assembly = typeof(Application.DependencyInjection).Assembly;

        // Act - Only check types that implement IRequest (actual queries)
        var result = Types.InAssembly(assembly)
            .That()
            .ImplementInterface(typeof(MediatR.IRequest<>))
            .And()
            .ResideInNamespace($"{ApplicationNamespace}.Features")
            .And()
            .HaveNameMatching(".*Query.*") // Must have Query in the name
            .Should()
            .HaveNameEndingWith("Query")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            "All query types (IRequest implementers) should end with 'Query' suffix. " +
            $"Violations: {string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>())}");
    }

    [Fact]
    public void Controllers_Should_EndWith_Controller_Suffix()
    {
        // Arrange
        var assembly = typeof(Enterprise.WebApi.Controllers.ProductsController).Assembly;

        // Act
        var result = Types.InAssembly(assembly)
            .That()
            .Inherit(typeof(Microsoft.AspNetCore.Mvc.ControllerBase))
            .Should()
            .HaveNameEndingWith("Controller")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            "All controllers should end with 'Controller' suffix. " +
            $"Violations: {string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>())}");
    }

    [Fact]
    public void Controllers_Should_ResideInControllersNamespace()
    {
        // Arrange
        var assembly = typeof(Enterprise.WebApi.Controllers.ProductsController).Assembly;

        // Act
        var result = Types.InAssembly(assembly)
            .That()
            .Inherit(typeof(Microsoft.AspNetCore.Mvc.ControllerBase))
            .Should()
            .ResideInNamespace($"{WebApiNamespace}.Controllers")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            "All controllers should reside in Controllers namespace. " +
            $"Violations: {string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>())}");
    }

    [Fact]
    public void Entities_Should_InheritFromBaseEntity()
    {
        // Arrange
        var assembly = typeof(Domain.Entities.Product).Assembly;

        // Act
        var result = Types.InAssembly(assembly)
            .That()
            .ResideInNamespace($"{DomainNamespace}.Entities")
            .And()
            .AreClasses()
            .And()
            .AreNotAbstract()
            .Should()
            .Inherit(typeof(Domain.Common.BaseEntity))
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            "All entities should inherit from BaseEntity. " +
            $"Violations: {string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>())}");
    }

    [Fact]
    public void Repositories_Should_ImplementIRepository()
    {
        // Arrange
        var assembly = typeof(Infrastructure.DependencyInjection).Assembly;

        // Act
        var result = Types.InAssembly(assembly)
            .That()
            .ResideInNamespace($"{InfrastructureNamespace}.Repositories")
            .And()
            .HaveNameEndingWith("Repository")
            .And()
            .AreClasses()
            .And()
            .AreNotAbstract()
            .Should()
            .ImplementInterface(typeof(Application.Common.Interfaces.IRepository<>))
            .Or()
            .ImplementInterface(typeof(Application.Common.Interfaces.IUserRepository))
            .Or()
            .ImplementInterface(typeof(Application.Common.Interfaces.IRefreshTokenRepository))
            .Or()
            .ImplementInterface(typeof(Application.Common.Interfaces.IAuditLogRepository))
            .Or()
            .ImplementInterface(typeof(Application.Common.Interfaces.IUnitOfWork))
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            "All repository classes should implement an IRepository interface. " +
            $"Violations: {string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>())}");
    }

    [Fact]
    public void Validators_Should_EndWith_Validator_Suffix()
    {
        // Arrange
        var assembly = typeof(Application.DependencyInjection).Assembly;

        // Act
        var result = Types.InAssembly(assembly)
            .That()
            .Inherit(typeof(FluentValidation.AbstractValidator<>))
            .Should()
            .HaveNameEndingWith("Validator")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            "All FluentValidation validators should end with 'Validator' suffix. " +
            $"Violations: {string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>())}");
    }

    [Fact]
    public void Behaviors_Should_ImplementIPipelineBehavior()
    {
        // Arrange
        var assembly = typeof(Application.DependencyInjection).Assembly;

        // Act
        var result = Types.InAssembly(assembly)
            .That()
            .ResideInNamespace($"{ApplicationNamespace}.Common.Behaviors")
            .And()
            .HaveNameEndingWith("Behavior")
            .Should()
            .ImplementInterface(typeof(MediatR.IPipelineBehavior<,>))
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            "All behavior classes should implement IPipelineBehavior. " +
            $"Violations: {string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>())}");
    }

    [Fact]
    public void Services_Should_EndWith_Service_Suffix()
    {
        // Arrange
        var assembly = typeof(Infrastructure.DependencyInjection).Assembly;

        // Act
        var result = Types.InAssembly(assembly)
            .That()
            .ResideInNamespace($"{InfrastructureNamespace}.Services")
            .And()
            .AreClasses()
            .And()
            .DoNotHaveName("PasswordHasher") // Exception: PasswordHasher is a valid service name
            .Should()
            .HaveNameEndingWith("Service")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            "All service classes should end with 'Service' suffix (except PasswordHasher). " +
            $"Violations: {string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>())}");
    }

    [Fact]
    public void Domain_Should_NotHaveDependencyOnEntityFramework()
    {
        // Arrange
        var assembly = typeof(Domain.Entities.Product).Assembly;

        // Act
        var result = Types.InAssembly(assembly)
            .ShouldNot()
            .HaveDependencyOn("Microsoft.EntityFrameworkCore")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            "Domain layer should not depend on Entity Framework. " +
            $"Violations: {string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>())}");
    }
}
