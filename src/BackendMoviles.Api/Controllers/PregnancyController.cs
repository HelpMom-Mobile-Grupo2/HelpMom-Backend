using System.Security.Claims;
using BackendMoviles.Application.Pregnancy;
using BackendMoviles.Domain.Pregnancy;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackendMoviles.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class PregnancyController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(CreatePregnancyRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var motherUserId))
        {
            return Unauthorized();
        }

        var result = await mediator.Send(
            new CreatePregnancyRecordCommand(motherUserId, request.LastMenstrualPeriod, request.DueDate),
            cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var motherUserId))
        {
            return Unauthorized();
        }

        return Ok(await mediator.Send(new GetPregnancyRecordsQuery(motherUserId), cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var motherUserId))
        {
            return Unauthorized();
        }

        var pregnancy = await mediator.Send(new GetPregnancyRecordQuery(id), cancellationToken);
        return pregnancy is null || pregnancy.MotherUserId != motherUserId ? NotFound() : Ok(pregnancy);
    }

    [HttpPost("{pregnancyId:guid}/symptoms")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RecordSymptom(
        Guid pregnancyId,
        RecordSymptomRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var motherUserId))
        {
            return Unauthorized();
        }

        var pregnancy = await mediator.Send(new GetPregnancyRecordQuery(pregnancyId), cancellationToken);
        if (pregnancy is null || pregnancy.MotherUserId != motherUserId)
        {
            return NotFound();
        }

        var result = await mediator.Send(
            new RecordSymptomCommand(pregnancyId, request.Description, request.Severity, request.OccurredAt),
            cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    private bool TryGetUserId(out Guid userId)
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(value, out userId);
    }
}

public sealed record CreatePregnancyRequest(DateOnly LastMenstrualPeriod, DateOnly DueDate);
public sealed record RecordSymptomRequest(string Description, SymptomSeverity Severity, DateTimeOffset? OccurredAt = null);