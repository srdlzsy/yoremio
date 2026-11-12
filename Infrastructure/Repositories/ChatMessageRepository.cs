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
                .Include(message => message.Urun)
                .FirstOrDefaultAsync(message => message.Id == id);
        }

        public async Task<IReadOnlyCollection<ChatMessage>> GetRecentMessagesForUserAsync(string userId, int take)
        {
            return await BuildRecentMessagesQuery(userId)
                .Take(take)
                .ToListAsync();
        }

        public async Task<IReadOnlyCollection<ChatMessage>> GetConversationMessagesAsync(string firstUserId, string secondUserId, int skip, int take, int? urunId = null, int? talepId = null)
        {
            var messages = await BuildConversationMessagesQuery(firstUserId, secondUserId, urunId, talepId)
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

        internal IQueryable<ChatMessage> BuildRecentMessagesQuery(string userId)
        {
            return _dbContext.ChatMessages
                .AsNoTracking()
                .Include(message => message.Sender)
                    .ThenInclude(user => user!.SaticiProfili)
                .Include(message => message.Receiver)
                    .ThenInclude(user => user!.SaticiProfili)
                .Include(message => message.Urun)
                .Where(message => message.SenderId == userId || message.ReceiverId == userId)
                .OrderByDescending(message => message.SentAt)
                .ThenByDescending(message => message.Id);
        }

        internal IQueryable<ChatMessage> BuildConversationMessagesQuery(string firstUserId, string secondUserId, int? urunId = null, int? talepId = null)
        {
            var query = _dbContext.ChatMessages
                .AsNoTracking()
                .Include(message => message.Urun)
                .Where(message =>
                    (message.SenderId == firstUserId && message.ReceiverId == secondUserId) ||
                    (message.SenderId == secondUserId && message.ReceiverId == firstUserId));

            if (urunId.HasValue)
            {
                query = query.Where(message => message.UrunId == urunId.Value);
            }

            if (talepId.HasValue)
            {
                query = query.Where(message => message.TalepId == talepId.Value);
            }

            return query;
        }

        public async Task<int> GetConversationMessageCountAsync(string firstUserId, string secondUserId, int? urunId = null, int? talepId = null)
        {
            var query = _dbContext.ChatMessages
                .AsNoTracking()
                .Where(message =>
                    (message.SenderId == firstUserId && message.ReceiverId == secondUserId) ||
                    (message.SenderId == secondUserId && message.ReceiverId == firstUserId));

            if (urunId.HasValue)
            {
                query = query.Where(message => message.UrunId == urunId.Value);
            }

            if (talepId.HasValue)
            {
                query = query.Where(message => message.TalepId == talepId.Value);
            }

            return await query.CountAsync();
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

        public async Task<Dictionary<string, int>> GetUnreadCountsBySenderAsync(string receiverId)
        {
            return await _dbContext.ChatMessages
                .AsNoTracking()
                .Where(message => message.ReceiverId == receiverId && message.ReadAt == null)
                .GroupBy(message => message.SenderId)
                .Select(group => new { SenderId = group.Key, Count = group.Count() })
                .ToDictionaryAsync(item => item.SenderId, item => item.Count);
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
