using System.Linq.Expressions;
using System.Security.Authentication;
using Microsoft.Extensions.Logging;
using OpenKoqis.Application.GenericRepository;
using OpenKoqis.Application.Services;
using OpenKoqis.Domain.Models;
using OpenKoqis.Domain.Models.Dto;

namespace OpenKoqis.Infrastructure.Services
{
    public class UserService : IUserService
    {
        private readonly IRepository<User> _repository;
        private readonly IJwtService _jwtService;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ILogger<UserService> _logger;

        public UserService(
            IRepository<User> repository,
            IJwtService jwtService,
            IPasswordHasher passwordHasher,
            ILogger<UserService> logger)
        {
            _repository = repository;
            _jwtService = jwtService;
            _passwordHasher = passwordHasher;
            _logger = logger;

            _logger.LogInformation("UserService initialized.");
        }

        public async Task<List<User>> GetAllAsync()
        {
            _logger.LogInformation("Fetching all users from repository.");
            var users = await _repository.GetAllAsync();
            _logger.LogInformation("Successfully retrieved {Count} users.", users.Count);
            return users;
        }

        public async Task<User?> GetByIdAsync(string id)
        {
            _logger.LogInformation("Searching for user with ID: {UserId}", id);
            var user = await _repository.FindById(id);

            if (user == null)
                _logger.LogWarning("User with ID: {UserId} not found.", id);
            else
                _logger.LogInformation("User {UserId} found.", id);

            return user;
        }

        public async Task<User> CreateAsync(User user)
        {
            _logger.LogInformation("Creating new user with Nickname: {Nickname}", user.Nickname);
            user.CreatedAt = DateTime.UtcNow;
            user.UpdatedAt = user.CreatedAt;

            _repository.InsertOne(user);
            _logger.LogInformation("User {Nickname} inserted into database.", user.Nickname);

            return await Task.FromResult(user);
        }

        public async Task UpdateAsync(string id, User user)
        {
            _logger.LogInformation("Updating user with ID: {UserId}", id);
            var existing = await _repository.FindById(id);

            if (existing == null)
            {
                _logger.LogError("Update failed. User '{UserId}' not found.", id);
                throw new KeyNotFoundException($"User '{id}' not found.");
            }

            user.Id = existing.Id;
            user.CreatedAt = existing.CreatedAt;
            user.UpdatedAt = DateTime.UtcNow;

            _repository.ReplaceOne(user);
            _logger.LogInformation("User {UserId} successfully updated.", id);
            await Task.CompletedTask;
        }

        public async Task DeleteAsync(string id)
        {
            _logger.LogInformation("Attempting to delete user with ID: {UserId}", id);
            var existing = await _repository.FindById(id);

            if (existing == null)
            {
                _logger.LogError("Delete failed. User '{UserId}' not found.", id);
                throw new KeyNotFoundException($"User '{id}' not found.");
            }

            _repository.DeleteById(id);
            _logger.LogInformation("User {UserId} deleted from database.", id);
            await Task.CompletedTask;
        }

        public async Task<TokenPair> RegisterAsync(UserRegistrationDto registrationDto)
        {
            _logger.LogInformation("Starting registration process for Nickname: {Nickname}", registrationDto.Nickname);

            Expression<Func<User, bool>> filter = u => u.Nickname == registrationDto.Nickname;
            var existingUser = await _repository.FindOne(filter);

            if (existingUser != null)
            {
                _logger.LogWarning("Registration failed. Nickname {Nickname} is already taken.", registrationDto.Nickname);
                throw new InvalidOperationException($"User with nickname '{registrationDto.Nickname}' already exists.");
            }

            _logger.LogDebug("Hashing password for user {Nickname}.", registrationDto.Nickname);
            string hashedPassword = _passwordHasher.HashPassword(registrationDto.Password);

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

            _repository.InsertOne(newUser);
            _logger.LogInformation("User {Nickname} registered and saved with ID: {UserId}", newUser.Nickname, newUser.Id);

            _logger.LogDebug("Generating JWT token pair for new user {Nickname}.", newUser.Nickname);
            return await _jwtService.GenerateTokenPairAsync(
                newUser.Id.ToString(),
                newUser.Nickname,
                newUser.Role
            );
        }

        public async Task<TokenPair> LoginAsync(string nickname, string password)
        {
            _logger.LogInformation("Login attempt for Nickname: {Nickname}", nickname);

            Expression<Func<User, bool>> filter = u => u.Nickname == nickname;
            var user = await _repository.FindOne(filter);

            if (user == null)
            {
                _logger.LogWarning("Login failed. User {Nickname} not found.", nickname);
                throw new AuthenticationException("Invalid nickname or password.");
            }

            _logger.LogDebug("Verifying password for user {Nickname}.", nickname);
            if (!_passwordHasher.VerifyPassword(password, user.PasswordHash))
            {
                _logger.LogWarning("Login failed. Incorrect password for user {Nickname}.", nickname);
                throw new AuthenticationException("Invalid nickname or password.");
            }

            _logger.LogInformation("User {Nickname} logged in successfully.", nickname);
            return await _jwtService.GenerateTokenPairAsync(
                user.Id.ToString(),
                user.Nickname,
                user.Role
            );
        }
    }
}
