using AutoMapper;
using AutoMapper.QueryableExtensions;
using Board.Application.AppData.Contexts.Adverts.Repositories;
using Board.Contracts.Advert;
using Board.Infrastucture.Repository;
using Microsoft.EntityFrameworkCore;

namespace Board.Infrastucture.DataAccess.Contexts.Advert.Repository;

using Advert = Domain.Adverts.Advert;

/// <inheritdoc cref="IAdvertRepository"/>
public class AdvertRepository : IAdvertRepository
{
    private readonly IRepository<Advert> _repository;
    private readonly IMapper _mapper;

    public AdvertRepository(IRepository<Advert> advertRepository, IMapper mapper)
    {
        _repository = advertRepository;
        _mapper = mapper;
    }

    public Task<AdvertShortInfoDto[]> GetAll(int skip, int take, CancellationToken cancellationToken)
    {
        return _repository.GetAll().Where(s => s.IsActive)
            .OrderByDescending(s => s.Created).ThenBy(s => s.Id)
            .Skip(skip).Take(take)
            .ProjectTo<AdvertShortInfoDto>(_mapper.ConfigurationProvider)
            .ToArrayAsync(cancellationToken);
    }

    public Task<AdvertInfoDto> Get(Guid id, CancellationToken cancellationToken)
    {
        return _repository.GetAll()
            .ProjectTo<AdvertInfoDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task Add(Advert entity, CancellationToken cancellationToken)
    {
        entity.Created = DateTime.UtcNow;
        return _repository.AddAsync(entity, cancellationToken);
    }

    public Task Update(Advert entity, CancellationToken cancellationToken)
    {
        return _repository.UpdateAsync(entity, cancellationToken);
    }

    public Task<Advert> FindById(Guid id, CancellationToken cancellationToken)
    {
        return _repository.GetByIdAsync(id, cancellationToken);
    }

    public Task Delete(Advert entity, CancellationToken cancellationToken)
    {
        return _repository.DeleteAsync(entity, cancellationToken);
    }
}