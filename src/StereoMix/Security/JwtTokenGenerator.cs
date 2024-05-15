using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace StereoMix.Security;

public interface IJwtConfiguration
{
    string Secret { get; }
    string Issuer { get; }
    string Audience { get; }
}

public sealed class JwtConfiguration : IJwtConfiguration
{
    [ConfigurationKeyName("SECRET")] public required string Secret { get; set; }

    [ConfigurationKeyName("ISSUER")] public required string Issuer { get; set; }

    [ConfigurationKeyName("AUDIENCE")] public required string Audience { get; set; }

    public override string ToString()
    {
        return $"JWT Configuration: Issuer={Issuer}, Audience={Audience}";
    }
}

public interface IJwtTokenGenrerator
{
    string AuthenticateTemporary(string userName);
}

public class JwtTokenGenerator : IJwtTokenGenrerator
{
    public const string JwtSecretKeyName = "JWT_SECRET";

    public string AuthenticateTemporary(string userName)
    {
        var randomId = Guid.NewGuid().ToString();
        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, userName), new Claim(ClaimTypes.NameIdentifier, randomId) });
        return AuthenticateUser(identity);
    }

    private static string AuthenticateUser(ClaimsIdentity identity)
    {
        identity.AddClaim(new Claim(ClaimTypes.Role, "User"));
        return Authenticate(identity);
    }

    private static string Authenticate(ClaimsIdentity identity)
    {
        var key = Environment.GetEnvironmentVariable(JwtSecretKeyName) ?? throw new InvalidOperationException("JWT_SECRET is not set");
        var keyBytes = Encoding.ASCII.GetBytes(key);
        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Issuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? throw new InvalidOperationException("JWT_ISSUER is not set"),
            Audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? throw new InvalidOperationException("JWT_AUDIENCE is not set"),
            Subject = identity,
            Expires = DateTime.UtcNow.AddMinutes(60),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}
