using Board.Contracts;
using Board.Contracts.Advert;

namespace Board.Application.AppData.Contexts.Adverts.Services;

/// <summary>
/// Сервис для работы с объявлениями.
/// </summary>
public interface IAdvertService
{
    /// <summary>
    /// Получить страницу активных объявлений, от новых к старым.
    /// </summary>
    Task<AdvertShortInfoDto[]> GetAll(PageRequestDto page, CancellationToken cancellationToken);

    /// <summary>
    /// Получить объявление по идентификатору.
    /// </summary>
    /// <exception cref="Common.Exceptions.EntityNotFoundException">Объявление не найдено.</exception>
    Task<AdvertInfoDto> Get(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Создать объявление от имени текущего пользователя.
    /// </summary>
    /// <exception cref="Common.Exceptions.BusinessRuleException">Категория не существует.</exception>
    Task<AdvertInfoDto> Add(CreateAdvertDto dto, CancellationToken cancellationToken);

    /// <summary>
    /// Получить модель для редактирования объявления (например, для частичного обновления).
    /// </summary>
    /// <exception cref="Common.Exceptions.EntityNotFoundException">Объявление не найдено.</exception>
    /// <exception cref="Common.Exceptions.AccessDeniedException">Текущий пользователь не автор объявления.</exception>
    Task<UpdateAdvertDto> GetForUpdate(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Обновить объявление.
    /// </summary>
    /// <exception cref="Common.Exceptions.EntityNotFoundException">Объявление не найдено.</exception>
    /// <exception cref="Common.Exceptions.AccessDeniedException">Текущий пользователь не автор объявления.</exception>
    /// <exception cref="Common.Exceptions.BusinessRuleException">Категория не существует.</exception>
    Task<AdvertInfoDto> Update(Guid id, UpdateAdvertDto dto, CancellationToken cancellationToken);

    /// <summary>
    /// Удалить объявление. Удаление несуществующего объявления ничего не делает.
    /// </summary>
    /// <exception cref="Common.Exceptions.AccessDeniedException">Текущий пользователь не автор объявления.</exception>
    Task Delete(Guid id, CancellationToken cancellationToken);
}
