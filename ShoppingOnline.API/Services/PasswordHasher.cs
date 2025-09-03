using BCrypt.Net;

namespace ShoppingOnline.API.Services
{
    public class PasswordHasher : IPasswordHasher
    {
        private readonly IConfiguration _configuration;
        private readonly int _workFactor;

        public PasswordHasher(IConfiguration configuration)
        {
            _configuration = configuration;
            // Get work factor from config, default to 4 for development
            _workFactor = _configuration.GetValue<int>("Security:BcryptWorkFactor", 4);
        }

        public string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password, _workFactor);
        }

        public bool VerifyPassword(string hashedPassword, string password)
        {
            try
            {
                return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
            }
            catch
            {
                return false;
            }
        }
    }
}