using AutoMapper;
using Board.Application.AppData.Common;
using Board.Application.AppData.Common.Exceptions;
using Board.Application.AppData.Contexts.Adverts.Repositories;
using Board.Application.AppData.Contexts.Categories.Repositories;
using Board.Contracts;
using Board.Contracts.Advert;
using Board.Domain.Adverts;

namespace Board.Application.AppData.Contexts.Adverts.Services;

/// <inheritdoc />
public class AdvertService : IAdvertService
{
    private readonly IAdvertRepository _advertRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IMapper _mapper;
    private readonly ICurrentUserAccessor _currentUserAccessor;

    public AdvertService(
        IAdvertRepository advertRepository,
        ICategoryRepository categoryRepository,
        IMapper mapper,
        ICurrentUserAccessor currentUserAccessor)
    {
        _advertRepository = advertRepository;
        _categoryRepository = categoryRepository;
        _mapper = mapper;
        _currentUserAccessor = currentUserAccessor;
    }

    /// <inheritdoc />
    public Task<AdvertShortInfoDto[]> GetAll(PageRequestDto page, CancellationToken cancellationToken)
    {
        return _advertRepository.GetAll(page.Skip, Math.Clamp(page.Take, 1, PageRequestDto.MaxTake), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<AdvertInfoDto> Get(Guid id, CancellationToken cancellationToken)
    {
        return await _advertRepository.Get(id, cancellationToken) ?? throw NotFound(id);
    }

    /// <inheritdoc />
    public async Task<AdvertInfoDto> Add(CreateAdvertDto dto, CancellationToken cancellationToken)
    {
        await EnsureCategoryExists(dto.CategoryId!.Value, cancellationToken);

        var entity = _mapper.Map<Advert>(dto);
        entity.AccountId = _currentUserAccessor.GetCurrentAccountId();
        await _advertRepository.Add(entity, cancellationToken);
        return _mapper.Map<AdvertInfoDto>(entity);
    }

    /// <inheritdoc />
    public async Task<UpdateAdvertDto> GetForUpdate(Guid id, CancellationToken cancellationToken)
    {
        var entity = await GetOwnAdvert(id, cancellationToken);
        return _mapper.Map<UpdateAdvertDto>(entity);
    }

    /// <inheritdoc />
    public async Task<AdvertInfoDto> Update(Guid id, UpdateAdvertDto dto, CancellationToken cancellationToken)
    {
        var entity = await GetOwnAdvert(id, cancellationToken);
        if (entity.CategoryId != dto.CategoryId)
        {
            await EnsureCategoryExists(dto.CategoryId!.Value, cancellationToken);
        }

        _mapper.Map(dto, entity);
        await _advertRepository.Update(entity, cancellationToken);
        return _mapper.Map<AdvertInfoDto>(entity);
    }

    /// <inheritdoc />
    public async Task Delete(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _advertRepository.FindById(id, cancellationToken);
        if (entity == null)
        {
            return;
        }

        EnsureIsAuthor(entity);
        await _advertRepository.Delete(entity, cancellationToken);
    }

    private async Task<Advert> GetOwnAdvert(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _advertRepository.FindById(id, cancellationToken) ?? throw NotFound(id);
        EnsureIsAuthor(entity);
        return entity;
    }

    private void EnsureIsAuthor(Advert entity)
    {
        if (!_currentUserAccessor.CanModify(entity.AccountId))
        {
            throw new AccessDeniedException("Изменять и удалять объявление может только его автор или администратор.");
        }
    }

    private async Task EnsureCategoryExists(Guid categoryId, CancellationToken cancellationToken)
    {
        if (await _categoryRepository.FindById(categoryId, cancellationToken) == null)
        {
            throw new BusinessRuleException($"Категория с идентификатором '{categoryId}' не существует.");
        }
    }

    private static EntityNotFoundException NotFound(Guid id) =>
        new($"Объявление с идентификатором '{id}' не найдено.");
}
