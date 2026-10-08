using AutoMapper;
using AutoMapper.QueryableExtensions;
using Board.Application.AppData.Contexts.Categories.Repositories;
using Board.Contracts.Category;
using Board.Infrastructure.Repository;
using Microsoft.EntityFrameworkCore;

namespace Board.Infrastructure.DataAccess.Contexts.Category.Repository
{
    /// <inheritdoc cref="ICategoryRepository"/>
    public class CategoryRepository : ICategoryRepository
    {
        private readonly IRepository<Domain.Categories.Category> _repository;
        private readonly IMapper _mapper;

        public CategoryRepository(IRepository<Domain.Categories.Category> repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        /// <inheritdoc/>
        public async Task<Guid> AddAsync(Domain.Categories.Category model, CancellationToken cancellationToken)
        {
            model.Created = DateTime.UtcNow;
            await _repository.AddAsync(model, cancellationToken);
            return model.Id;
        }

        /// <inheritdoc/>
        public Task<List<Domain.Categories.Category>> GetActiveAsync(CancellationToken cancellationToken)
        {
            return _repository.GetAll().Where(s => s.IsActive).ToListAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public Task<CategoryInfoDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return _repository.GetAll().Where(s => s.Id == id)
                              .ProjectTo<CategoryInfoDto>(_mapper.ConfigurationProvider)
                              .FirstOrDefaultAsync(cancellationToken);
        }

        public Task<Domain.Categories.Category?> FindById(Guid id, CancellationToken cancellationToken)
        {
            return _repository.GetByIdAsync(id, cancellationToken);
        }

        public async Task<bool> IsInUseAsync(Guid id, CancellationToken cancellationToken)
        {
            return await _repository.GetAll().AnyAsync(s => s.ParentId == id, cancellationToken)
                || await _repository.GetAll().Where(s => s.Id == id).SelectMany(s => s.Adverts).AnyAsync(cancellationToken);
        }

        public Task UpdateAsync(Domain.Categories.Category model, CancellationToken cancellationToken)
        {
            return _repository.UpdateAsync(model, cancellationToken);
        }

        public Task DeleteAsync(Domain.Categories.Category model, CancellationToken cancellationToken)
        {
            return _repository.DeleteAsync(model, cancellationToken);
        }
    }
}
