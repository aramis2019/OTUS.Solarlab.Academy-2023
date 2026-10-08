using Board.Application.AppData.Common;
using Board.Application.AppData.Common.Exceptions;
using Board.Application.AppData.Contexts.Accounts.Repositories;
using Board.Contracts.Account;
using Board.Domain.Account;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Board.Application.AppData.Contexts.Accounts.Services;

/// <inheritdoc cref="IAccountService" />
public class AccountService : IAccountService
{
    private readonly IAccountRepository _accountRepository;
    private readonly ICurrentUserAccessor _currentUserAccessor;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IConfiguration _configuration;

    public AccountService(
        IAccountRepository accountRepository,
        ICurrentUserAccessor currentUserAccessor,
        IPasswordHasher passwordHasher,
        IConfiguration configuration)
    {
        _accountRepository = accountRepository;
        _currentUserAccessor = currentUserAccessor;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
    }

    /// <inheritdoc />
    public async Task<Guid> RegisterAccountAsync(CreateAccountDto accountDto, CancellationToken cancellation)
    {
        var existingAccount = await _accountRepository.FindWhere(account => account.Login == accountDto.Login, cancellation);
        if (existingAccount != null)
        {
            throw new BusinessRuleException($"Пользователь с логином '{accountDto.Login}' уже зарегистрирован.");
        }

        var account = new Account
        {
            Name = accountDto.Login,
            Login = accountDto.Login,
            PasswordHash = _passwordHasher.Hash(accountDto.Password),
            Created = DateTime.UtcNow
        };

        await _accountRepository.AddAsync(account, cancellation);

        return account.Id;
    }

    /// <inheritdoc />
    public async Task<LoginResultDto> LoginAsync(LoginAccountDto accountDto, CancellationToken cancellation)
    {
        var existingAccount = await _accountRepository.FindWhere(account => account.Login == accountDto.Login, cancellation);

        // Одинаковое сообщение для неизвестного логина и неверного пароля, чтобы нельзя было перебором узнать логины.
        if (existingAccount == null || !_passwordHasher.Verify(accountDto.Password, existingAccount.PasswordHash))
        {
            throw new InvalidCredentialsException();
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, existingAccount.Id.ToString()),
            new Claim(ClaimTypes.Name, existingAccount.Login)
        };

        var adminLogins = _configuration.GetSection("Administration:AdminLogins").GetChildren().Select(c => c.Value);
        if (adminLogins.Contains(existingAccount.Login, StringComparer.OrdinalIgnoreCase))
        {
            claims.Add(new Claim(ClaimTypes.Role, Roles.Admin));
        }

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddDays(1),
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!)),
                SecurityAlgorithms.HmacSha256));

        return new LoginResultDto { Token = new JwtSecurityTokenHandler().WriteToken(token) };
    }

    /// <inheritdoc />
    public async Task<AccountDto> GetCurrentAsync(CancellationToken cancellation)
    {
        var id = _currentUserAccessor.GetCurrentAccountId()
            ?? throw new InvalidOperationException("Метод доступен только аутентифицированному пользователю.");

        var user = await _accountRepository.FindById(id, cancellation);
        if (user == null)
        {
            throw new EntityNotFoundException($"Не найден пользователь с идентификатором '{id}'.");
        }

        return new AccountDto
        {
            Id = user.Id,
            Login = user.Login
        };
    }
}
