namespace Board.Contracts.Account
{
    /// <summary>
    /// Информация об аккаунте.
    /// </summary>
    public class AccountDto
    {
        /// <summary>
        /// Идентификатор.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Логин.
        /// </summary>
        public string Login { get; set; } = string.Empty;
    }
}
