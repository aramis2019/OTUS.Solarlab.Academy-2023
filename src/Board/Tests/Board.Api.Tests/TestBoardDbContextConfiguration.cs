using Board.Infrastucture.DataAccess;
using Board.Infrastucture.DataAccess.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Board.Api.Tests
{
    public class TestBoardDbContextConfiguration : IDbContextOptionsConfigurator<BoardDbContext>
    {
        private readonly string _databaseName;
        private readonly ILoggerFactory _loggerFactory;

        public TestBoardDbContextConfiguration(string databaseName, ILoggerFactory loggerFactory)
        {
            _databaseName = databaseName;
            _loggerFactory = loggerFactory;
        }

        public void Configure(DbContextOptionsBuilder<BoardDbContext> options)
        {
            options.UseInMemoryDatabase(_databaseName);
            options.UseLoggerFactory(_loggerFactory);
            options.EnableSensitiveDataLogging();
        }
    }
}