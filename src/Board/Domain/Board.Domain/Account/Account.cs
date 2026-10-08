namespace Board.Domain.Account
{
    /// <summary>
    /// Акаунт
    /// </summary>
    public class Account
    {
        /// <summary>
        /// Идентификатор.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Имя пользователя.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Логин пользователя.
        /// </summary>
        public string Login { get; set; }

        /// <summary>
        /// Логин в верхнем регистре: по нему ищется аккаунт и обеспечивается уникальность без учёта регистра.
        /// </summary>
        public string NormalizedLogin { get; set; }

        /// <summary>
        /// Привести логин к виду для поиска и сравнения.
        /// </summary>
        public static string NormalizeLogin(string login) => login.Trim().ToUpperInvariant();
        
        /// <summary>
        /// Хеш пароля (PBKDF2, вместе с солью и числом итераций).
        /// </summary>
        public string PasswordHash { get; set; }

        /// <summary>
        /// Дата регистрации.
        /// </summary>
        public DateTime Created { get; set; }
    }
}