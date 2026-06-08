using SmartBin.Domain.Models;
using SmartBin.Domain.Models.Dto;
namespace SmartBin.Application.Services
{
    public interface IUserService
    {
        Task<List<User>> GetAllAsync();
        Task<User?> GetByIdAsync(string id);
        Task<User> CreateAsync(User user);
        Task UpdateAsync(string id, User user);
        Task DeleteAsync(string id);

        // New user registry
        Task<TokenPair> RegisterAsync(UserRegistrationDto registrationDto);

        // User login
        Task<TokenPair> LoginAsync(string email, string password);
    }
}
