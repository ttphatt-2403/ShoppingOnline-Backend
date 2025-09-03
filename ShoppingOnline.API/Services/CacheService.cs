using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;
using ShoppingOnline.API.Models;
using Microsoft.EntityFrameworkCore;

namespace ShoppingOnline.API.Services
{
    public interface ICacheService
    {
        Task<T?> GetAsync<T>(string key) where T : class;
        Task SetAsync<T>(string key, T value, TimeSpan? expiry = null) where T : class;
        Task RemoveAsync(string key);
    }

    public class MemoryCacheService : ICacheService
    {
        private readonly IMemoryCache _cache;
        private readonly IConfiguration _configuration;

        public MemoryCacheService(IMemoryCache cache, IConfiguration configuration)
        {
            _cache = cache;
            _configuration = configuration;
        }

        public Task<T?> GetAsync<T>(string key) where T : class
        {
            _cache.TryGetValue(key, out T? value);
            return Task.FromResult(value);
        }

        public Task SetAsync<T>(string key, T value, TimeSpan? expiry = null) where T : class
        {
            var options = new MemoryCacheEntryOptions();
            
            if (expiry.HasValue)
            {
                options.AbsoluteExpirationRelativeToNow = expiry;
            }
            else
            {
                // Default cache timeout from config
                var defaultTimeout = _configuration.GetValue<int>("Performance:CacheTimeout", 300);
                options.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(defaultTimeout);
            }

            _cache.Set(key, value, options);
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key)
        {
            _cache.Remove(key);
            return Task.CompletedTask;
        }
    }

    // Performance-optimized User Service
    public interface IUserCacheService
    {
        Task<User?> GetUserByUsernameAsync(string username);
        Task InvalidateUserCacheAsync(int userId);
        Task<Role?> GetRoleAsync(int roleId);
    }

    public class UserCacheService : IUserCacheService
    {
        private readonly ICacheService _cache;
        private readonly ShoppingDbContext _context;

        public UserCacheService(ICacheService cache, ShoppingDbContext context)
        {
            _cache = cache;
            _context = context;
        }

        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            var cacheKey = $"user:username:{username.ToLower()}";
            var cachedUser = await _cache.GetAsync<User>(cacheKey);
            
            if (cachedUser != null)
                return cachedUser;

            // Optimized query without ToLower() in database
            var user = await _context.Users
                .AsNoTracking() // Important for performance
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Username == username && u.IsActive);

            if (user != null)
            {
                await _cache.SetAsync(cacheKey, user, TimeSpan.FromMinutes(15));
            }

            return user;
        }

        public async Task InvalidateUserCacheAsync(int userId)
        {
            // Invalidate all related cache entries
            var user = await _context.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserId == userId);
            
            if (user != null)
            {
                await _cache.RemoveAsync($"user:username:{user.Username.ToLower()}");
                await _cache.RemoveAsync($"user:id:{userId}");
            }
        }

        public async Task<Role?> GetRoleAsync(int roleId)
        {
            var cacheKey = $"role:{roleId}";
            var cachedRole = await _cache.GetAsync<Role>(cacheKey);
            
            if (cachedRole != null)
                return cachedRole;

            var role = await _context.Roles
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.RoleId == roleId);

            if (role != null)
            {
                // Roles rarely change, cache for longer
                await _cache.SetAsync(cacheKey, role, TimeSpan.FromHours(1));
            }

            return role;
        }
    }
}
