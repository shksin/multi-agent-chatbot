using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace MultiAgentSystem.Api.Services;

public interface IAuthService
{
    Task<AuthResponse?> AuthenticateAsync(string username, string password);
    bool ValidateToken(string token);
}

public class AuthService : IAuthService
{
    private readonly IConfiguration _configuration;
    private readonly Dictionary<string, string> _dummyUsers = new()
    {
        { "admin", "password123" },
        { "user1", "pass123" },
        { "demo", "demo123" }
    };

    public AuthService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<AuthResponse?> AuthenticateAsync(string username, string password)
    {
        await Task.Delay(100); // Simulate async operation

        if (!_dummyUsers.TryGetValue(username, out var storedPassword) || storedPassword != password)
        {
            return null;
        }

        var token = GenerateJwtToken(username);
        return new AuthResponse
        {
            Token = token,
            Username = username,
            ExpiresIn = 3600
        };
    }

    public bool ValidateToken(string token)
    {
        try
        {
            var jwtKey = _configuration["Jwt:Key"] ?? "ThisIsASecretKeyForDevelopmentPurposesOnly123!";
            var key = Encoding.ASCII.GetBytes(jwtKey);

            var tokenHandler = new JwtSecurityTokenHandler();
            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.Zero
            }, out _);

            return true;
        }
        catch
        {
            return false;
        }
    }

    private string GenerateJwtToken(string username)
    {
        var jwtKey = _configuration["Jwt:Key"] ?? "ThisIsASecretKeyForDevelopmentPurposesOnly123!";
        var key = Encoding.ASCII.GetBytes(jwtKey);

        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.NameIdentifier, username),
                new Claim("userId", Guid.NewGuid().ToString())
            }),
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
}
