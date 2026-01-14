using Asp.Versioning;
using Enterprise.Application.Common.Models;
using Enterprise.Application.Features.Users.Commands.CreateUser;
using Enterprise.Application.Features.Users.Commands.DeleteUser;
using Enterprise.Application.Features.Users.Commands.UpdateUser;
using Enterprise.Application.Features.Users.Queries.GetUserById;
using Enterprise.Application.Features.Users.Queries.GetUsersPaginated;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Enterprise.WebApi.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize(Roles = "Admin")]
[EnableRateLimiting("perUser")]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [ResponseCache(Duration = 30, Location = ResponseCacheLocation.Any, VaryByQueryKeys = new[] { "pageNumber", "pageSize", "searchTerm", "sortBy" })]
    public async Task<ActionResult<ApiResponse<PaginatedResult<UserDto>>>> GetUsers(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? sortBy = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetUsersPaginatedQuery(pageNumber, pageSize, searchTerm, sortBy);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(new ApiResponse<PaginatedResult<UserDto>>(result));
    }

    [HttpGet("{id}")]
    [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any, VaryByQueryKeys = new[] { "id" })]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetUserById(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetUserByIdQuery(id);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(new ApiResponse<UserDto>(result));
    }

    [HttpPost]
    [EnableRateLimiting("expensive")]
    public async Task<ActionResult<ApiResponse<UserDto>>> CreateUser(
        [FromBody] CreateUserCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetUserById), new { id = result.Id }, new ApiResponse<UserDto>(result));
    }

    [HttpPut("{id}")]
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

    [HttpDelete("{id}")]
    [EnableRateLimiting("expensive")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteUser(Guid id, CancellationToken cancellationToken)
    {
        var command = new DeleteUserCommand(id);
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(new ApiResponse<bool>(result));
    }
}