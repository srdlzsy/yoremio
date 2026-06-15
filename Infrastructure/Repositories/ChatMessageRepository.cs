using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class ChatMessageRepository : BaseRepository<ChatMessage>, IChatMessageRepository
    {
        private readonly YoremioContext _dbContext;

        public ChatMessageRepository(YoremioContext context) : base(context)
        {
            _dbContext = context;
        }

        public async Task<bool> UserExistsAsync(string userId)
        {
            return await _dbContext.Users.AnyAsync(user => user.Id == userId);
        }

        public async Task<ChatMessage?> GetByIdWithUsersAsync(long id)
        {
            return await _dbContext.ChatMessages
                .AsNoTracking()
                .Include(message => message.Sender)
                .Include(message => message.Receiver)
                .FirstOrDefaultAsync(message => message.Id == id);
        }

        public async Task<IReadOnlyCollection<ChatMessage>> GetRecentMessagesForUserAsync(string userId, int take)
        {
            return await _dbContext.ChatMessages
                .AsNoTracking()
                .Include(message => message.Sender)
                .Include(message => message.Receiver)
                .Where(message => message.SenderId == userId || message.ReceiverId == userId)
                .OrderByDescending(message => message.SentAt)
                .ThenByDescending(message => message.Id)
                .Take(take)
                .ToListAsync();
        }

        public async Task<IReadOnlyCollection<ChatMessage>> GetConversationMessagesAsync(string firstUserId, string secondUserId, int skip, int take)
        {
            var messages = await _dbContext.ChatMessages
                .AsNoTracking()
                .Where(message =>
                    (message.SenderId == firstUserId && message.ReceiverId == secondUserId) ||
                    (message.SenderId == secondUserId && message.ReceiverId == firstUserId))
                .OrderByDescending(message => message.SentAt)
                .ThenByDescending(message => message.Id)
                .Skip(skip)
                .Take(take)
                .ToListAsync();

            return messages
                .OrderBy(message => message.SentAt)
                .ThenBy(message => message.Id)
                .ToList();
        }

        public async Task<int> GetConversationMessageCountAsync(string firstUserId, string secondUserId)
        {
            return await _dbContext.ChatMessages
                .AsNoTracking()
                .CountAsync(message =>
                    (message.SenderId == firstUserId && message.ReceiverId == secondUserId) ||
                    (message.SenderId == secondUserId && message.ReceiverId == firstUserId));
        }

        public async Task<int> GetUnreadCountAsync(string receiverId, string senderId)
        {
            return await _dbContext.ChatMessages
                .AsNoTracking()
                .CountAsync(message =>
                    message.ReceiverId == receiverId &&
                    message.SenderId == senderId &&
                    message.ReadAt == null);
        }

        public async Task<int> MarkConversationReadAsync(string receiverId, string senderId, DateTime readAt)
        {
            return await _dbContext.ChatMessages
                .Where(message =>
                    message.ReceiverId == receiverId &&
                    message.SenderId == senderId &&
                    message.ReadAt == null)
                .ExecuteUpdateAsync(setters => setters.SetProperty(message => message.ReadAt, readAt));
        }
    }
}
