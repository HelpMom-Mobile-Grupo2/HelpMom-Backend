using BackendMoviles.Application.Common;
using BackendMoviles.Application.Identity;
using BackendMoviles.Domain.Common;
using BackendMoviles.Domain.Identity;
using FluentValidation;
using MediatR;

namespace BackendMoviles.Application.Family;

public sealed record CreateFamilyInvitationCommand(Guid MotherUserId) : IRequest<FamilyInvitationDto>;

public sealed class CreateFamilyInvitationValidator : AbstractValidator<CreateFamilyInvitationCommand>
{
    public CreateFamilyInvitationValidator()
    {
        RuleFor(request => request.MotherUserId).NotEmpty();
    }
}

public sealed class CreateFamilyInvitationHandler(
    IFamilyGroupRepository families,
    IFamilyInvitationRepository invitations,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateFamilyInvitationCommand, FamilyInvitationDto>
{
    private const int MaxCodeAttempts = 20;

    public async Task<FamilyInvitationDto> Handle(
        CreateFamilyInvitationCommand request,
        CancellationToken cancellationToken)
    {
        var family = await families.GetByMemberAsync(request.MotherUserId, FamilyRole.Mother, cancellationToken)
            ?? throw new KeyNotFoundException("No se encontró tu grupo familiar.");

        var now = DateTimeOffset.UtcNow;
        foreach (var previous in await invitations.GetActiveByFamilyAsync(family.Id, now, cancellationToken))
        {
            previous.Invalidate(now);
        }

        var invitation = FamilyInvitation.Create(family.Id, await GenerateUniqueCodeAsync(now, cancellationToken), now);
        await invitations.AddAsync(invitation, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new FamilyInvitationDto(invitation.Code, invitation.ExpiresAt);
    }

    // Codes only need to be unique among active invitations; expired or used ones can be reused.
    private async Task<string> GenerateUniqueCodeAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < MaxCodeAttempts; attempt++)
        {
            var code = FamilyInvitation.GenerateCode();
            if (!await invitations.ActiveCodeExistsAsync(code, now, cancellationToken))
            {
                return code;
            }
        }

        throw new InvalidOperationException("No se pudo generar un código de invitación. Intenta de nuevo.");
    }
}

public sealed record JoinFamilyCommand(Guid PartnerUserId, string Code) : IRequest<JoinFamilyDto>;

public sealed class JoinFamilyValidator : AbstractValidator<JoinFamilyCommand>
{
    public JoinFamilyValidator()
    {
        RuleFor(request => request.PartnerUserId).NotEmpty();
        RuleFor(request => request.Code)
            .Must(code => FamilyInvitation.IsValidCode(FamilyInvitation.NormalizeCode(code)))
            .WithMessage("El código debe tener el formato HM-0000.");
    }
}

public sealed class JoinFamilyHandler(
    IFamilyInvitationRepository invitations,
    IFamilyGroupRepository families,
    IUserRepository users,
    ISender sender)
    : IRequestHandler<JoinFamilyCommand, JoinFamilyDto>
{
    public async Task<JoinFamilyDto> Handle(JoinFamilyCommand request, CancellationToken cancellationToken)
    {
        var invitation = await invitations.GetLatestByCodeAsync(FamilyInvitation.NormalizeCode(request.Code), cancellationToken)
            ?? throw new KeyNotFoundException("El código de invitación no existe.");

        var now = DateTimeOffset.UtcNow;
        if (invitation.IsUsed)
        {
            throw new InvalidOperationException("El código de invitación ya fue usado.");
        }

        if (invitation.IsExpired(now))
        {
            throw new InvalidOperationException("El código de invitación venció. Pide uno nuevo.");
        }

        if (await families.GetByMemberAsync(request.PartnerUserId, FamilyRole.Partner, cancellationToken) is not null)
        {
            throw new InvalidOperationException("Ya estás vinculado a una gestante.");
        }

        var family = await families.GetByIdAsync(invitation.FamilyGroupId, cancellationToken)
            ?? throw new KeyNotFoundException("No se encontró el grupo familiar.");
        if (family.Members.Any(member => member.Role == FamilyRole.Partner))
        {
            throw new InvalidOperationException("Esta gestante ya tiene un acompañante vinculado.");
        }

        var motherUserId = family.Members.Single(member => member.Role == FamilyRole.Mother).UserId;
        var mother = await users.GetByIdAsync(motherUserId, cancellationToken)
            ?? throw new KeyNotFoundException("No se encontró a la gestante del grupo.");

        // The invitation shares the scoped unit of work, so AddFamilyMember persists both changes in one save.
        invitation.MarkUsed(request.PartnerUserId, now);
        await sender.Send(new AddFamilyMemberCommand(family.Id, request.PartnerUserId, FamilyRole.Partner), cancellationToken);
        return new JoinFamilyDto(family.Id, mother.FullName);
    }
}

public sealed record RemovePartnerCommand(Guid MotherUserId) : IRequest;

public sealed class RemovePartnerValidator : AbstractValidator<RemovePartnerCommand>
{
    public RemovePartnerValidator()
    {
        RuleFor(request => request.MotherUserId).NotEmpty();
    }
}

public sealed class RemovePartnerHandler(IFamilyGroupRepository families, IUnitOfWork unitOfWork)
    : IRequestHandler<RemovePartnerCommand>
{
    public async Task Handle(RemovePartnerCommand request, CancellationToken cancellationToken)
    {
        var family = await families.GetByMemberAsync(request.MotherUserId, FamilyRole.Mother, cancellationToken)
            ?? throw new KeyNotFoundException("No se encontró tu grupo familiar.");
        var partner = family.Members.FirstOrDefault(member => member.Role == FamilyRole.Partner)
            ?? throw new KeyNotFoundException("No tienes un acompañante vinculado.");

        family.RemoveMember(partner.UserId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

public sealed record GetFamilyQuery(Guid UserId) : IRequest<FamilyGroupDto>;

public sealed class GetFamilyHandler(IUserRepository users, IFamilyGroupRepository families)
    : IRequestHandler<GetFamilyQuery, FamilyGroupDto>
{
    public async Task<FamilyGroupDto> Handle(GetFamilyQuery request, CancellationToken cancellationToken)
    {
        var user = await users.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new KeyNotFoundException("No se encontró el usuario.");
        var family = await families.GetByMemberAsync(user.Id, user.Role, cancellationToken)
            ?? throw new KeyNotFoundException("Aún no perteneces a un grupo familiar.");

        var members = await users.GetByIdsAsync(family.Members.Select(member => member.UserId), cancellationToken);
        return family.ToDto(members);
    }
}
