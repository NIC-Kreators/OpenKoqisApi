using Microsoft.Extensions.Logging; // Добавляем этот namespace
using BCrypt.Net;
using OpenKoqis.Application.Services;

namespace OpenKoqis.Infrastructure.Services
{
    public class BCryptPasswordHasher : IPasswordHasher
    {
        private readonly ILogger<BCryptPasswordHasher> _logger;

        // Внедряем логгер через конструктор
        public BCryptPasswordHasher(ILogger<BCryptPasswordHasher> logger)
        {
            _logger = logger;
            _logger.LogDebug("BCryptPasswordHasher initialized.");
        }

        // Хеширование пароля
        public string HashPassword(string password)
        {
            _logger.LogInformation("Starting password hashing process...");

            try
            {
                var hash = BCrypt.Net.BCrypt.HashPassword(password, 10);

                _logger.LogInformation("Password successfully hashed.");
                return hash;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while hashing password.");
                throw;
            }
        }

        // Верификация пароля
        public bool VerifyPassword(string providedPassword, string hashedPassword)
        {
            _logger.LogInformation("Starting password verification...");

            try
            {
                bool isValid = BCrypt.Net.BCrypt.Verify(providedPassword, hashedPassword);

                if (isValid)
                {
                    _logger.LogInformation("Password verification successful. Access granted.");
                }
                else
                {
                    _logger.LogWarning("Password verification failed. Invalid credentials provided.");
                }

                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during password verification.");
                return false;
            }
        }
    }
}