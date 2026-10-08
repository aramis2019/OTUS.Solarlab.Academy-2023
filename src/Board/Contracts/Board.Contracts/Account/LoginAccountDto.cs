using System.ComponentModel.DataAnnotations;

namespace Board.Contracts.Account
{
    /// <summary>
    /// Модель для входа в аккаунт.
    /// </summary>
    public class LoginAccountDto
    {
        /// <summary>
        /// Логин.
        /// </summary>
        [Required(ErrorMessage = "Логин не указан")]
        public string Login { get; set; } = string.Empty;

        /// <summary>
        /// Пароль.
        /// </summary>
        [Required(ErrorMessage = "Пароль не указан")]
        public string Password { get; set; } = string.Empty;
    }
}