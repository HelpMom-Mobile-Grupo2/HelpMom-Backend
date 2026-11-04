using System.Security.Claims;
using BackendMoviles.Application.Common;
using BackendMoviles.Application.Family;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackendMoviles.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class FamilyController(IMediator mediator) : ControllerBase
{
    [HttpPost("invitations")]
    [Authorize(Roles = "Mother")]
    [ProducesResponseType(typeof(FamilyInvitationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateInvitation(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var motherUserId))
        {
            return Unauthorized();
        }

        var result = await mediator.Send(new CreateFamilyInvitationCommand(motherUserId), cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPost("join")]
    [Authorize(Roles = "Partner")]
    [ProducesResponseType(typeof(JoinFamilyDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Join(JoinFamilyRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var partnerUserId))
        {
            return Unauthorized();
        }

        return Ok(await mediator.Send(new JoinFamilyCommand(partnerUserId, request.Code), cancellationToken));
    }

    [HttpDelete("partner")]
    [Authorize(Roles = "Mother")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemovePartner(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var motherUserId))
        {
            return Unauthorized();
        }

        await mediator.Send(new RemovePartnerCommand(motherUserId), cancellationToken);
        return NoContent();
    }

    [HttpGet]
    [Authorize(Roles = "Mother,Partner")]
    [ProducesResponseType(typeof(FamilyGroupDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        return Ok(await mediator.Send(new GetFamilyQuery(userId), cancellationToken));
    }

    private bool TryGetUserId(out Guid userId)
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(value, out userId);
    }
}

public sealed record JoinFamilyRequest(string Code);
