using AutoMapper;
using Board.Application.AppData.Common.Exceptions;
using Board.Application.AppData.Contexts.Categories.Repositories;
using Board.Contracts.Category;
using Board.Domain.Categories;

namespace Board.Application.AppData.Contexts.Categories.Services
{
    /// <inheritdoc />
    public class CategoryService : ICategoryService
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly IMapper _mapper;

        public CategoryService(ICategoryRepository categoryRepository, IMapper mapper)
        {
            _categoryRepository = categoryRepository;
            _mapper = mapper;
        }

        /// <inheritdoc />
        public async Task<Guid> CreateAsync(CreateCategoryDto model, CancellationToken cancellationToken)
        {
            if (model.ParentId != null)
            {
                await EnsureCategoryExists(model.ParentId.Value, cancellationToken);
            }

            var entity = _mapper.Map<CreateCategoryDto, Category>(model);
            return await _categoryRepository.AddAsync(entity, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<List<CategoryInfoDto>> GetActiveAsync(CancellationToken cancellationToken)
        {
            var entities = await _categoryRepository.GetActiveAsync(cancellationToken);
            return entities.Select(s => new CategoryInfoDto
            {
                Name = s.Name,
                ParentId = s.ParentId,
                IsActive = s.IsActive,
                CreatedAt = s.Created,
                Id = s.Id,
            }).ToList();
        }

        /// <inheritdoc />
        public async Task<CategoryInfoDto> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return await _categoryRepository.GetByIdAsync(id, cancellationToken) ?? throw NotFound(id);
        }

        /// <inheritdoc />
        public async Task<UpdateCategoryDto> GetForUpdateAsync(Guid id, CancellationToken cancellationToken)
        {
            var entity = await _categoryRepository.FindById(id, cancellationToken) ?? throw NotFound(id);
            return _mapper.Map<UpdateCategoryDto>(entity);
        }

        /// <inheritdoc />
        public async Task<CategoryInfoDto> UpdateAsync(Guid id, UpdateCategoryDto model, CancellationToken cancellationToken)
        {
            var entity = await _categoryRepository.FindById(id, cancellationToken) ?? throw NotFound(id);
            if (model.ParentId != null && model.ParentId != entity.ParentId)
            {
                await EnsureParentDoesNotCreateCycle(id, model.ParentId.Value, cancellationToken);
            }

            _mapper.Map(model, entity);
            await _categoryRepository.UpdateAsync(entity, cancellationToken);
            return _mapper.Map<CategoryInfoDto>(entity);
        }

        /// <inheritdoc />
        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
        {
            var entity = await _categoryRepository.FindById(id, cancellationToken);
            if (entity == null)
            {
                return;
            }

            if (await _categoryRepository.IsInUseAsync(id, cancellationToken))
            {
                throw new BusinessRuleException("Нельзя удалить категорию, у которой есть дочерние категории или объявления.");
            }

            await _categoryRepository.DeleteAsync(entity, cancellationToken);
        }

        private async Task EnsureParentDoesNotCreateCycle(Guid id, Guid parentId, CancellationToken cancellationToken)
        {
            await EnsureCategoryExists(parentId, cancellationToken);

            // Поднимаемся от новой родительской категории к корню: если встретим саму категорию — получится цикл.
            var visited = new HashSet<Guid>();
            Guid? currentId = parentId;
            while (currentId != null && visited.Add(currentId.Value))
            {
                if (currentId == id)
                {
                    throw new BusinessRuleException("Категория не может быть вложена сама в себя.");
                }

                currentId = (await _categoryRepository.FindById(currentId.Value, cancellationToken))?.ParentId;
            }
        }

        private async Task EnsureCategoryExists(Guid id, CancellationToken cancellationToken)
        {
            if (await _categoryRepository.FindById(id, cancellationToken) == null)
            {
                throw new BusinessRuleException($"Категория с идентификатором '{id}' не существует.");
            }
        }

        private static EntityNotFoundException NotFound(Guid id) =>
            new($"Категория с идентификатором '{id}' не найдена.");
    }
}
