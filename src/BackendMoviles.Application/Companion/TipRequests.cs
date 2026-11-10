using BackendMoviles.Application.Common;
using BackendMoviles.Domain.Common;
using BackendMoviles.Domain.Companion;
using BackendMoviles.Domain.Identity;
using BackendMoviles.Domain.Pregnancy;
using FluentValidation;
using MediatR;

namespace BackendMoviles.Application.Companion;

public sealed record GetTodayTipsQuery(Guid PartnerUserId) : IRequest<IReadOnlyCollection<DailyTipDto>>;

public sealed class GetTodayTipsValidator : AbstractValidator<GetTodayTipsQuery>
{
    public GetTodayTipsValidator()
    {
        RuleFor(request => request.PartnerUserId).NotEmpty();
    }
}

public sealed class GetTodayTipsHandler(
    IFamilyGroupRepository families,
    IPregnancyRepository pregnancies,
    IDailyTipRepository tips)
    : IRequestHandler<GetTodayTipsQuery, IReadOnlyCollection<DailyTipDto>>
{
    public const int TipsPerDay = 3;

    public async Task<IReadOnlyCollection<DailyTipDto>> Handle(
        GetTodayTipsQuery request,
        CancellationToken cancellationToken)
    {
        var family = await families.GetLinkedFamilyAsync(request.PartnerUserId, cancellationToken);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var age = await pregnancies.GetCurrentGestationalAgeAsync(family.GetMotherUserId(), today, cancellationToken);

        // Without a pregnancy the whole catalogue rotates; weeks beyond the catalogue use the last week.
        int? week = age is { } current ? Math.Clamp(current.Weeks, DailyTip.FirstWeek, DailyTip.LastWeek) : null;
        var candidates = await tips.GetForWeekAsync(week, cancellationToken);
        var completed = await tips.GetCompletedTipIdsAsync(request.PartnerUserId, today, cancellationToken);

        return SelectForDay(candidates, today)
            .Select(tip => new DailyTipDto(tip.Id, tip.Title, tip.Body, completed.Contains(tip.Id)))
            .ToArray();
    }

    // Each day starts TipsPerDay positions later, so consecutive days show different tips when possible.
    public static IReadOnlyCollection<DailyTip> SelectForDay(IReadOnlyList<DailyTip> candidates, DateOnly day)
    {
        if (candidates.Count == 0)
        {
            return [];
        }

        var start = (day.DayOfYear - 1) * TipsPerDay % candidates.Count;
        return Enumerable.Range(0, Math.Min(TipsPerDay, candidates.Count))
            .Select(offset => candidates[(start + offset) % candidates.Count])
            .ToArray();
    }
}

public sealed record MarkTipDoneCommand(Guid PartnerUserId, Guid TipId) : IRequest;

public sealed class MarkTipDoneValidator : AbstractValidator<MarkTipDoneCommand>
{
    public MarkTipDoneValidator()
    {
        RuleFor(request => request.PartnerUserId).NotEmpty();
        RuleFor(request => request.TipId).NotEmpty();
    }
}

public sealed class MarkTipDoneHandler(IDailyTipRepository tips, IUnitOfWork unitOfWork)
    : IRequestHandler<MarkTipDoneCommand>
{
    public async Task Handle(MarkTipDoneCommand request, CancellationToken cancellationToken)
    {
        if (await tips.GetByIdAsync(request.TipId, cancellationToken) is null)
        {
            throw new KeyNotFoundException("No se encontró el consejo.");
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var completed = await tips.GetCompletedTipIdsAsync(request.PartnerUserId, today, cancellationToken);
        if (completed.Contains(request.TipId))
        {
            return;
        }

        await tips.AddCompletionAsync(TipCompletion.Create(request.TipId, request.PartnerUserId, today), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

public sealed record UnmarkTipDoneCommand(Guid PartnerUserId, Guid TipId) : IRequest;

public sealed class UnmarkTipDoneValidator : AbstractValidator<UnmarkTipDoneCommand>
{
    public UnmarkTipDoneValidator()
    {
        RuleFor(request => request.PartnerUserId).NotEmpty();
        RuleFor(request => request.TipId).NotEmpty();
    }
}

// Lets the partner undo a tip marked as done by mistake today. Idempotent, like marking it.
public sealed class UnmarkTipDoneHandler(IDailyTipRepository tips, IUnitOfWork unitOfWork)
    : IRequestHandler<UnmarkTipDoneCommand>
{
    public async Task Handle(UnmarkTipDoneCommand request, CancellationToken cancellationToken)
    {
        if (await tips.GetByIdAsync(request.TipId, cancellationToken) is null)
        {
            throw new KeyNotFoundException("No se encontró el consejo.");
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        await tips.RemoveCompletionAsync(request.TipId, request.PartnerUserId, today, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
