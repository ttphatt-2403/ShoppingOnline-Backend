namespace ShoppingOnline.API.DTOs.Users
{
    public class LoginResponse
    {
        public int UserId { get; set; }
        public string Username { get; set; } = null!;
        public string? Email { get; set; }
        public string Token { get; set; } = null!;
        public int? RoleId { get; set; }
        public string? RoleName { get; set; }
    }
}