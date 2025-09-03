using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoppingOnline.API.Models;

namespace ShoppingOnline.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RolesController : ControllerBase
    {
        private readonly ShoppingDbContext _context;

        public RolesController(ShoppingDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Get all roles (public endpoint for registration forms)
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<object>> GetRoles()
        {
            var roles = await _context.Roles
                .Select(r => new
                {
                    roleId = r.RoleId,
                    roleName = r.RoleName,
                    description = r.Description
                })
                .ToListAsync();

            return Ok(new
            {
                roles = roles,
                totalCount = roles.Count
            });
        }

        /// <summary>
        /// Get role by ID
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<object>> GetRole(int id)
        {
            var role = await _context.Roles
                .Include(r => r.Users)
                .FirstOrDefaultAsync(r => r.RoleId == id);

            if (role == null)
            {
                return NotFound("Role not found");
            }

            return Ok(new
            {
                roleId = role.RoleId,
                roleName = role.RoleName,
                description = role.Description,
                userCount = role.Users.Count,
                users = role.Users.Select(u => new
                {
                    userId = u.UserId,
                    username = u.Username,
                    email = u.Email,
                    isActive = u.IsActive
                }).ToList()
            });
        }

        /// <summary>
        /// Create new role (Admin only)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<object>> CreateRole([FromBody] object request)
        {
            // Parse request body
            var requestData = System.Text.Json.JsonSerializer.Deserialize<dynamic>(request.ToString() ?? "{}");
            var roleName = ((System.Text.Json.JsonElement)requestData).GetProperty("roleName").GetString();
            
            string? description = null;
            if (((System.Text.Json.JsonElement)requestData).TryGetProperty("description", out var descElement))
            {
                description = descElement.GetString();
            }

            if (string.IsNullOrWhiteSpace(roleName))
            {
                return BadRequest("Role name is required");
            }

            // Check if role already exists
            var existingRole = await _context.Roles
                .FirstOrDefaultAsync(r => r.RoleName.ToLower() == roleName.ToLower());

            if (existingRole != null)
            {
                return BadRequest("Role name already exists");
            }

            var role = new Role
            {
                RoleName = roleName,
                Description = description
            };

            _context.Roles.Add(role);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetRole), new { id = role.RoleId }, new
            {
                roleId = role.RoleId,
                roleName = role.RoleName,
                description = role.Description,
                message = "Role created successfully"
            });
        }

        /// <summary>
        /// Update role (Admin only)
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<object>> UpdateRole(int id, [FromBody] object request)
        {
            var role = await _context.Roles.FindAsync(id);
            if (role == null)
            {
                return NotFound("Role not found");
            }

            // Parse request body
            var requestData = System.Text.Json.JsonSerializer.Deserialize<dynamic>(request.ToString() ?? "{}");
            var roleName = ((System.Text.Json.JsonElement)requestData).GetProperty("roleName").GetString();
            
            string? description = null;
            if (((System.Text.Json.JsonElement)requestData).TryGetProperty("description", out var descElement))
            {
                description = descElement.GetString();
            }

            if (string.IsNullOrWhiteSpace(roleName))
            {
                return BadRequest("Role name is required");
            }

            // Check if role name already exists (excluding current role)
            var existingRole = await _context.Roles
                .FirstOrDefaultAsync(r => r.RoleName.ToLower() == roleName.ToLower() && r.RoleId != id);

            if (existingRole != null)
            {
                return BadRequest("Role name already exists");
            }

            role.RoleName = roleName;
            role.Description = description;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                roleId = role.RoleId,
                roleName = role.RoleName,
                description = role.Description,
                message = "Role updated successfully"
            });
        }

        /// <summary>
        /// Delete role (Admin only) - Cannot delete if users are assigned
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteRole(int id)
        {
            var role = await _context.Roles
                .Include(r => r.Users)
                .FirstOrDefaultAsync(r => r.RoleId == id);

            if (role == null)
            {
                return NotFound("Role not found");
            }

            // Check if any users are assigned to this role
            if (role.Users.Any())
            {
                return BadRequest($"Cannot delete role. {role.Users.Count} users are assigned to this role.");
            }

            _context.Roles.Remove(role);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Role deleted successfully" });
        }

        /// <summary>
        /// Get users by role (Admin only)
        /// </summary>
        [HttpGet("{id}/users")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<object>> GetUsersByRole(int id, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var role = await _context.Roles.FindAsync(id);
            if (role == null)
            {
                return NotFound("Role not found");
            }

            var totalCount = await _context.Users.CountAsync(u => u.RoleId == id);
            var users = await _context.Users
                .Where(u => u.RoleId == id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new
                {
                    userId = u.UserId,
                    username = u.Username,
                    email = u.Email,
                    phone = u.Phone,
                    isActive = u.IsActive,
                    createdAt = u.CreatedAt
                })
                .ToListAsync();

            return Ok(new
            {
                roleName = role.RoleName,
                users = users,
                totalCount = totalCount,
                page = page,
                pageSize = pageSize,
                totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            });
        }
    }
}
