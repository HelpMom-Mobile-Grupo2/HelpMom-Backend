using BackendMoviles.Application.Common;
using BackendMoviles.Domain.Common;
using BackendMoviles.Domain.Companion;
using BackendMoviles.Domain.Identity;
using FluentValidation;
using MediatR;

namespace BackendMoviles.Application.Companion;

public sealed record CreateQuickAlertCommand(Guid MotherUserId, QuickAlertType Type, string? Message = null)
    : IRequest<QuickAlertDto>;

public sealed class CreateQuickAlertValidator : AbstractValidator<CreateQuickAlertCommand>
{
    public CreateQuickAlertValidator()
    {
        RuleFor(request => request.MotherUserId).NotEmpty();
        RuleFor(request => request.Type).IsInEnum();
        RuleFor(request => request.Message).MaximumLength(QuickAlert.MaxMessageLength);
    }
}

public sealed class CreateQuickAlertHandler(
    IFamilyGroupRepository families,
    IQuickAlertRepository alerts,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateQuickAlertCommand, QuickAlertDto>
{
    public async Task<QuickAlertDto> Handle(CreateQuickAlertCommand request, CancellationToken cancellationToken)
    {
        var family = await families.GetByMemberAsync(request.MotherUserId, FamilyRole.Mother, cancellationToken)
            ?? throw new KeyNotFoundException("No se encontró tu grupo familiar.");
        if (family.GetPartner() is null)
        {
            throw new InvalidOperationException("Necesitas vincular a tu acompañante para enviar alertas.");
        }

        var alert = QuickAlert.Create(family.Id, request.MotherUserId, request.Type, request.Message, DateTimeOffset.UtcNow);
        await alerts.AddAsync(alert, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return alert.ToDto();
    }
}

public sealed record GetQuickAlertsQuery(Guid UserId, string? Status = null) : IRequest<IReadOnlyCollection<QuickAlertDto>>
{
    public const string Pending = "pending";
    public const string All = "all";

    public bool PendingOnly => string.Equals(Status?.Trim(), Pending, StringComparison.OrdinalIgnoreCase);
}

public sealed class GetQuickAlertsValidator : AbstractValidator<GetQuickAlertsQuery>
{
    public GetQuickAlertsValidator()
    {
        RuleFor(request => request.UserId).NotEmpty();
        RuleFor(request => request.Status)
            .Must(status => string.IsNullOrWhiteSpace(status)
                || string.Equals(status.Trim(), GetQuickAlertsQuery.Pending, StringComparison.OrdinalIgnoreCase)
                || string.Equals(status.Trim(), GetQuickAlertsQuery.All, StringComparison.OrdinalIgnoreCase))
            .WithMessage("El estado debe ser pending o all.");
    }
}

public sealed class GetQuickAlertsHandler(
    IUserRepository users,
    IFamilyGroupRepository families,
    IQuickAlertRepository alerts)
    : IRequestHandler<GetQuickAlertsQuery, IReadOnlyCollection<QuickAlertDto>>
{
    public async Task<IReadOnlyCollection<QuickAlertDto>> Handle(
        GetQuickAlertsQuery request,
        CancellationToken cancellationToken)
    {
        var user = await users.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new KeyNotFoundException("No se encontró el usuario.");
        var family = user.Role == FamilyRole.Partner
            ? await families.GetLinkedFamilyAsync(user.Id, cancellationToken)
            : await families.GetByMemberAsync(user.Id, user.Role, cancellationToken)
                ?? throw new KeyNotFoundException("No se encontró tu grupo familiar.");

        var items = await alerts.GetByFamilyAsync(family.Id, request.PendingOnly, cancellationToken);
        return items.Select(alert => alert.ToDto()).ToArray();
    }
}

public sealed record RespondToQuickAlertCommand(Guid PartnerUserId, Guid AlertId, QuickAlertResponse Response)
    : IRequest<QuickAlertDto>;

public sealed class RespondToQuickAlertValidator : AbstractValidator<RespondToQuickAlertCommand>
{
    public RespondToQuickAlertValidator()
    {
        RuleFor(request => request.PartnerUserId).NotEmpty();
        RuleFor(request => request.AlertId).NotEmpty();
        RuleFor(request => request.Response).IsInEnum();
    }
}

public sealed class RespondToQuickAlertHandler(
    IFamilyGroupRepository families,
    IQuickAlertRepository alerts,
    IUnitOfWork unitOfWork)
    : IRequestHandler<RespondToQuickAlertCommand, QuickAlertDto>
{
    public async Task<QuickAlertDto> Handle(RespondToQuickAlertCommand request, CancellationToken cancellationToken)
    {
        var family = await families.GetLinkedFamilyAsync(request.PartnerUserId, cancellationToken);
        var alert = await alerts.GetByIdAsync(request.AlertId, cancellationToken);
        if (alert is null || alert.FamilyGroupId != family.Id)
        {
            throw new KeyNotFoundException("No se encontró la alerta.");
        }

        if (alert.Status == QuickAlertStatus.Responded)
        {
            throw new InvalidOperationException("La alerta ya fue respondida.");
        }

        alert.Respond(request.PartnerUserId, request.Response, DateTimeOffset.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return alert.ToDto();
    }
}
