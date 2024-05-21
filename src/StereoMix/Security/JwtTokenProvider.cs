using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace StereoMix.Security;

public interface IJwtTokenProvider
{
    string AuthenticateUser(string userId, string userName);
    string AuthenticateGameServer(string id);
    Task<TokenValidationResult> ValidateTokenAsync(string token);
}

public class JwtTokenProvider : IJwtTokenProvider
{
    private const string JwtSecretKeyName = "JWT_SECRET";
    private const string JwtIssuerKeyName = "StereoMix:JWT:Issuer";
    private const string ClientAudienceKeyName = "StereoMix:JWT:Audience:Client";
    private const string GameServerAudienceKeyName = "StereoMix:JWT:Audience:GameServer";

    private readonly string _clientAudience;
    private readonly SigningCredentials _credentials;
    private readonly string _gameServerAudience;
    private readonly string _jwtIssuer;

    private readonly ILogger<JwtTokenProvider> _logger;

    public JwtTokenProvider(IConfiguration configuration, ILogger<JwtTokenProvider> logger)
    {
        _logger = logger;

        _clientAudience = configuration[ClientAudienceKeyName] ?? throw new KeyNotFoundException($"{ClientAudienceKeyName} is not set in appsettings.json");
        _gameServerAudience = configuration[GameServerAudienceKeyName] ?? throw new KeyNotFoundException($"{GameServerAudienceKeyName} is not set in appsettings.json");
        _jwtIssuer = configuration[JwtIssuerKeyName] ?? throw new KeyNotFoundException($"{JwtIssuerKeyName} is not set in appsettings.json");

        var secret = configuration[JwtSecretKeyName] ?? throw new KeyNotFoundException($"{JwtSecretKeyName} is not set in Environment Variables");
        _credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secret)), SecurityAlgorithms.HmacSha256Signature);
    }

    public string AuthenticateUser(string userId, string userName)
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Role, StereoMixRole.UserRole),
            new Claim(StereoMixClaimTypes.UserName, userName),
            new Claim(StereoMixClaimTypes.UserId, userId)
        });

        return Authenticate(identity, _clientAudience);
    }

    public string AuthenticateGameServer(string id)
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Role, StereoMixRole.GameServerRole),
            new Claim(StereoMixClaimTypes.RoomId, id)
        });
        return Authenticate(identity, _gameServerAudience);
    }

    public async Task<TokenValidationResult> ValidateTokenAsync(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        return await tokenHandler.ValidateTokenAsync(token, new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = _credentials.Key,
            ValidateIssuer = true,
            ValidIssuer = _jwtIssuer,
            ValidateAudience = true,
            ValidAudiences = new[] { _clientAudience, _gameServerAudience },
            ValidateLifetime = true
        }).ConfigureAwait(false);
    }

    private string Authenticate(ClaimsIdentity identity, string audience)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Issuer = _jwtIssuer,
            Audience = audience,
            Subject = identity,
            SigningCredentials = _credentials,
            Expires = DateTime.UtcNow.AddMinutes(360)
        };

        // debug descriptor
        _logger.LogDebug("Issuer: {Issuer}", tokenDescriptor.Issuer);
        _logger.LogDebug("Audience: {Audience}", tokenDescriptor.Audience);
        _logger.LogDebug("Subject: {Subject}", tokenDescriptor.Subject);
        _logger.LogDebug("SigningCredentials: {SigningCredentials}", tokenDescriptor.SigningCredentials);
        _logger.LogDebug("Expires: {Expires}", tokenDescriptor.Expires);

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}
