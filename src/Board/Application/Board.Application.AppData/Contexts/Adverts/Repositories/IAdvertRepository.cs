using Board.Contracts.Advert;
using Board.Domain.Adverts;

namespace Board.Application.AppData.Contexts.Adverts.Repositories;

/// <summary>
/// Репозиторий для работы с объявлениями.
/// </summary>
public interface IAdvertRepository
{
    /// <summary>
    /// Получить список объявлений.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Список объявлений.</returns>
    Task<AdvertShortInfoDto[]> GetAll(CancellationToken cancellationToken);

    /// <summary>
    /// Получить объявление по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор объявления.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Модель объявления.</returns>
    Task<AdvertInfoDto> Get(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Добавить объявление.
    /// </summary>
    /// <param name="entity">Объявление.</param>
    /// <param name="cancellation">Токен отмены операции.</param>
    /// <returns>Модель добавленного объявления.</returns>
    Task<AdvertInfoDto> Add(Advert entity, CancellationToken cancellation);

    /// <summary>
    /// Найти сущность объявления по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор объявления.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Объявление или null, если не найдено.</returns>
    Task<Advert?> FindById(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Удалить объявление.
    /// </summary>
    /// <param name="entity">Объявление.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    Task Delete(Advert entity, CancellationToken cancellationToken);
}