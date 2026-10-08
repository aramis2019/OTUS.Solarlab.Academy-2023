using Board.Contracts.Category;

namespace Board.Application.AppData.Contexts.Categories.Services
{
    /// <summary>
    /// Сервис категорий.
    /// </summary>
    public interface ICategoryService
    {
        /// <summary>
        /// Создать категорию.
        /// </summary>
        /// <returns>Идентификатор созданной категории.</returns>
        /// <exception cref="Common.Exceptions.BusinessRuleException">Родительская категория не существует.</exception>
        Task<Guid> CreateAsync(CreateCategoryDto model, CancellationToken cancellationToken);

        /// <summary>
        /// Получить категорию по идентификатору.
        /// </summary>
        /// <exception cref="Common.Exceptions.EntityNotFoundException">Категория не найдена.</exception>
        Task<CategoryInfoDto> GetByIdAsync(Guid id, CancellationToken cancellationToken);

        /// <summary>
        /// Получить список активных категорий.
        /// </summary>
        Task<List<CategoryInfoDto>> GetActiveAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Получить модель для редактирования категории (например, для частичного обновления).
        /// </summary>
        /// <exception cref="Common.Exceptions.EntityNotFoundException">Категория не найдена.</exception>
        Task<UpdateCategoryDto> GetForUpdateAsync(Guid id, CancellationToken cancellationToken);

        /// <summary>
        /// Обновить категорию.
        /// </summary>
        /// <exception cref="Common.Exceptions.EntityNotFoundException">Категория не найдена.</exception>
        /// <exception cref="Common.Exceptions.BusinessRuleException">Родительская категория не существует или образуется цикл.</exception>
        Task<CategoryInfoDto> UpdateAsync(Guid id, UpdateCategoryDto model, CancellationToken cancellationToken);

        /// <summary>
        /// Удалить категорию. Удаление несуществующей категории ничего не делает.
        /// </summary>
        /// <exception cref="Common.Exceptions.BusinessRuleException">У категории есть дочерние категории или объявления.</exception>
        Task DeleteAsync(Guid id, CancellationToken cancellationToken);
    }
}
