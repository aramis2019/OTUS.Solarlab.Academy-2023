using Board.Contracts.File;

namespace Board.Application.AppData.Contexts.Files.Repositories
{
    /// <summary>
    /// Репозиторий файлов.
    /// </summary>
    public interface IFileRepository
    {
        /// <summary>
        /// Получение информации о файле по его идентификатору.
        /// </summary>
        /// <param name="id">Идентификатор файла.</param>
        /// <param name="cancellationToken">Токен отмены.</param>
        /// <returns>Информация о файле.</returns>
        Task<FileInfoDto> GetInfoByIdAsync(Guid id, CancellationToken cancellationToken);

        /// <summary>
        /// Загрузка файла в систему.
        /// </summary>
        /// <param name="model">Модель файла.</param>
        /// <param name="cancellationToken">Токен отмены.</param>
        /// <returns>Идентификатор загруженного файла.</returns>
        Task<Guid> UploadAsync(Domain.Files.File model, CancellationToken cancellationToken);

        /// <summary>
        /// Скачивание файла.
        /// </summary>
        /// <param name="id">Идентификатор файла.</param>
        /// <param name="cancellationToken">Токен отмены.</param>
        /// <returns>Информация о скачиваемом файле.</returns>
        Task<FileDto> DownloadAsync(Guid id, CancellationToken cancellationToken);

        /// <summary>
        /// Найти сущность файла по идентификатору.
        /// </summary>
        /// <param name="id">Идентификатор файла.</param>
        /// <param name="cancellationToken">Токен отмены.</param>
        /// <returns>Файл или null, если не найден.</returns>
        Task<Domain.Files.File?> FindByIdAsync(Guid id, CancellationToken cancellationToken);

        /// <summary>
        /// Удаление файла.
        /// </summary>
        /// <param name="file">Файл.</param>
        /// <param name="cancellationToken">Токен отмены.</param>
        Task DeleteAsync(Domain.Files.File file, CancellationToken cancellationToken);
    }
}
