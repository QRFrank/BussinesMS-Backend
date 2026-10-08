using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BussinesMS.Aplicacion.Seguridad;

public class JwtSettings
{
    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpirationMinutes { get; set; }
}

public class JwtHelper
{
    private readonly JwtSettings _settings;

    public JwtHelper(JwtSettings settings)
    {
        _settings = settings;
    }

    public string GenerateToken(int usuarioId, string username, int rolId, int sistemaId)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, usuarioId.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, username),
            new Claim("rolId", rolId.ToString()),
            new Claim("sistemaId", sistemaId.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var ahora = DateTime.UtcNow;
        var cincoAM = ahora.Date.AddHours(5);
        var expiration = ahora > cincoAM ? cincoAM.AddDays(1) : cincoAM;

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: expiration,
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_settings.SecretKey);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _settings.Issuer,
                ValidateAudience = true,
                ValidAudience = _settings.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            return tokenHandler.ValidateToken(token, validationParameters, out _);
        }
        catch
        {
            return null;
        }
    }

    public int? GetUsuarioId(ClaimsPrincipal? claims)
    {
        var sub = claims?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        return int.TryParse(sub, out var id) ? id : null;
    }

    public string? GetUsername(ClaimsPrincipal? claims)
    {
        return claims?.FindFirst(JwtRegisteredClaimNames.UniqueName)?.Value;
    }

    public int? GetRolId(ClaimsPrincipal? claims)
    {
        var rolIdClaim = claims?.FindFirst("rolId")?.Value;
        return int.TryParse(rolIdClaim, out var rolId) ? rolId : null;
    }

    public int? GetSistemaId(ClaimsPrincipal? claims)
    {
        var sistemaIdClaim = claims?.FindFirst("sistemaId")?.Value;
        return int.TryParse(sistemaIdClaim, out var sistemaId) ? sistemaId : null;
    }
}