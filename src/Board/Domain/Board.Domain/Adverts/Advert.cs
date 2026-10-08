using Board.Domain.Categories;

namespace Board.Domain.Adverts;

/// <summary>
/// Объявление.
/// </summary>
public class Advert
{
    /// <summary>
    /// Идентификатор.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Наименование.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Описание.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Цена.
    /// </summary>
    public decimal? Price { get; set; }

    /// <summary>
    /// Ссылка на изображение.
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Полный адрес.
    /// </summary>
    public string Address { get; set; } = string.Empty;
    
    /// <summary>
    /// Признак актуальности.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Дата/время создания (UTC).
    /// </summary>
    public DateTime Created { get; set; }

    /// <summary>
    /// Идентификатор категории.
    /// </summary>
    public Guid CategoryId { get; set; }

    /// <summary>
    /// Категория.
    /// </summary>
    public virtual Category Category { get; set; } = null!;

    /// <summary>
    /// Идентификатор аккаунта автора (null у объявлений, созданных до появления авторства).
    /// </summary>
    public Guid? AccountId { get; set; }
}