using System.Security.Claims;
using BackendMoviles.Application.Common;
using BackendMoviles.Application.Triage;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackendMoviles.Api.Controllers;

[ApiController]
[Tags("Triage")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
[Route("api/[controller]")]
[Authorize(Roles = "Mother")]
public sealed class TriageController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(TriageConversationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> StartConversation(
        SendTriageMessageRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var motherUserId))
        {
            return Unauthorized();
        }

        var result = await mediator.Send(
            new SendTriageMessageCommand(motherUserId, request.Content),
            cancellationToken);
        return CreatedAtAction(nameof(GetConversation), new { conversationId = result.Id }, result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<TriageConversationSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetConversations(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var motherUserId))
        {
            return Unauthorized();
        }

        return Ok(await mediator.Send(new GetTriageConversationsQuery(motherUserId), cancellationToken));
    }

    [HttpPost("{conversationId:guid}/messages")]
    [ProducesResponseType(typeof(TriageConversationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SendMessage(
        Guid conversationId,
        SendTriageMessageRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var motherUserId))
        {
            return Unauthorized();
        }

        var result = await mediator.Send(
            new SendTriageMessageCommand(motherUserId, request.Content, conversationId),
            cancellationToken);
        return Ok(result);
    }

    [HttpGet("{conversationId:guid}")]
    [ProducesResponseType(typeof(TriageConversationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetConversation(Guid conversationId, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var motherUserId))
        {
            return Unauthorized();
        }

        var conversation = await mediator.Send(new GetTriageConversationQuery(conversationId), cancellationToken);
        return conversation is null || conversation.MotherUserId != motherUserId ? NotFound() : Ok(conversation);
    }

    private bool TryGetUserId(out Guid userId)
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(value, out userId);
    }
}

public sealed record SendTriageMessageRequest(string Content);