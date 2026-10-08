namespace Board.Domain.Files
{
    /// <summary>
    /// Сущность файла.
    /// </summary>
    public class File
    {
        /// <summary>
        /// Идентификатор файла.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Имя файла.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Контент файла.
        /// </summary>
        public byte[] Content { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// ContentType файла.
        /// </summary>
        public string ContentType { get; set; } = string.Empty;

        /// <summary>
        /// Размер файла.
        /// </summary>
        public int Length { get; set; }

        /// <summary>
        /// Время создания.
        /// </summary>
        public DateTime Created { get; set; }

        /// <summary>
        /// Идентификатор аккаунта, загрузившего файл (null у файлов, загруженных до появления авторства).
        /// </summary>
        public Guid? AccountId { get; set; }
    }
}
