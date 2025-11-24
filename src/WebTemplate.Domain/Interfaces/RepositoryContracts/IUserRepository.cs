using WebTemplate.Domain.Entities;

namespace WebTemplate.Domain.Interfaces.RepositoryContracts;

public interface IUserRepository
{
    Task<List<User>> GetAllUsersAsync();
    Task<User?> GetUserByIdAsync(Guid userId);
    Task<User?> GetUserByEmailAsync(string email);
    Task<User?> GetUserWithRolesByEmailAsync(string email);
    Task<bool> UserExistsByEmailAsync(string email);
    Task AddUserAsync(User user);
    Task UpdateUserAsync(User user);
    Task DeleteUserAsync(Guid userId);
}
