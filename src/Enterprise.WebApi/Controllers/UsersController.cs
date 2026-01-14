using Asp.Versioning;
using Enterprise.Application.Common.Models;
using Enterprise.Application.DTOs;
using Enterprise.Application.Features.Users.Commands.CreateUser;
using Enterprise.Application.Features.Users.Commands.DeleteUser;
using Enterprise.Application.Features.Users.Commands.UpdateUser;
using Enterprise.Application.Features.Users.Queries.GetUserById;
using Enterprise.Application.Features.Users.Queries.GetUsersPaginated;
using Enterprise.WebApi.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Enterprise.WebApi.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
[EnableRateLimiting("perUser")]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Create a new user
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting("expensive")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<UserDto>>> CreateUser(
        [FromBody] CreateUserCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetUserById), new { id = result.Id }, new ApiResponse<UserDto>(result));
    }

    /// <summary>
    /// Update an existing user
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<UserDto>>> UpdateUser(
        Guid id,
        [FromBody] UpdateUserCommand command,
        CancellationToken cancellationToken)
    {
        if (id != command.Id)
        {
            return BadRequest(new ApiResponse<UserDto>("ID mismatch"));
        }

        var result = await _mediator.Send(command, cancellationToken);
        return Ok(new ApiResponse<UserDto>(result));
    }

    /// <summary>
    /// Get paginated list of users
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<UserDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PaginatedResult<UserDto>>>> GetUsers(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? searchTerm = null,
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetUsersPaginatedQuery(pageNumber, pageSize, searchTerm, isActive);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(new ApiResponse<PaginatedResult<UserDto>>(result));
    }

    /// <summary>
    /// Get user by ID
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetUserById(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetUserByIdQuery(id);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(new ApiResponse<UserDto>(result));
    }

    /// <summary>
    /// Delete a user (soft delete)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteUser(Guid id, CancellationToken cancellationToken)
    {
        var command = new DeleteUserCommand(id);
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(new ApiResponse<bool>(result));
    }
}
