using BackendMoviles.Application.Common;
using BackendMoviles.Domain.Identity;
using FluentValidation;
using MediatR;

namespace BackendMoviles.Application.Identity;

public sealed record RegisterUserCommand(string Email, string FullName, string Password) : IRequest<AuthResponse>;

public sealed class RegisterUserValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserValidator()
    {
        RuleFor(request => request.Email).NotEmpty().EmailAddress().MaximumLength(254);
        RuleFor(request => request.FullName).NotEmpty().MaximumLength(160);
        RuleFor(request => request.Password).MinimumLength(10).MaximumLength(128);
    }
}

public sealed class RegisterUserHandler(
    IUserRepository users,
    IFamilyGroupRepository families,
    IPasswordService passwords,
    IAccessTokenService tokens,
    BackendMoviles.Domain.Common.IUnitOfWork unitOfWork)
    : IRequestHandler<RegisterUserCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var email = EmailAddress.Create(request.Email);
        if (await users.GetByEmailAsync(email, cancellationToken) is not null)
        {
            throw new InvalidOperationException("Ya existe una cuenta con ese correo.");
        }

        var user = User.Create(email, request.FullName, passwords.Hash(request.Password));
        var family = FamilyGroup.Create($"Familia de {user.FullName}", user.Id);
        await users.AddAsync(user, cancellationToken);
        await families.AddAsync(family, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthResponse(user.ToDto(), family.Id, FamilyRole.Mother.ToString(), tokens.CreateToken(user, FamilyRole.Mother));
    }
}

public sealed record LoginCommand(string Email, string Password) : IRequest<AuthResponse>;

public sealed class LoginValidator : AbstractValidator<LoginCommand>
{
    public LoginValidator()
    {
        RuleFor(request => request.Email).NotEmpty().EmailAddress();
        RuleFor(request => request.Password).NotEmpty();
    }
}

public sealed class LoginHandler(
    IUserRepository users,
    IFamilyGroupRepository families,
    IPasswordService passwords,
    IAccessTokenService tokens)
    : IRequestHandler<LoginCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await users.GetByEmailAsync(EmailAddress.Create(request.Email), cancellationToken);
        if (user is null || !user.IsActive || !passwords.Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Correo o contraseña incorrectos.");
        }

        var role = await families.GetRoleForUserAsync(user.Id, cancellationToken);
        return new AuthResponse(user.ToDto(), null, role?.ToString(), tokens.CreateToken(user, role));
    }
}

public sealed record AddFamilyMemberCommand(Guid FamilyGroupId, Guid UserId, FamilyRole Role) : IRequest<FamilyGroupDto>;

public sealed class AddFamilyMemberValidator : AbstractValidator<AddFamilyMemberCommand>
{
    public AddFamilyMemberValidator()
    {
        RuleFor(request => request.FamilyGroupId).NotEmpty();
        RuleFor(request => request.UserId).NotEmpty();
        RuleFor(request => request.Role).IsInEnum().NotEqual(FamilyRole.Mother);
    }
}

public sealed class AddFamilyMemberHandler(
    IFamilyGroupRepository families,
    IUserRepository users,
    BackendMoviles.Domain.Common.IUnitOfWork unitOfWork)
    : IRequestHandler<AddFamilyMemberCommand, FamilyGroupDto>
{
    public async Task<FamilyGroupDto> Handle(AddFamilyMemberCommand request, CancellationToken cancellationToken)
    {
        var family = await families.GetByIdAsync(request.FamilyGroupId, cancellationToken)
            ?? throw new KeyNotFoundException("No se encontró el grupo familiar.");
        if (await users.GetByIdAsync(request.UserId, cancellationToken) is null)
        {
            throw new KeyNotFoundException("No se encontró el usuario a invitar.");
        }

        var member = family.AddMember(request.UserId, request.Role);
        unitOfWork.Add(member);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return family.ToDto();
    }
}