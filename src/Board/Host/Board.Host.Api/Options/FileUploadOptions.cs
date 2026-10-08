namespace Board.Host.Api.Options;

/// <summary>
/// Ограничения на загрузку файлов (секция конфигурации <c>FileUpload</c>).
/// </summary>
public class FileUploadOptions
{
    /// <summary>
    /// Имя секции конфигурации.
    /// </summary>
    public const string SectionName = "FileUpload";

    /// <summary>
    /// Максимальный размер файла в байтах.
    /// </summary>
    public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024;
}
