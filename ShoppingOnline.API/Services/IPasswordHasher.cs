using ShoppingOnline.API.Models;

namespace ShoppingOnline.API.Services
{
    public interface IPasswordHasher
    {
        string HashPassword(string password);
        bool VerifyPassword(string hashedPassword, string password);
    }
}