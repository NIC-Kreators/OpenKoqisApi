using Microsoft.Extensions.Logging;
using OpenKoqis.Application.Services;

namespace OpenKoqis.Infrastructure.Services;

public class BCryptPasswordHasher(ILogger<BCryptPasswordHasher> logger) : IPasswordHasher
{
    public string HashPassword(string password)
    {
        logger.LogInformation("Starting password hashing process...");

        try
        {
            var hash = BCrypt.Net.BCrypt.HashPassword(password, 10);

            logger.LogInformation("Password successfully hashed");
            return hash;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while hashing password");
            throw;
        }
    }

    public bool VerifyPassword(string providedPassword, string hashedPassword)
    {
        logger.LogInformation("Starting password verification...");

        try
        {
            var isValid = BCrypt.Net.BCrypt.Verify(providedPassword, hashedPassword);

            if (isValid)
            {
                logger.LogInformation("Password verification successful. Access granted");
            }
            else
            {
                logger.LogWarning("Password verification failed. Invalid credentials provided");
            }

            return isValid;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred during password verification");
            return false;
        }
    }
}
