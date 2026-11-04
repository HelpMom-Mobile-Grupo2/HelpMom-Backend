using System.Security.Claims;
using BackendMoviles.Application.Common;
using BackendMoviles.Application.Companion;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackendMoviles.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Partner")]
public sealed class CompanionController(IMediator mediator) : ControllerBase
{
    [HttpGet("summary")]
    [ProducesResponseType(typeof(CompanionSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var partnerUserId))
        {
            return Unauthorized();
        }

        return Ok(await mediator.Send(new GetCompanionSummaryQuery(partnerUserId), cancellationToken));
    }

    [HttpGet("tips/today")]
    [ProducesResponseType(typeof(IReadOnlyCollection<DailyTipDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> GetTodayTips(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var partnerUserId))
        {
            return Unauthorized();
        }

        return Ok(await mediator.Send(new GetTodayTipsQuery(partnerUserId), cancellationToken));
    }

    [HttpPost("tips/{id:guid}/done")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkTipDone(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var partnerUserId))
        {
            return Unauthorized();
        }

        await mediator.Send(new MarkTipDoneCommand(partnerUserId, id), cancellationToken);
        return NoContent();
    }

    private bool TryGetUserId(out Guid userId)
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(value, out userId);
    }
}
