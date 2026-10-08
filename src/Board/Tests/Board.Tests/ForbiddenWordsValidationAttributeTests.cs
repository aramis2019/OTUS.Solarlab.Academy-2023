using System;
using System.ComponentModel.DataAnnotations;
using Board.Application.AppData.Services;
using Board.Contracts.Attributes;
using Board.Contracts.Interfaces;
using Moq;
using Shouldly;
using Xunit;

namespace Board.Tests
{
    public class ForbiddenWordsValidationAttributeTests
    {
        private class Model
        {
            [ForbiddenWordsValidation]
            public string? Text { get; set; }
        }

        [Theory]
        [InlineData("Это реклама")]
        [InlineData("Без РЕКЛАМЫ никак")]
        [InlineData("Займусь рекламой")]
        [InlineData("дурак")]
        [InlineData("Вокруг одни дураки!")]
        [InlineData("Не беру взятками")]
        public void ForbiddenWordForm_IsRejected(string text)
        {
            IsValid(text).ShouldBeFalse();
        }

        [Theory]
        [InlineData("Подам рекламацию поставщику")]
        [InlineData("Продам велосипед")]
        [InlineData("")]
        [InlineData(null)]
        public void OrdinaryText_IsAccepted(string? text)
        {
            IsValid(text).ShouldBeTrue();
        }

        private static bool IsValid(string? text)
        {
            var services = new Mock<IServiceProvider>();
            services.Setup(s => s.GetService(typeof(IForbiddenWordsService))).Returns(new ForbiddenWordsService());
            var model = new Model { Text = text };
            var context = new ValidationContext(model, services.Object, null);
            return Validator.TryValidateObject(model, context, null, validateAllProperties: true);
        }
    }
}
