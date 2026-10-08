using AutoMapper;
using Board.Application.AppData.Common;
using Board.Application.AppData.Contexts.Adverts.Repositories;
using Board.Contracts.Advert;
using Board.Domain.Adverts;

namespace Board.Application.AppData.Contexts.Adverts.Services;

/// <inheritdoc />
public class AdvertService : IAdvertService
{
    private readonly IAdvertRepository _advertRepository;
    private readonly IMapper _mapper;
    private readonly ICurrentUserAccessor _currentUserAccessor;

    public AdvertService(IAdvertRepository advertRepository, IMapper mapper, ICurrentUserAccessor currentUserAccessor)
    {
        _advertRepository = advertRepository;
        _mapper = mapper;
        _currentUserAccessor = currentUserAccessor;
    }

    /// <inheritdoc />
    public Task<AdvertShortInfoDto[]> GetAll(CancellationToken cancellationToken)
    {
        return _advertRepository.GetAll(cancellationToken);
    }

    /// <inheritdoc />
    public Task<AdvertInfoDto> Get(Guid id, CancellationToken cancellationToken)
    {
        return _advertRepository.Get(id, cancellationToken);
    }

    /// <inheritdoc />
    public Task<AdvertInfoDto> Add(CreateAdvertDto dto, CancellationToken cancellationToken)
    {
        Advert entity = _mapper.Map<Advert>(dto);
        entity.AccountId = _currentUserAccessor.GetCurrentAccountId();
        return _advertRepository.Add(entity, cancellationToken);
    }

    /// <inheritdoc />
    public async Task Delete(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _advertRepository.FindById(id, cancellationToken);
        if (entity == null)
        {
            return;
        }

        if (entity.AccountId == null || entity.AccountId != _currentUserAccessor.GetCurrentAccountId())
        {
            throw new UnauthorizedAccessException("Удалить объявление может только его автор.");
        }

        await _advertRepository.Delete(entity, cancellationToken);
    }
}