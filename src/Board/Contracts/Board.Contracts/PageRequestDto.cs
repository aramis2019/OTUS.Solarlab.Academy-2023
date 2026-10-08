using System.ComponentModel.DataAnnotations;

namespace Board.Contracts
{
    /// <summary>
    /// Параметры постраничного запроса.
    /// </summary>
    public class PageRequestDto
    {
        /// <summary>
        /// Максимальный размер страницы.
        /// </summary>
        public const int MaxTake = 100;

        /// <summary>
        /// Сколько записей пропустить.
        /// </summary>
        [Range(0, int.MaxValue, ErrorMessage = "Значение не может быть отрицательным")]
        public int Skip { get; set; }

        /// <summary>
        /// Сколько записей вернуть (от 1 до 100).
        /// </summary>
        [Range(1, MaxTake, ErrorMessage = "Размер страницы должен быть от 1 до 100")]
        public int Take { get; set; } = 20;
    }
}
