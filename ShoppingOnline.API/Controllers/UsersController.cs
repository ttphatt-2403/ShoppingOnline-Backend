using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using ShoppingOnline.API.Models;
using ShoppingOnline.API.DTOs.Users;
using ShoppingOnline.API.Enums;
using ShoppingOnline.API.Services;

namespace ShoppingOnline.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : BaseController
    {
        private readonly ShoppingDbContext _context;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IJwtService _jwtService;

        public UsersController(ShoppingDbContext context, IPasswordHasher passwordHasher, IJwtService jwtService)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _jwtService = jwtService;
        }

        /// <summary>
        /// Get all users
        /// </summary>
        /// <param name="includeInactive">Set to true to include inactive users in results</param>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<UserResponse>>> GetUsers(bool includeInactive = false)
        {
            var query = _context.Users.Include(u => u.Role).AsQueryable();
            
            // Only show active users unless includeInactive is true
            if (!includeInactive)
            {
                query = query.Where(u => u.IsActive);
            }
            
            return await query
                .Select(u => new UserResponse
                {
                    UserId = u.UserId,
                    Username = u.Username,
                    Email = u.Email,
                    Phone = u.Phone,
                    RoleId = u.RoleId,
                    RoleName = u.Role != null ? u.Role.RoleName : null,
                    CreatedAt = u.CreatedAt,
                    IsActive = u.IsActive
                })
                .ToListAsync();
        }

        // GET: api/Users/5
        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<UserResponse>> GetUser(int id)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .Where(u => u.UserId == id)
                .Select(u => new UserResponse
                {
                    UserId = u.UserId,
                    Username = u.Username,
                    Email = u.Email,
                    Phone = u.Phone,
                    RoleId = u.RoleId,
                    RoleName = u.Role != null ? u.Role.RoleName : null,
                    CreatedAt = u.CreatedAt,
                    IsActive = u.IsActive
                })
                .FirstOrDefaultAsync();

            if (user == null)
            {
                return NotFound();
            }

            return user;
        }

        /// <summary>
        /// Register new customer account
        /// </summary>
        [HttpPost("register")]
        public async Task<ActionResult<UserResponse>> Register(RegisterRequest request)
        {
            // Validate ModelState
            var validationResult = ValidateModelState();
            if (validationResult != null) return validationResult;

            // Validate required parameters
            var paramValidation = ValidateRequiredParameters(
                (request.Username, nameof(request.Username)),
                (request.Password, nameof(request.Password))
            );
            if (paramValidation != null) return paramValidation;

            // Sanitize inputs
            request.Username = SanitizeString(request.Username);
            request.Email = string.IsNullOrWhiteSpace(request.Email) ? null : SanitizeString(request.Email);
            request.Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : SanitizeString(request.Phone);

            // Validate username
            if (request.Username.Length < 3 || request.Username.Length > 50)
            {
                return ErrorResponse("Username must be between 3 and 50 characters");
            }

            // Validate password strength
            var (isPasswordValid, passwordErrors) = ValidatePassword(request.Password);
            if (!isPasswordValid)
            {
                return ErrorResponse("Password validation failed", passwordErrors);
            }

            // Validate email format if provided
            if (!string.IsNullOrEmpty(request.Email) && !IsValidEmail(request.Email))
            {
                return ErrorResponse("Invalid email format");
            }

            // Validate phone number if provided
            if (!IsValidPhoneNumber(request.Phone))
            {
                return ErrorResponse("Invalid phone number format. Use Vietnamese format: 0xxxxxxxxx or +84xxxxxxxxx");
            }

            // Kiểm tra username đã tồn tại chưa
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Username.ToLower() == request.Username.ToLower());

            if (existingUser != null)
            {
                return ErrorResponse("Username already exists", null, 409);
            }

            // Kiểm tra email đã tồn tại chưa
            if (!string.IsNullOrEmpty(request.Email))
            {
                var existingEmail = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email != null && u.Email.ToLower() == request.Email.ToLower());

                if (existingEmail != null)
                {
                    return ErrorResponse("Email already exists", null, 409);
                }
            }

            var user = new User
            {
                Username = request.Username,
                Password = _passwordHasher.HashPassword(request.Password), // Hash password
                Email = request.Email,
                Phone = request.Phone,
                RoleId = (int)RoleType.Customer, // Always Customer role for registration
                CreatedAt = DateTime.UtcNow,
                IsActive = true // New registrations are active by default
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Reload user với role để lấy RoleName
            await _context.Entry(user).Reference(u => u.Role).LoadAsync();

            var response = new UserResponse
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email,
                Phone = user.Phone,
                RoleId = user.RoleId,
                RoleName = user.Role != null ? user.Role.RoleName : null,
                CreatedAt = user.CreatedAt,
                IsActive = user.IsActive
            };

            return SuccessResponse(response, "User registered successfully");
        }

        /// <summary>
        /// Login user and return JWT token
        /// </summary>
        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
        {
            // Tìm user theo username
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Username.ToLower() == request.Username.ToLower() && u.IsActive);

            if (user == null)
                return Unauthorized("Username hoặc password không đúng");

            // Kiểm tra mật khẩu
            if (!_passwordHasher.VerifyPassword(user.Password, request.Password))
                return Unauthorized("Username hoặc password không đúng");

            // Tạo JWT token
            var token = _jwtService.GenerateToken(user, user.Role?.RoleName);

            var response = new LoginResponse
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email,
                Token = token,
                RoleId = user.RoleId,
                RoleName = user.Role?.RoleName
            };

            return Ok(response);
        }

        // POST: api/Users
        [HttpPost]
        [Authorize(Roles = "Admin")] // Only Admin can create users with any role
        public async Task<ActionResult<UserResponse>> PostUser(CreateUserRequest request)
        {
            // Kiểm tra username đã tồn tại chưa
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Username.ToLower() == request.Username.ToLower());

            if (existingUser != null)
            {
                return BadRequest("Username already exists");
            }

            // Kiểm tra email đã tồn tại chưa (nếu có email)
            if (!string.IsNullOrEmpty(request.Email))
            {
                var existingEmail = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email != null && u.Email.ToLower() == request.Email.ToLower());

                if (existingEmail != null)
                {
                    return BadRequest("Email already exists");
                }
            }

            // Kiểm tra role có tồn tại không (nếu có roleId)
            if (request.RoleId.HasValue)
            {
                if (!Enum.IsDefined(typeof(RoleType), request.RoleId.Value))
                {
                    return BadRequest("Invalid RoleId");
                }
            }

            var user = new User
            {
                Username = request.Username,
                Password = _passwordHasher.HashPassword(request.Password), // Hash password
                Email = request.Email,
                Phone = request.Phone,
                RoleId = request.RoleId ?? (int)RoleType.Customer, // Default to Customer role
                CreatedAt = DateTime.UtcNow,
                IsActive = request.IsActive
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Reload user với role để lấy RoleName
            await _context.Entry(user).Reference(u => u.Role).LoadAsync();

            var response = new UserResponse
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email,
                Phone = user.Phone,
                RoleId = user.RoleId,
                RoleName = user.Role != null ? user.Role.RoleName : null,
                CreatedAt = user.CreatedAt,
                IsActive = user.IsActive
            };

            return CreatedAtAction(nameof(GetUser), new { id = user.UserId }, response);
        }

        // PUT: api/Users/5
        [HttpPut("{id}")]
        public async Task<ActionResult<UserResponse>> PutUser(int id, UpdateUserRequest request)
        {
            if (id != request.UserId)
            {
                return BadRequest("ID in URL does not match ID in request body");
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null) 
            {
                return NotFound("User not found");
            }

            // Kiểm tra username đã tồn tại chưa (trừ user hiện tại)
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Username.ToLower() == request.Username.ToLower() && u.UserId != id);

            if (existingUser != null)
            {
                return BadRequest("Username already exists");
            }

            // Kiểm tra email đã tồn tại chưa (trừ user hiện tại)
            if (!string.IsNullOrEmpty(request.Email))
            {
                var existingEmail = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email != null && u.Email.ToLower() == request.Email.ToLower() && u.UserId != id);

                if (existingEmail != null)
                {
                    return BadRequest("Email already exists");
                }
            }

            // Kiểm tra role có tồn tại không (nếu có roleId)
            if (request.RoleId.HasValue)
            {
                if (!Enum.IsDefined(typeof(RoleType), request.RoleId.Value))
                {
                    return BadRequest("Invalid RoleId");
                }
            }

            user.Username = request.Username;
            user.Email = request.Email;
            user.Phone = request.Phone;
            user.RoleId = request.RoleId;
            if (request.IsActive.HasValue)
            {
                user.IsActive = request.IsActive.Value;
            }
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Reload user với role để lấy RoleName
            await _context.Entry(user).Reference(u => u.Role).LoadAsync();

            var response = new UserResponse
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email,
                Phone = user.Phone,
                RoleId = user.RoleId,
                RoleName = user.Role != null ? user.Role.RoleName : null,
                CreatedAt = user.CreatedAt,
                IsActive = user.IsActive
            };

            return Ok(response);
        }

        /// <summary>
        /// Soft delete a user by setting IsActive to false
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            
            if (user == null)
            {
                return NotFound("User not found");
            }

            // Soft delete - mark as inactive instead of removing from database
            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // GET: api/Users/Roles
        [HttpGet("Roles")]
        public ActionResult<object> GetRoles()
        {
            var roles = new
            {
                Admin = (int)RoleType.Admin,
                ProductManager = (int)RoleType.ProductManager,
                OrderManager = (int)RoleType.OrderManager,
                Account = (int)RoleType.Account,
                Shipper = (int)RoleType.Shipper,
                Customer = (int)RoleType.Customer
            };

            return Ok(roles);
        }
    }
}
