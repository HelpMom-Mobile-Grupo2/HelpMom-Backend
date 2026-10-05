using BackendMoviles.Application.Common;
using BackendMoviles.Domain.Common;
using BackendMoviles.Domain.Triage;
using FluentValidation;
using MediatR;

namespace BackendMoviles.Application.Triage;

public sealed record SendTriageMessageCommand(
    Guid MotherUserId,
    string Content,
    Guid? ConversationId = null) : IRequest<TriageConversationDto>;

public sealed class SendTriageMessageValidator : AbstractValidator<SendTriageMessageCommand>
{
    public SendTriageMessageValidator()
    {
        RuleFor(request => request.MotherUserId).NotEmpty();
        RuleFor(request => request.Content).NotEmpty().MaximumLength(4000);
    }
}

public sealed class SendTriageMessageHandler(
    ITriageConversationRepository conversations,
    IUnitOfWork unitOfWork)
    : IRequestHandler<SendTriageMessageCommand, TriageConversationDto>
{
    private static readonly string[] EmergencyTerms =
    [
        "sangrado abundante", "no puedo respirar", "dificultad para respirar",
        "convulsiones", "desmayo", "dolor intenso", "se mueve menos"
    ];

    public async Task<TriageConversationDto> Handle(
        SendTriageMessageCommand request,
        CancellationToken cancellationToken)
    {
        var conversation = request.ConversationId is Guid conversationId
            ? await conversations.GetByIdAsync(conversationId, cancellationToken)
                ?? throw new KeyNotFoundException("No se encontró la conversación de triage.")
            : TriageConversation.Start(request.MotherUserId);

        if (conversation.MotherUserId != request.MotherUserId)
        {
            throw new UnauthorizedAccessException("La conversación no pertenece a esta cuenta.");
        }

        var now = DateTimeOffset.UtcNow;
        var motherMessage = conversation.AddMessage(MessageAuthor.Mother, request.Content, now);
        unitOfWork.Add(motherMessage);
        var assessment = Assess(request.Content, now);
        conversation.SetAssessment(assessment);
        var assistantMessage = conversation.AddMessage(MessageAuthor.Assistant, assessment.Guidance, now.AddTicks(1));
        unitOfWork.Add(assistantMessage);

        if (request.ConversationId is null)
        {
            await conversations.AddAsync(conversation, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return conversation.ToDto();
    }

    private static TriageAssessment Assess(string content, DateTimeOffset assessedAt)
    {
        var normalized = content.ToLowerInvariant();
        if (EmergencyTerms.Any(term => normalized.Contains(term, StringComparison.Ordinal)))
        {
            return TriageAssessment.Create(
                TriageUrgency.Emergency,
                "Esta simulación detectó una posible señal de alarma. Contacta ahora a los servicios de emergencia o acude a urgencias. No esperes una respuesta de esta aplicación.",
                assessedAt);
        }

        if (normalized.Contains("fiebre", StringComparison.Ordinal)
            || normalized.Contains("dolor", StringComparison.Ordinal)
            || normalized.Contains("sangrado", StringComparison.Ordinal)
            || normalized.Contains("contracciones", StringComparison.Ordinal))
        {
            return TriageAssessment.Create(
                TriageUrgency.ContactCareTeam,
                "La respuesta de triage es simulada. Contacta a tu equipo de salud para valorar estos síntomas; si empeoran o te preocupan, busca atención urgente.",
                assessedAt);
        }

        return TriageAssessment.Create(
            TriageUrgency.SelfCare,
            "Esta respuesta es una simulación, no un diagnóstico. Registra cómo evoluciona el síntoma y consulta a tu equipo de salud ante cualquier duda o empeoramiento.",
            assessedAt);
    }
}

public sealed record GetTriageConversationQuery(Guid Id) : IRequest<TriageConversationDto?>;

public sealed class GetTriageConversationHandler(ITriageConversationRepository conversations)
    : IRequestHandler<GetTriageConversationQuery, TriageConversationDto?>
{
    public async Task<TriageConversationDto?> Handle(
        GetTriageConversationQuery request,
        CancellationToken cancellationToken)
    {
        var conversation = await conversations.GetByIdAsync(request.Id, cancellationToken);
        return conversation?.ToDto();
    }
}