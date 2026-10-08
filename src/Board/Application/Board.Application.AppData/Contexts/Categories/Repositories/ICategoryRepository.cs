using Board.Contracts.Category;
using Board.Domain.Categories;

namespace Board.Application.AppData.Contexts.Categories.Repositories
{
    /// <summary>
    /// Репозиторий категорий.
    /// </summary>
    public interface ICategoryRepository
    {
        /// <summary>
        /// Добавить категорию.
        /// </summary>
        /// <returns>Идентификатор созданной категории.</returns>
        Task<Guid> AddAsync(Category model, CancellationToken cancellationToken);

        /// <summary>
        /// Получить модель категории по идентификатору.
        /// </summary>
        /// <returns>Модель категории или null, если не найдена.</returns>
        Task<CategoryInfoDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

        /// <summary>
        /// Получить список активных категорий.
        /// </summary>
        Task<List<Category>> GetActiveAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Найти сущность категории по идентификатору.
        /// </summary>
        /// <returns>Категория или null, если не найдена.</returns>
        Task<Category?> FindById(Guid id, CancellationToken cancellationToken);

        /// <summary>
        /// Проверить, есть ли у категории дочерние категории или объявления.
        /// </summary>
        Task<bool> IsInUseAsync(Guid id, CancellationToken cancellationToken);

        /// <summary>
        /// Сохранить изменения категории.
        /// </summary>
        Task UpdateAsync(Category model, CancellationToken cancellationToken);

        /// <summary>
        /// Удалить категорию.
        /// </summary>
        Task DeleteAsync(Category model, CancellationToken cancellationToken);
    }
}
