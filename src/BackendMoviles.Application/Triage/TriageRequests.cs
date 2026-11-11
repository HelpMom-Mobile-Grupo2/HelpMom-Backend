using System.Globalization;
using System.Text;
using BackendMoviles.Application.Common;
using BackendMoviles.Application.Companion;
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
    IUnitOfWork unitOfWork,
    IPublisher publisher)
    : IRequestHandler<SendTriageMessageCommand, TriageConversationDto>
{
    // Each alarm sign matches when every word of any of its patterns appears in the message.
    // Words are stems ("sangr" matches "sangrado", "sangrando"), so similar phrasings also match.
    private static readonly AlarmSign[] AlarmSigns =
    [
        new("sangrado abundante",
            ["desangr"], ["hemorrag"], ["sangr", "much"], ["sangr", "abundant"],
            ["sangr", "bastante"], ["sangr", "demasiad"], ["sangr", "no para"], ["sangr", "fuerte"],
            ["sangr", "chorro"], ["sangr", "coagulo"]),
        new("dificultad para respirar",
            ["no puedo respirar"], ["respir", "dificil"], ["respir", "cuesta"], ["respir", "dificultad"],
            ["falta", "aire"], ["me ahogo"], ["sin aire"], ["no me entra aire"]),
        new("convulsiones",
            ["convuls"], ["ataque", "temblor"]),
        new("desmayo o pérdida de conciencia",
            ["desmay"], ["perdi el conocimiento"], ["perdi la conciencia"], ["me voy a caer", "mare"]),
        new("dolor muy intenso",
            ["dolor", "intens"], ["dolor", "fuerte"], ["dolor", "insoportable"], ["dolor", "terrible"],
            ["dolor", "horrible"], ["dolor", "much"], ["dolor", "demasiad"], ["duele", "much"],
            ["duele", "demasiad"], ["duele", "horrible"], ["duele", "fuerte"], ["no aguanto", "dolor"]),
        new("el bebé se mueve menos",
            ["se mueve menos"], ["no se mueve"], ["no siento al bebe"], ["no siento a mi bebe"],
            ["no siento", "patad"], ["no siento", "bebe"], ["no lo siento mover"], ["no la siento mover"],
            ["dejo de moverse"], ["ya no se mueve"]),
        new("posible ruptura de fuente",
            ["rompi fuente"], ["rompio", "fuente"], ["rompi la fuente"], ["sale", "liquido"],
            ["perdida", "liquido"], ["se me rompio"], ["moje", "liquido"]),
        new("visión borrosa o dolor de cabeza fuerte",
            ["vision borrosa"], ["veo borroso"], ["veo lucecitas"], ["veo puntos"], ["cabeza", "fuerte"],
            ["cabeza", "much"], ["cabeza", "intens"], ["cabeza", "insoportable"]),
        new("fiebre muy alta",
            ["fiebre", "alta"], ["fiebre", "muy"], ["fiebre", "39"], ["fiebre", "40"], ["temperatura", "alta"]),
    ];

    private static readonly string[] CareTeamTerms = Normalize(
    [
        "fiebre", "dolor", "duele", "sangr", "contracci", "mancha", "vomit", "hinchaz", "hinchad",
        "ardor", "flujo", "presion", "colico", "calambre"
    ]);

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
        var sign = DetectAlarmSign(request.Content);
        var assessment = Assess(request.Content, sign, now);
        conversation.SetAssessment(assessment);
        var assistantMessage = conversation.AddMessage(MessageAuthor.Assistant, assessment.Guidance, now.AddTicks(1));
        unitOfWork.Add(assistantMessage);

        if (request.ConversationId is null)
        {
            await conversations.AddAsync(conversation, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        if (assessment.Urgency == TriageUrgency.Emergency)
        {
            await publisher.Publish(
                new EmergencyDetected(request.MotherUserId, EmergencyReasons.TriageEmergency(sign?.Label)),
                cancellationToken);
        }

        return conversation.ToDto();
    }

    // Lower-cases and strips accents so "Rompí fuente" and "rompi fuente" match the same term.
    public static string Normalize(string text)
    {
        var decomposed = text.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    private static string[] Normalize(string[] terms) => terms.Select(Normalize).ToArray();

    // Returns the first alarm sign found in the message, or null when there is none.
    public static AlarmSign? DetectAlarmSign(string content)
    {
        var normalized = Normalize(content);
        return AlarmSigns.FirstOrDefault(sign => sign.Matches(normalized));
    }

    private static TriageAssessment Assess(string content, AlarmSign? sign, DateTimeOffset assessedAt)
    {
        var normalized = Normalize(content);
        if (sign is not null)
        {
            return TriageAssessment.Create(
                TriageUrgency.Emergency,
                $"Esta simulación detectó una posible señal de alarma ({sign.Label}). Contacta ahora a los servicios de emergencia o acude a urgencias. Tu acompañante fue avisado. No esperes una respuesta de esta aplicación.",
                assessedAt);
        }

        if (CareTeamTerms.Any(term => normalized.Contains(term, StringComparison.Ordinal)))
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

public sealed record GetTriageConversationsQuery(Guid MotherUserId)
    : IRequest<IReadOnlyCollection<TriageConversationSummaryDto>>;

public sealed class GetTriageConversationsValidator : AbstractValidator<GetTriageConversationsQuery>
{
    public GetTriageConversationsValidator()
    {
        RuleFor(request => request.MotherUserId).NotEmpty();
    }
}

public sealed class GetTriageConversationsHandler(ITriageConversationRepository conversations)
    : IRequestHandler<GetTriageConversationsQuery, IReadOnlyCollection<TriageConversationSummaryDto>>
{
    public async Task<IReadOnlyCollection<TriageConversationSummaryDto>> Handle(
        GetTriageConversationsQuery request,
        CancellationToken cancellationToken)
    {
        var items = await conversations.GetByMotherAsync(request.MotherUserId, cancellationToken);
        return items.Select(conversation => new TriageConversationSummaryDto(
                conversation.Id,
                conversation.CreatedAt,
                conversation.LatestAssessment?.Urgency,
                conversation.Messages.MaxBy(message => message.SentAt)?.Content))
            .ToArray();
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

public sealed class AlarmSign
{
    private readonly string[][] patterns;

    public AlarmSign(string label, params string[][] patterns)
    {
        Label = label;
        this.patterns = patterns
            .Select(words => words.Select(SendTriageMessageHandler.Normalize).ToArray())
            .ToArray();
    }

    public string Label { get; }

    public bool Matches(string normalizedText) =>
        patterns.Any(words => words.All(word => normalizedText.Contains(word, StringComparison.Ordinal)));
}
