namespace ShoppingOnline.API.DTOs.Chat
{
    public class ChatConversationResponse
    {
        public int ConversationId { get; set; }
        public int CustomerId { get; set; }
        public int? StaffId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastMessageAt { get; set; }
        
        // User details
        public string CustomerName { get; set; } = string.Empty;
        public string? StaffName { get; set; }
        
        // Last message info
        public string? LastMessage { get; set; }
        public int UnreadCount { get; set; }
    }

    public class ChatMessageResponse
    {
        public int MessageId { get; set; }
        public int ConversationId { get; set; }
        public int SenderId { get; set; }
        public string MessageText { get; set; } = string.Empty;
        public string MessageType { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
        
        // Sender details
        public string SenderName { get; set; } = string.Empty;
        public bool IsFromCustomer { get; set; }
    }

    public class CreateConversationRequest
    {
        public int CustomerId { get; set; }
        public int? StaffId { get; set; }
    }

    public class SendMessageRequest
    {
        public int ConversationId { get; set; }
        public string MessageText { get; set; } = string.Empty;
        public string MessageType { get; set; } = "text";
    }

    public class AssignStaffRequest
    {
        public int StaffId { get; set; }
    }
}
