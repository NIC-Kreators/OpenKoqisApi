using OpenKoqis.Domain.Models;
using OpenKoqis.Domain.Models.Dto;

namespace OpenKoqis.Application.Services;

public interface IUserService
{
    Task<List<User>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<User?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<User> CreateAsync(User user, CancellationToken cancellationToken = default);
    Task UpdateAsync(string id, User user, CancellationToken cancellationToken = default);
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);

    Task<TokenPair> RegisterAsync(UserRegistrationDto registrationDto, CancellationToken cancellationToken = default);
    Task<TokenPair> LoginAsync(string email, string password, CancellationToken cancellationToken = default);
}
