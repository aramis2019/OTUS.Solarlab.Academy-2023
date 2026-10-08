using Board.Contracts.Advert;
using Board.Domain.Adverts;

namespace Board.Application.AppData.Contexts.Adverts.Repositories;

/// <summary>
/// Репозиторий для работы с объявлениями.
/// </summary>
public interface IAdvertRepository
{
    /// <summary>
    /// Получить страницу активных объявлений, от новых к старым.
    /// </summary>
    /// <param name="skip">Сколько объявлений пропустить.</param>
    /// <param name="take">Сколько объявлений вернуть.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Список объявлений.</returns>
    Task<AdvertShortInfoDto[]> GetAll(int skip, int take, CancellationToken cancellationToken);

    /// <summary>
    /// Получить объявление по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор объявления.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Модель объявления или null, если не найдено.</returns>
    Task<AdvertInfoDto?> Get(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Найти сущность объявления по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор объявления.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Объявление или null, если не найдено.</returns>
    Task<Advert?> FindById(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Добавить объявление.
    /// </summary>
    /// <param name="entity">Объявление.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    Task Add(Advert entity, CancellationToken cancellationToken);

    /// <summary>
    /// Сохранить изменения объявления.
    /// </summary>
    /// <param name="entity">Объявление.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    Task Update(Advert entity, CancellationToken cancellationToken);

    /// <summary>
    /// Удалить объявление.
    /// </summary>
    /// <param name="entity">Объявление.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    Task Delete(Advert entity, CancellationToken cancellationToken);
}
