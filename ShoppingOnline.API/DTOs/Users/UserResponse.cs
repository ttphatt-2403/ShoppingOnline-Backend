namespace ShoppingOnline.API.DTOs.Users
{
    public class UserResponse
    {
        public int UserId { get; set; }
        public string Username { get; set; } = null!;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public int? RoleId { get; set; }
        public string? RoleName { get; set; }
        public DateTime? CreatedAt { get; set; }
        /// <summary>
        /// Indicates if the user account is active
        /// </summary>
        public bool IsActive { get; set; }
    }
}
