using System.Security.Claims;
using BackendMoviles.Application.Common;
using BackendMoviles.Application.Companion;
using BackendMoviles.Domain.Companion;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackendMoviles.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class AlertsController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = "Mother")]
    [ProducesResponseType(typeof(QuickAlertDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(CreateQuickAlertRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var motherUserId))
        {
            return Unauthorized();
        }

        var result = await mediator.Send(
            new CreateQuickAlertCommand(motherUserId, request.Type, request.Message),
            cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpGet]
    [Authorize(Roles = "Mother,Partner")]
    [ProducesResponseType(typeof(IReadOnlyCollection<QuickAlertDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> GetAlerts([FromQuery] string? status, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        return Ok(await mediator.Send(new GetQuickAlertsQuery(userId, status), cancellationToken));
    }

    [HttpPost("{id:guid}/respond")]
    [Authorize(Roles = "Partner")]
    [ProducesResponseType(typeof(QuickAlertDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Respond(
        Guid id,
        RespondQuickAlertRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var partnerUserId))
        {
            return Unauthorized();
        }

        return Ok(await mediator.Send(
            new RespondToQuickAlertCommand(partnerUserId, id, request.Response),
            cancellationToken));
    }

    private bool TryGetUserId(out Guid userId)
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(value, out userId);
    }
}

public sealed record CreateQuickAlertRequest(QuickAlertType Type, string? Message = null);
public sealed record RespondQuickAlertRequest(QuickAlertResponse Response);
