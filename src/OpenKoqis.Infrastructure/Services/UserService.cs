using System.Linq.Expressions;
using System.Security.Authentication;
using Microsoft.Extensions.Logging;
using OpenKoqis.Application.GenericRepository;
using OpenKoqis.Application.Services;
using OpenKoqis.Domain.Models;
using OpenKoqis.Domain.Models.Dto;

namespace OpenKoqis.Infrastructure.Services;

public class UserService(
    IRepository<User> repository,
    IJwtService jwtService,
    IPasswordHasher passwordHasher,
    ILogger<UserService> logger)
    : IUserService
{
    public async Task<List<User>> GetAllAsync()
    {
        logger.LogInformation("Fetching all users from repository");
        var users = await repository.GetAllAsync();
        logger.LogInformation("Successfully retrieved {Count} users", users.Count);
        return users;
    }

    public async Task<User?> GetByIdAsync(string id)
    {
        logger.LogInformation("Searching for user with ID: {UserId}", id);
        var user = await repository.FindById(id);

        if (user == null)
            logger.LogWarning("User with ID: {UserId} not found", id);
        else
            logger.LogInformation("User {UserId} found", id);

        return user;
    }

    public async Task<User> CreateAsync(User user)
    {
        logger.LogInformation("Creating new user with Nickname: {Nickname}", user.Nickname);
        user.CreatedAt = DateTime.UtcNow;
        user.UpdatedAt = user.CreatedAt;

        repository.InsertOne(user);
        logger.LogInformation("User {Nickname} inserted into database", user.Nickname);

        return user;
    }

    public async Task UpdateAsync(string id, User user)
    {
        logger.LogInformation("Updating user with ID: {UserId}", id);
        var existing = await repository.FindById(id);

        if (existing == null)
        {
            logger.LogError("Update failed. User '{UserId}' not found", id);
            throw new KeyNotFoundException($"User '{id}' not found.");
        }

        user.Id = existing.Id;
        user.CreatedAt = existing.CreatedAt;
        user.UpdatedAt = DateTime.UtcNow;

        repository.ReplaceOne(user);
        logger.LogInformation("User {UserId} successfully updated", id);
    }

    public async Task DeleteAsync(string id)
    {
        logger.LogInformation("Attempting to delete user with ID: {UserId}", id);
        var existing = await repository.FindById(id);

        if (existing == null)
        {
            logger.LogError("Delete failed. User '{UserId}' not found", id);
            throw new KeyNotFoundException($"User '{id}' not found.");
        }

        repository.DeleteById(id);
        logger.LogInformation("User {UserId} deleted from database", id);
    }

    public async Task<TokenPair> RegisterAsync(UserRegistrationDto registrationDto)
    {
        logger.LogInformation("Starting registration process for Nickname: {Nickname}", registrationDto.Nickname);

        Expression<Func<User, bool>> filter = u => u.Nickname == registrationDto.Nickname;
        var existingUser = await repository.FindOne(filter);

        if (existingUser != null)
        {
            logger.LogWarning("Registration failed. Nickname {Nickname} is already taken", registrationDto.Nickname);
            throw new InvalidOperationException($"User with nickname '{registrationDto.Nickname}' already exists.");
        }

        logger.LogDebug("Hashing password for user {Nickname}", registrationDto.Nickname);
        string hashedPassword = passwordHasher.HashPassword(registrationDto.Password);

        var newUser = new User
        {
            Nickname = registrationDto.Nickname,
            FullName = registrationDto.FullName,
            Role = GuestRole.Instance,
            PasswordHash = hashedPassword,
            PasswordRecreationRequired = false,
            PasswordLastChangedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        repository.InsertOne(newUser);
        logger.LogInformation("User {Nickname} registered and saved with ID: {UserId}", newUser.Nickname, newUser.Id);

        logger.LogDebug("Generating JWT token pair for new user {Nickname}", newUser.Nickname);
        return await jwtService.GenerateTokenPairAsync(
            newUser.Id.ToString(),
            newUser.Nickname,
            newUser.Role
        );
    }

    public async Task<TokenPair> LoginAsync(string nickname, string password)
    {
        logger.LogInformation("Login attempt for Nickname: {Nickname}", nickname);

        Expression<Func<User, bool>> filter = u => u.Nickname == nickname;
        var user = await repository.FindOne(filter);

        if (user == null)
        {
            logger.LogWarning("Login failed. User {Nickname} not found", nickname);
            throw new AuthenticationException("Invalid nickname or password.");
        }

        logger.LogDebug("Verifying password for user {Nickname}", nickname);

        if (!passwordHasher.VerifyPassword(password, user.PasswordHash))
        {
            logger.LogWarning("Login failed. Incorrect password for user {Nickname}", nickname);
            throw new AuthenticationException("Invalid nickname or password.");
        }

        logger.LogInformation("User {Nickname} logged in successfully", nickname);
        return await jwtService.GenerateTokenPairAsync(
            user.Id.ToString(),
            user.Nickname,
            user.Role
        );
    }
}
