using System;
using System.Collections.Generic;

namespace ShoppingOnline.API.Models;

public partial class ChatMessage
{
    public int MessageId { get; set; }

    public int? ConversationId { get; set; }

    public int? SenderId { get; set; }

    public string? MessageText { get; set; }

    public string? MessageType { get; set; }

    public DateTime? CreatedAt { get; set; }

    public bool? IsRead { get; set; }

    public virtual ChatConversation? Conversation { get; set; }

    public virtual User? Sender { get; set; }
}
