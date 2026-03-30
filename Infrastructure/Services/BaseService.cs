using Application.Interfaces;
using Domain.Interfaces;
using System.Linq.Expressions;

namespace Infrastructure.Services
{
    public class BaseService<T> : IBaseService<T> where T : class
    {
        private readonly IBaseRepository<T> _repository;

        public BaseService(IBaseRepository<T> repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<T?> GetByIdAsync(object id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            return await _repository.FindAsync(predicate);
        }

        public async Task<bool> AddAsync(T entity)
        {
            await _repository.AddAsync(entity);
            return await _repository.SaveChangesAsync();
        }

        public async Task<bool> UpdateAsync(T entity)
        {
            _repository.Update(entity);
            return await _repository.SaveChangesAsync();
        }

        public async Task<bool> DeleteAsync(object id)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
                return false;

            _repository.Remove(entity);
            return await _repository.SaveChangesAsync();
        }
    }
}
