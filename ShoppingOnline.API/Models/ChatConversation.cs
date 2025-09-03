using System;
using System.Collections.Generic;

namespace ShoppingOnline.API.Models;

public partial class ChatConversation
{
    public int ConversationId { get; set; }

    public int? CustomerId { get; set; }

    public int? StaffId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? LastMessageAt { get; set; }

    public virtual ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();

    public virtual User? Customer { get; set; }

    public virtual User? Staff { get; set; }
}
