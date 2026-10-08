using AutoMapper;
using Board.Application.AppData.Common;
using Board.Application.AppData.Common.Exceptions;
using Board.Application.AppData.Contexts.Files.Repositories;
using Board.Contracts.File;

namespace Board.Application.AppData.Contexts.Files.Services
{
    /// <inheritdoc cref="IFileService"/>
    public class FileService : IFileService
    {
        private readonly IFileRepository _fileRepository;
        private readonly IMapper _mapper;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        /// <summary>
        /// Инициализация экземпляра <see cref="FileService"/>.
        /// </summary>        
        public FileService(IFileRepository fileRepository, IMapper mapper, ICurrentUserAccessor currentUserAccessor)
        {
            _fileRepository = fileRepository;
            _mapper = mapper;
            _currentUserAccessor = currentUserAccessor;
        }

        /// <inheritdoc/>
        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
        {
            var file = await _fileRepository.FindByIdAsync(id, cancellationToken);
            if (file == null)
            {
                return;
            }

            if (!_currentUserAccessor.CanModify(file.AccountId))
            {
                throw new AccessDeniedException("Удалить файл может только тот, кто его загрузил, или администратор.");
            }

            await _fileRepository.DeleteAsync(file, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<FileDto> DownloadAsync(Guid id, CancellationToken cancellationToken)
        {
            return await _fileRepository.DownloadAsync(id, cancellationToken) ?? throw NotFound(id);
        }

        /// <inheritdoc/>
        public async Task<FileInfoDto> GetInfoByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return await _fileRepository.GetInfoByIdAsync(id, cancellationToken) ?? throw NotFound(id);
        }

        /// <inheritdoc/>
        public Task<Guid> UploadAsync(FileDto model, CancellationToken cancellationToken)
        {
            var file = _mapper.Map<FileDto, Domain.Files.File>(model);
            file.AccountId = _currentUserAccessor.GetCurrentAccountId();
            return _fileRepository.UploadAsync(file, cancellationToken);
        }

        private static EntityNotFoundException NotFound(Guid id) =>
            new($"Файл с идентификатором '{id}' не найден.");
    }
}
