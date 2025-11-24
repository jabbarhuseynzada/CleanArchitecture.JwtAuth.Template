using WebTemplate.Domain.Entities;

namespace WebTemplate.Domain.Interfaces.RepositoryContracts;

public interface IRoleRepository
{
    Task<Role?> GetRoleByNameAsync(string roleName);
    Task<List<Role>> GetAllRolesAsync();
}
