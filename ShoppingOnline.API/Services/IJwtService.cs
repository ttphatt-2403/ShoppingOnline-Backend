using ShoppingOnline.API.Models;

namespace ShoppingOnline.API.Services
{
    public interface IJwtService
    {
        string GenerateToken(User user, string? roleName);
    }
}