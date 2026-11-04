using System.Security.Claims;
using BackendMoviles.Application.Telemetry;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackendMoviles.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Mother")]
public sealed class TelemetryController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddSimulatedMeasurement(
        AddTelemetryRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var motherUserId))
        {
            return Unauthorized();
        }

        var result = await mediator.Send(
            new AddTelemetryCommand(
                motherUserId,
                request.FetalHeartRateBpm,
                request.TemperatureCelsius,
                request.MeasuredAt),
            cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpGet("status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetHealthStatus(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var motherUserId))
        {
            return Unauthorized();
        }

        var status = await mediator.Send(new GetHealthStatusQuery(motherUserId), cancellationToken);
        return status is null ? NotFound(new { message = "Aún no hay mediciones de salud." }) : Ok(status);
    }

    [HttpGet("history")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetHistory([FromQuery] int count = 20, CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var motherUserId))
        {
            return Unauthorized();
        }

        return Ok(await mediator.Send(new GetTelemetryHistoryQuery(motherUserId, count), cancellationToken));
    }

    private bool TryGetUserId(out Guid userId)
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(value, out userId);
    }
}

public sealed record AddTelemetryRequest(
    int FetalHeartRateBpm,
    decimal TemperatureCelsius,
    DateTimeOffset? MeasuredAt = null);