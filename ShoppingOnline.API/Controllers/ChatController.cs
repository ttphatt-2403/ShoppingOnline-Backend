using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoppingOnline.API.DTOs.Chat;
using ShoppingOnline.API.Models;
using System.Security.Claims;

namespace ShoppingOnline.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ChatController : ControllerBase
    {
        private readonly ShoppingDbContext _context;

        public ChatController(ShoppingDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Get all conversations (Admin only, with pagination)
        /// </summary>
        [HttpGet("conversations")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<ChatConversationResponse>>> GetAllConversations([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var conversations = await _context.ChatConversations
                    .Include(c => c.Customer)
                    .Include(c => c.Staff)
                    .Include(c => c.ChatMessages)
                    .OrderByDescending(c => c.LastMessageAt ?? c.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(c => new ChatConversationResponse
                    {
                        ConversationId = c.ConversationId,
                        CustomerId = c.CustomerId ?? 0,
                        StaffId = c.StaffId,
                        CreatedAt = c.CreatedAt ?? DateTime.UtcNow,
                        LastMessageAt = c.LastMessageAt,
                        CustomerName = c.Customer != null ? c.Customer.Username : "Unknown Customer",
                        StaffName = c.Staff != null ? c.Staff.Username : "Unassigned",
                        LastMessage = c.ChatMessages.OrderByDescending(m => m.CreatedAt).FirstOrDefault() != null
                            ? c.ChatMessages.OrderByDescending(m => m.CreatedAt).First().MessageText
                            : "No messages",
                        UnreadCount = c.ChatMessages.Count(m => m.IsRead == false)
                    })
                    .ToListAsync();

                var totalCount = await _context.ChatConversations.CountAsync();

                var result = new
                {
                    conversations = conversations,
                    totalCount = totalCount,
                    page = page,
                    pageSize = pageSize,
                    totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving conversations", details = ex.Message });
            }
        }

        /// <summary>
        /// Get conversations for current user
        /// </summary>
        [HttpGet("my-conversations")]
        public async Task<ActionResult<IEnumerable<ChatConversationResponse>>> GetMyConversations()
        {
            try
            {
                var userId = GetCurrentUserId();
                var userRole = GetCurrentUserRole();

                if (userId == null)
                    return Unauthorized(new { message = "User not found" });

                IQueryable<ChatConversation> query = _context.ChatConversations
                    .Include(c => c.Customer)
                    .Include(c => c.Staff)
                    .Include(c => c.ChatMessages);

                // Filter based on user role
                if (userRole == "Admin")
                {
                    // Staff can see conversations assigned to them
                    query = query.Where(c => c.StaffId == userId);
                }
                else
                {
                    // Customers can see their own conversations
                    query = query.Where(c => c.CustomerId == userId);
                }

                var conversations = await query
                    .OrderByDescending(c => c.LastMessageAt ?? c.CreatedAt)
                    .Select(c => new ChatConversationResponse
                    {
                        ConversationId = c.ConversationId,
                        CustomerId = c.CustomerId ?? 0,
                        StaffId = c.StaffId,
                        CreatedAt = c.CreatedAt ?? DateTime.UtcNow,
                        LastMessageAt = c.LastMessageAt,
                        CustomerName = c.Customer != null ? c.Customer.Username : "Unknown Customer",
                        StaffName = c.Staff != null ? c.Staff.Username : "Unassigned",
                        LastMessage = c.ChatMessages.OrderByDescending(m => m.CreatedAt).FirstOrDefault() != null
                            ? c.ChatMessages.OrderByDescending(m => m.CreatedAt).First().MessageText
                            : "No messages",
                        UnreadCount = c.ChatMessages.Count(m => m.IsRead == false && m.SenderId != userId)
                    })
                    .ToListAsync();

                return Ok(conversations);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving your conversations", details = ex.Message });
            }
        }

        /// <summary>
        /// Get messages for a conversation
        /// </summary>
        [HttpGet("conversations/{conversationId}/messages")]
        public async Task<ActionResult<IEnumerable<ChatMessageResponse>>> GetConversationMessages(int conversationId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            try
            {
                var userId = GetCurrentUserId();
                var userRole = GetCurrentUserRole();

                if (userId == null)
                    return Unauthorized(new { message = "User not found" });

                // Check if user has access to this conversation
                var conversation = await _context.ChatConversations.FindAsync(conversationId);
                if (conversation == null)
                    return NotFound(new { message = "Conversation not found" });

                // Users can only access their own conversations, admins can access all
                if (userRole != "Admin" && conversation.CustomerId != userId && conversation.StaffId != userId)
                    return Forbid("You don't have access to this conversation");

                var messages = await _context.ChatMessages
                    .Include(m => m.Sender)
                    .Where(m => m.ConversationId == conversationId)
                    .OrderByDescending(m => m.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(m => new ChatMessageResponse
                    {
                        MessageId = m.MessageId,
                        ConversationId = m.ConversationId ?? 0,
                        SenderId = m.SenderId ?? 0,
                        MessageText = m.MessageText ?? "",
                        MessageType = m.MessageType ?? "text",
                        CreatedAt = m.CreatedAt ?? DateTime.UtcNow,
                        IsRead = m.IsRead ?? false,
                        SenderName = m.Sender != null ? m.Sender.Username : "Unknown User",
                        IsFromCustomer = m.SenderId == conversation.CustomerId
                    })
                    .ToListAsync();

                // Mark messages as read for the current user
                var unreadMessages = await _context.ChatMessages
                    .Where(m => m.ConversationId == conversationId && 
                               m.SenderId != userId && 
                               m.IsRead == false)
                    .ToListAsync();

                foreach (var message in unreadMessages)
                {
                    message.IsRead = true;
                }

                if (unreadMessages.Any())
                {
                    await _context.SaveChangesAsync();
                }

                return Ok(messages.OrderBy(m => m.CreatedAt));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving messages", details = ex.Message });
            }
        }

        /// <summary>
        /// Start a new conversation (customers only)
        /// </summary>
        [HttpPost("conversations")]
        public async Task<ActionResult<ChatConversationResponse>> StartConversation()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                    return Unauthorized(new { message = "User not found" });

                // Check if user already has an active conversation
                var existingConversation = await _context.ChatConversations
                    .Include(c => c.Customer)
                    .Include(c => c.Staff)
                    .FirstOrDefaultAsync(c => c.CustomerId == userId);

                if (existingConversation != null)
                {
                    var existingResponse = new ChatConversationResponse
                    {
                        ConversationId = existingConversation.ConversationId,
                        CustomerId = existingConversation.CustomerId ?? 0,
                        StaffId = existingConversation.StaffId,
                        CreatedAt = existingConversation.CreatedAt ?? DateTime.UtcNow,
                        LastMessageAt = existingConversation.LastMessageAt,
                        CustomerName = existingConversation.Customer?.Username ?? "Unknown Customer",
                        StaffName = existingConversation.Staff?.Username ?? "Unassigned",
                        LastMessage = "Existing conversation",
                        UnreadCount = 0
                    };

                    return Ok(existingResponse);
                }

                var conversation = new ChatConversation
                {
                    CustomerId = userId.Value,
                    StaffId = null, // Will be assigned later by admin
                    CreatedAt = DateTime.UtcNow,
                    LastMessageAt = DateTime.UtcNow
                };

                _context.ChatConversations.Add(conversation);
                await _context.SaveChangesAsync();

                var customer = await _context.Users.FindAsync(userId.Value);

                var conversationResponse = new ChatConversationResponse
                {
                    ConversationId = conversation.ConversationId,
                    CustomerId = conversation.CustomerId ?? 0,
                    StaffId = conversation.StaffId,
                    CreatedAt = conversation.CreatedAt ?? DateTime.UtcNow,
                    LastMessageAt = conversation.LastMessageAt,
                    CustomerName = customer?.Username ?? "Unknown Customer",
                    StaffName = "Unassigned",
                    LastMessage = "New conversation started",
                    UnreadCount = 0
                };

                return CreatedAtAction(nameof(GetConversationMessages), 
                    new { conversationId = conversation.ConversationId }, conversationResponse);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while starting the conversation", details = ex.Message });
            }
        }

        /// <summary>
        /// Send a message
        /// </summary>
        [HttpPost("messages")]
        public async Task<ActionResult<ChatMessageResponse>> SendMessage(SendMessageRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var userRole = GetCurrentUserRole();

                if (userId == null)
                    return Unauthorized(new { message = "User not found" });

                // Check if conversation exists
                var conversation = await _context.ChatConversations
                    .Include(c => c.Customer)
                    .Include(c => c.Staff)
                    .FirstOrDefaultAsync(c => c.ConversationId == request.ConversationId);

                if (conversation == null)
                    return NotFound(new { message = "Conversation not found" });

                // Check if user has access to this conversation
                if (userRole != "Admin" && conversation.CustomerId != userId && conversation.StaffId != userId)
                    return Forbid("You don't have access to this conversation");

                // Validate message
                if (string.IsNullOrWhiteSpace(request.MessageText))
                    return BadRequest(new { message = "Message text is required" });

                var validMessageTypes = new[] { "text", "image", "file" };
                if (!validMessageTypes.Contains(request.MessageType))
                    return BadRequest(new { message = "Invalid message type" });

                var message = new ChatMessage
                {
                    ConversationId = request.ConversationId,
                    SenderId = userId.Value,
                    MessageText = request.MessageText,
                    MessageType = request.MessageType,
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false
                };

                _context.ChatMessages.Add(message);

                // Update conversation's last message time
                conversation.LastMessageAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                var sender = await _context.Users.FindAsync(userId.Value);

                var messageResponse = new ChatMessageResponse
                {
                    MessageId = message.MessageId,
                    ConversationId = message.ConversationId ?? 0,
                    SenderId = message.SenderId ?? 0,
                    MessageText = message.MessageText ?? "",
                    MessageType = message.MessageType ?? "text",
                    CreatedAt = message.CreatedAt ?? DateTime.UtcNow,
                    IsRead = message.IsRead ?? false,
                    SenderName = sender?.Username ?? "Unknown User",
                    IsFromCustomer = userId == conversation.CustomerId
                };

                return CreatedAtAction(nameof(GetConversationMessages), 
                    new { conversationId = request.ConversationId }, messageResponse);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while sending the message", details = ex.Message });
            }
        }

        /// <summary>
        /// Assign staff to conversation (Admin only)
        /// </summary>
        [HttpPut("conversations/{conversationId}/assign-staff")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ChatConversationResponse>> AssignStaff(int conversationId, AssignStaffRequest request)
        {
            try
            {
                var conversation = await _context.ChatConversations
                    .Include(c => c.Customer)
                    .Include(c => c.Staff)
                    .FirstOrDefaultAsync(c => c.ConversationId == conversationId);

                if (conversation == null)
                    return NotFound(new { message = "Conversation not found" });

                // Validate staff user
                var staff = await _context.Users.FindAsync(request.StaffId);
                if (staff == null)
                    return BadRequest(new { message = "Staff user not found" });

                conversation.StaffId = request.StaffId;
                await _context.SaveChangesAsync();

                var conversationResponse = new ChatConversationResponse
                {
                    ConversationId = conversation.ConversationId,
                    CustomerId = conversation.CustomerId ?? 0,
                    StaffId = conversation.StaffId,
                    CreatedAt = conversation.CreatedAt ?? DateTime.UtcNow,
                    LastMessageAt = conversation.LastMessageAt,
                    CustomerName = conversation.Customer?.Username ?? "Unknown Customer",
                    StaffName = staff.Username ?? "Unknown Staff",
                    LastMessage = "Staff assigned",
                    UnreadCount = 0
                };

                return Ok(conversationResponse);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while assigning staff", details = ex.Message });
            }
        }

        /// <summary>
        /// Get unread message count for current user
        /// </summary>
        [HttpGet("unread-count")]
        public async Task<ActionResult<object>> GetUnreadCount()
        {
            try
            {
                var userId = GetCurrentUserId();
                var userRole = GetCurrentUserRole();

                if (userId == null)
                    return Unauthorized(new { message = "User not found" });

                int unreadCount;

                if (userRole == "Admin")
                {
                    // For staff, count unread messages in conversations assigned to them
                    unreadCount = await _context.ChatMessages
                        .Include(m => m.Conversation)
                        .CountAsync(m => m.Conversation!.StaffId == userId && 
                                       m.SenderId != userId && 
                                       m.IsRead == false);
                }
                else
                {
                    // For customers, count unread messages in their conversations
                    unreadCount = await _context.ChatMessages
                        .Include(m => m.Conversation)
                        .CountAsync(m => m.Conversation!.CustomerId == userId && 
                                       m.SenderId != userId && 
                                       m.IsRead == false);
                }

                return Ok(new { unreadCount = unreadCount });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while getting unread count", details = ex.Message });
            }
        }

        #region Helper Methods

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : null;
        }

        private string? GetCurrentUserRole()
        {
            return User.FindFirst(ClaimTypes.Role)?.Value;
        }

        #endregion
    }
}
