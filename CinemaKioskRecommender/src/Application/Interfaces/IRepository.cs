using CinemaKioskRecommender.Domain.Common;
using System.Linq.Expressions;

namespace CinemaKioskRecommender.Application.Interfaces;

public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(Guid id);
    Task<List<T>> GetAllAsync();
    Task<List<T>> FindAsync(Expression<Func<T, bool>> predicate);
    Task AddAsync(T entity);
    Task SaveChangesAsync();
    IQueryable<T> Query();
}
