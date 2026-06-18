using System;
using System.Collections.Generic;
using System.Text;
using OpenKoqis.Domain.Models;

namespace OpenKoqis.Application.Services
{
    // В OpenKoqis.Application/Abstractions/Security/
    public interface IJwtService
    {
        // Возвращает пару: обычный токен и рефреш-токен
        Task<TokenPair> GenerateTokenPairAsync(string userId, string userName, UserRole role);

        // Валидирует рефреш-токен
        Task<bool> IsRefreshTokenValidAsync(string userId, string refreshToken);

        // Удаляет рефреш-токен после использования (или при выходе)
        Task RemoveRefreshTokenAsync(string userId, string refreshToken);
    }

    public record TokenPair(string AccessToken, string RefreshToken);
}
