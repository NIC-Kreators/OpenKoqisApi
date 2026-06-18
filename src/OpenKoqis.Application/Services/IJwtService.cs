using System;
using System.Collections.Generic;
using System.Text;
using OpenKoqis.Domain.Models;

namespace OpenKoqis.Application.Services
{
    // В OpenKoqis.Application/Abstractions/Security/
    public interface IJwtService
    {
        // Normal and refresh token
        Task<TokenPair> GenerateTokenPairAsync(string userId, string userName, UserRole role);

        // Validating refresh token
        Task<bool> IsRefreshTokenValidAsync(string userId, string refreshToken);

        // Deleteting refresh token
        Task RemoveRefreshTokenAsync(string userId, string refreshToken);
    }

    public record TokenPair(string AccessToken, string RefreshToken);
}
