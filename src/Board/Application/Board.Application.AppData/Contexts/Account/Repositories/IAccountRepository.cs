using Board.Domain.Account;
using System.Linq.Expressions;

namespace Board.Application.AppData.Contexts.Accounts.Repositories;

/// <summary>
/// Репозиторий для работы с аккаунтами.
/// </summary>
public interface IAccountRepository
{
    /// <summary>
    /// Поиск пользователя по фильтру.
    /// </summary>
    /// <param name="predicate"></param>
    /// <param name="cancellation"></param>
    /// <returns></returns>
    Task<Account?> FindWhere(Expression<Func<Account, bool>> predicate, CancellationToken cancellation);

    /// <summary>
    /// Поиск пользователя по идентификатору.
    /// </summary>
    /// <param name="id"> Идентификатор пользователя</param>
    /// <param name="cancellation"></param>
    /// <returns></returns>
    Task<Account?> FindById(Guid id, CancellationToken cancellation);

    /// <summary>
    /// Добавление пользователя.
    /// </summary>
    /// <param name="entity">Пользователь.</param>
    /// <returns></returns>
    Task<Guid> AddAsync(Account entity, CancellationToken cancellation);
}