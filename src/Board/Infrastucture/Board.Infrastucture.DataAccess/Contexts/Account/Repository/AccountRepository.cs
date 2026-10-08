using System.Linq.Expressions;
using AutoMapper;
using Board.Application.AppData.Common.Exceptions;
using Board.Application.AppData.Contexts.Accounts.Repositories;
using Board.Infrastucture.Repository;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Board.Infrastucture.DataAccess.Contexts.Account.Repository;

/// <inheritdoc cref="IAccountRepository"/>
public class AccountRepository : IAccountRepository
{
    private readonly IRepository<Domain.Account.Account> _repository;
    private readonly IMapper _mapper;

    public AccountRepository(IRepository<Domain.Account.Account> repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    /// <inheritdoc/>
    public async Task<Guid> AddAsync(Domain.Account.Account model, CancellationToken cancellation)
    {
        try
        {
            await _repository.AddAsync(model, cancellation);
        }
        catch (DbUpdateException e) when (e.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // Проверка логина в сервисе не защищает от одновременной регистрации: её ловит уникальный индекс.
            throw new BusinessRuleException($"Пользователь с логином '{model.Login}' уже зарегистрирован.");
        }

        return model.Id;
    }

    /// <inheritdoc/>
    public Task<Domain.Account.Account> FindById(Guid id, CancellationToken cancellation)
    {
        return _repository.GetByIdAsync(id, cancellation);  
    }

    /// <inheritdoc/>
    public async Task<Domain.Account.Account> FindWhere(Expression<Func<Domain.Account.Account, bool>> predicate, CancellationToken cancellation)
    {
        return await _repository.GetAllFiltered(predicate).FirstOrDefaultAsync(cancellation);
    }
}