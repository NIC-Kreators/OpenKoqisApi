using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using OpenKoqis.Application.Services;
using OpenKoqis.Domain.Models;

namespace OpenKoqis.Infrastructure.Services
{
    public class JwtService : IJwtService
    {
        private static readonly ConcurrentDictionary<string, string> _refreshTokens = new();
        private readonly IConfiguration _configuration;
        private readonly byte[] _secretKeyBytes;
        private readonly ILogger<JwtService> _logger;

        public JwtService(IConfiguration configuration, ILogger<JwtService> logger)
        {
            _configuration = configuration;
            _logger = logger;

            _logger.LogDebug("Initializing JwtService...");

            var secret = _configuration["Jwt:Key"]
                         ?? throw new InvalidOperationException("Jwt:Key not configured.");

            _secretKeyBytes = Encoding.ASCII.GetBytes(secret);

            _logger.LogInformation("JwtService initialized with secret key from configuration.");
        }

        private string GenerateAccessToken(string userId, string userName, UserRole role)
        {
            _logger.LogDebug("Generating Access Token for user: {UserName} (ID: {UserId}) with role: {Role}", userName, userId, role.Name);

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = new SymmetricSecurityKey(_secretKeyBytes);
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
              new Claim(JwtRegisteredClaimNames.Sub, userId),
              new Claim(ClaimTypes.Name, userName),
              new Claim(ClaimTypes.Role, role.Name)
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(15),
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"],
                SigningCredentials = credentials
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var encodedToken = tokenHandler.WriteToken(token);

            _logger.LogTrace("Access Token successfully generated for ID: {UserId}", userId);
            return encodedToken;
        }

        private string GenerateRefreshToken()
        {
            var token = Guid.NewGuid().ToString("N");
            _logger.LogTrace("New Refresh Token string generated.");
            return token;
        }

        public async Task<TokenPair> GenerateTokenPairAsync(string userId, string userName, UserRole role)
        {
            _logger.LogInformation("Creating new Token Pair for user {UserId}", userId);

            var accessToken = GenerateAccessToken(userId, userName, role);
            var refreshToken = GenerateRefreshToken();

            _refreshTokens.AddOrUpdate(userId, refreshToken, (key, oldValue) =>
            {
                _logger.LogDebug("Updating existing Refresh Token in memory for user {UserId}", userId);
                return refreshToken;
            });

            _logger.LogInformation("Token pair successfully generated and stored for user {UserId}", userId);
            return new TokenPair(accessToken, refreshToken);
        }

        public async Task<bool> IsRefreshTokenValidAsync(string userId, string refreshToken)
        {
            _logger.LogDebug("Validating Refresh Token for user {UserId}", userId);

            if (_refreshTokens.TryGetValue(userId, out var storedToken))
            {
                var isValid = storedToken == refreshToken;
                if (isValid)
                {
                    _logger.LogInformation("Refresh Token is valid for user {UserId}", userId);
                }
                else
                {
                    _logger.LogWarning("Refresh Token mismatch for user {UserId}. Access denied.", userId);
                }
                return isValid;
            }

            _logger.LogWarning("No Refresh Token found in memory for user {UserId}", userId);
            return false;
        }

        public async Task RemoveRefreshTokenAsync(string userId, string refreshToken)
        {
            _logger.LogInformation("Attempting to remove Refresh Token for user {UserId}", userId);

            if (_refreshTokens.TryRemove(userId, out _))
            {
                _logger.LogInformation("Refresh Token removed successfully for user {UserId}", userId);
            }
            else
            {
                _logger.LogDebug("No Refresh Token was found to remove for user {UserId}", userId);
            }

            await Task.CompletedTask;
        }
    }
}
