using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Board.Contracts.Interfaces;

namespace Board.Contracts.Attributes
{
    /// <summary>
    /// Атрибут проверки строкового поля на содержание запрещённых слов.
    /// Проверяются отдельные слова, а не подстроки: «рекламой» запрещено, «рекламация» — нет.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class ForbiddenWordsValidationAttribute : ValidationAttribute
    {
        /// <summary>
        /// Максимальная длина окончания после основы запрещённого слова.
        /// </summary>
        public const int MaxEndingLength = 3;

        private static readonly Regex WordRegex = new(@"\p{L}+", RegexOptions.Compiled);

        /// <inheritdoc />
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is not string valueAsString)
            {
                return ValidationResult.Success;
            }

            // получить сервис из контекста
            var service = validationContext.GetService(typeof(IForbiddenWordsService)) as IForbiddenWordsService;
            if (service == null)
            {
                return ValidationResult.Success;
            }

            var stems = service.GetForbiddenWordStems();
            var containsForbiddenWord = WordRegex.Matches(valueAsString)
                .Select(match => match.Value.ToLowerInvariant())
                .Any(word => stems.Any(stem => IsFormOf(word, stem)));

            return containsForbiddenWord
                ? new ValidationResult("Значение содержит запрещённые слова")
                : ValidationResult.Success;
        }

        private static bool IsFormOf(string word, string stem) =>
            word.StartsWith(stem, StringComparison.Ordinal) && word.Length - stem.Length <= MaxEndingLength;
    }
}
