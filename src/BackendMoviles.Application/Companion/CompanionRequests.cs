using BackendMoviles.Application.Common;
using BackendMoviles.Domain.Identity;
using BackendMoviles.Domain.Pregnancy;
using BackendMoviles.Domain.Telemetry;
using FluentValidation;
using MediatR;

namespace BackendMoviles.Application.Companion;

internal static class CompanionContext
{
    public const string NotLinkedMessage = "Aún no estás vinculado a una gestante.";

    public static async Task<FamilyGroup> GetLinkedFamilyAsync(
        this IFamilyGroupRepository families,
        Guid partnerUserId,
        CancellationToken cancellationToken) =>
        await families.GetByMemberAsync(partnerUserId, FamilyRole.Partner, cancellationToken)
            ?? throw new InvalidOperationException(NotLinkedMessage);

    // Uses the most recent pregnancy; null when there is none or its LMP is still in the future.
    public static async Task<GestationalAge?> GetCurrentGestationalAgeAsync(
        this IPregnancyRepository pregnancies,
        Guid motherUserId,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        var pregnancy = (await pregnancies.GetByMotherAsync(motherUserId, cancellationToken)).FirstOrDefault();
        return pregnancy is null || pregnancy.LastMenstrualPeriod > today ? null : pregnancy.GetGestationalAge(today);
    }
}

public sealed record GetCompanionSummaryQuery(Guid PartnerUserId) : IRequest<CompanionSummaryDto>;

public sealed class GetCompanionSummaryValidator : AbstractValidator<GetCompanionSummaryQuery>
{
    public GetCompanionSummaryValidator()
    {
        RuleFor(request => request.PartnerUserId).NotEmpty();
    }
}

public sealed class GetCompanionSummaryHandler(
    IFamilyGroupRepository families,
    IUserRepository users,
    IPregnancyRepository pregnancies,
    IHealthTelemetryRepository telemetry)
    : IRequestHandler<GetCompanionSummaryQuery, CompanionSummaryDto>
{
    public async Task<CompanionSummaryDto> Handle(GetCompanionSummaryQuery request, CancellationToken cancellationToken)
    {
        var family = await families.GetLinkedFamilyAsync(request.PartnerUserId, cancellationToken);
        var motherUserId = family.GetMotherUserId();
        var mother = await users.GetByIdAsync(motherUserId, cancellationToken)
            ?? throw new KeyNotFoundException("No se encontró a la gestante del grupo.");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var age = await pregnancies.GetCurrentGestationalAgeAsync(motherUserId, today, cancellationToken);
        var latest = (await telemetry.GetRecentByMotherAsync(motherUserId, 1, cancellationToken)).FirstOrDefault();
        var semaphore = latest?.Semaphore;

        return new CompanionSummaryDto(
            mother.FullName,
            age?.Weeks,
            age?.Days,
            semaphore?.Light,
            semaphore?.Summary,
            latest?.MeasuredAt);
    }
}
