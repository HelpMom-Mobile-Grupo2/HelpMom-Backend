using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BackendMoviles.Application.Common;
using BackendMoviles.Domain.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace BackendMoviles.Infrastructure.Security;

public sealed class JwtAccessTokenService(IOptions<JwtTokenOptions> options) : IAccessTokenService
{
    public string CreateToken(User user, FamilyRole? role)
    {
        var settings = options.Value;
        var secretBytes = Encoding.UTF8.GetBytes(settings.Secret);
        if (secretBytes.Length < 32)
        {
            throw new InvalidOperationException("JWT Secret must contain at least 32 UTF-8 bytes.");
        }

        if (settings.ExpirationMinutes is < 1 or > 1440)
        {
            throw new InvalidOperationException("JWT ExpirationMinutes must be between 1 and 1440.");
        }

        var now = DateTime.UtcNow;
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email.Value),
            new(ClaimTypes.Name, user.FullName)
        };

        if (role is not null)
        {
            claims.Add(new Claim(ClaimTypes.Role, role.Value.ToString()));
        }

        var signingKey = new SymmetricSecurityKey(secretBytes);
        var token = new JwtSecurityToken(
            settings.Issuer,
            settings.Audience,
            claims,
            now,
            now.AddMinutes(settings.ExpirationMinutes),
            new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}