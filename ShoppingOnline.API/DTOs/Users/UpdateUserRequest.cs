using System.ComponentModel.DataAnnotations;

namespace ShoppingOnline.API.DTOs.Users
{
    public class UpdateUserRequest
    {
        [Required]
        public int UserId { get; set; }

        [Required]
        [StringLength(50)]
        public string Username { get; set; } = null!;

        [EmailAddress]
        public string? Email { get; set; }

        [Phone]
        public string? Phone { get; set; }

        public int? RoleId { get; set; }
        
        /// <summary>
        /// Optional. When set to false, marks the user as inactive (soft-deleted).
        /// </summary>
        public bool? IsActive { get; set; }
    }
}
