using System;
using Board.Domain.Adverts;
using Board.Domain.Categories;
using Board.Infrastructure.DataAccess;

namespace Board.Api.Tests
{
    public static class DataSeedHelper
    {
        // Фиксированные идентификаторы: у каждой фабрики своя БД, но сид одинаковый.
        public static readonly Guid TestAdvertId = Guid.Parse("a0000000-0000-0000-0000-000000000001");
        public static readonly Guid TestCategoryId = Guid.Parse("c0000000-0000-0000-0000-000000000001");

        public static void InitializeDbForTests(BoardDbContext db)
        {
            if (db.Find<Category>(TestCategoryId) != null)
            {
                return;
            }

            var testCategory = new Category
            {
                Id = TestCategoryId,
                Name = "test_cat_1",
                IsActive = true,
                Created = DateTime.UtcNow
            };
            db.Add(testCategory);

            var advert = new Advert
            {
                Id = TestAdvertId,
                Name = "test_advert_name",
                Description = "test_desc",
                IsActive = true,
                Created = DateTime.UtcNow,
                CategoryId = testCategory.Id,
                Address = "new_prostokvashino"
            };
            db.Add(advert);

            db.SaveChanges();
        }
    }
}