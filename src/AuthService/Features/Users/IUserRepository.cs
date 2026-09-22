using AuthService.Data;
using AuthService.Models;

namespace AuthService.Features.Users;

public interface IUserRepository : IBaseRepository<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default);
    Task<User?> GetByIdWithRolesAsync(Guid id, CancellationToken ct = default);
    Task<(IReadOnlyList<User> Users, int TotalCount)> GetPagedWithRolesAsync(int page, int pageSize, CancellationToken ct = default);
}
