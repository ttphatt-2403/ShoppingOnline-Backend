using System;
using System.Collections.Generic;

namespace ShoppingOnline.API.Models;

public partial class User
{
    public int UserId { get; set; }

    public string Username { get; set; } = null!;

    public string Password { get; set; } = null!;

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public int? RoleId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
    
    /// <summary>
    /// Indicates whether the user account is active.
    /// When false, the user is considered soft-deleted.
    /// </summary>
    public bool IsActive { get; set; } = true;

    public virtual ICollection<Cart> Carts { get; set; } = new List<Cart>();

    public virtual ICollection<ChatConversation> ChatConversationCustomers { get; set; } = new List<ChatConversation>();

    public virtual ICollection<ChatConversation> ChatConversationStaffs { get; set; } = new List<ChatConversation>();

    public virtual ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();

    public virtual ICollection<Complaint> Complaints { get; set; } = new List<Complaint>();

    public virtual ICollection<Order> OrderAssignedShippers { get; set; } = new List<Order>();

    public virtual ICollection<Order> OrderCreatedByNavigations { get; set; } = new List<Order>();

    public virtual ICollection<Order> OrderUsers { get; set; } = new List<Order>();

    public virtual ICollection<Report> Reports { get; set; } = new List<Report>();

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

    public virtual Role? Role { get; set; }

    public virtual ICollection<Shipping> Shippings { get; set; } = new List<Shipping>();
}
