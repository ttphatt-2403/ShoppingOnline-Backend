using System.ComponentModel.DataAnnotations;

namespace ShoppingOnline.API.DTOs.Users
{
    public class CreateUserRequest
    {
        [Required(ErrorMessage = "Username is required")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 50 characters")]
        [RegularExpression(@"^[a-zA-Z0-9_.-]+$", ErrorMessage = "Username can only contain letters, numbers, dots, hyphens and underscores")]
        public string Username { get; set; } = null!;

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be between 6 and 100 characters")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$", ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, and one number")]
        public string Password { get; set; } = null!;

        [EmailAddress(ErrorMessage = "Invalid email format")]
        [StringLength(100, ErrorMessage = "Email must not exceed 100 characters")]
        public string? Email { get; set; }

        [RegularExpression(@"^(\+84|84|0)(3|5|7|8|9)[0-9]{8}$", ErrorMessage = "Invalid Vietnamese phone number format")]
        public string? Phone { get; set; }

        [Range(1, 6, ErrorMessage = "RoleId must be between 1 and 6")]
        public int? RoleId { get; set; }
        
        /// <summary>
        /// Optional. Defaults to true if not specified.
        /// </summary>
        public bool IsActive { get; set; } = true;
    }
}
