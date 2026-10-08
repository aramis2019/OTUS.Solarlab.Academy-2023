using AutoMapper;
using Board.Application.AppData.Common;
using Board.Application.AppData.Contexts.Accounts.Repositories;
using Board.Application.AppData.Contexts.Accounts.Services;
using Board.Application.AppData.Contexts.Adverts.Repositories;
using Board.Application.AppData.Contexts.Adverts.Services;
using Board.Application.AppData.Contexts.Categories.Repositories;
using Board.Application.AppData.Contexts.Categories.Services;
using Board.Application.AppData.Contexts.Files.Repositories;
using Board.Application.AppData.Contexts.Files.Services;
using Board.Application.AppData.Services;
using Board.Contracts;
using Board.Contracts.Advert;
using Board.Contracts.Interfaces;
using Board.Host.Api.Middlewares;
using Board.Host.Api.Options;
using Board.Host.Api.Services;
using Board.Infrastucture.DataAccess;
using Board.Infrastucture.DataAccess.Contexts.Account.Repository;
using Board.Infrastucture.DataAccess.Contexts.Advert.Repository;
using Board.Infrastucture.DataAccess.Contexts.Category.Repository;
using Board.Infrastucture.DataAccess.Contexts.Files.Repository;
using Board.Infrastucture.DataAccess.Interfaces;
using Board.Infrastucture.MapProfiles;
using Board.Infrastucture.Repository;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Добавляем DbContext
builder.Services.AddSingleton<IDbContextOptionsConfigurator<BoardDbContext>, BoardDbContextConfiguration>();
        
builder.Services.AddDbContext<BoardDbContext>((Action<IServiceProvider, DbContextOptionsBuilder>)
    ((sp, dbOptions) => sp.GetRequiredService<IDbContextOptionsConfigurator<BoardDbContext>>()
        .Configure((DbContextOptionsBuilder<BoardDbContext>)dbOptions)));

builder.Services.AddScoped((Func<IServiceProvider, DbContext>) (sp => sp.GetRequiredService<BoardDbContext>()));

// Add repositories to the container.
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<IFileRepository, FileRepository>();
builder.Services.AddScoped<IAdvertRepository, AdvertRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();

// Add services to the container.
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddScoped<IAdvertService, AdvertService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IForbiddenWordsService, ForbiddenWordsService>();
builder.Services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
builder.Services.AddScoped<ICurrentUserAccessor, HttpContextCurrentUserAccessor>();

// AutoMapper 15+ распространяется по коммерческой лицензии: ключ задаётся в AutoMapper:LicenseKey
// (user-secrets / переменная окружения AutoMapper__LicenseKey). Без ключа AutoMapper работает, но пишет предупреждение в лог.
builder.Services.AddAutoMapper(cfg =>
{
    cfg.LicenseKey = builder.Configuration["AutoMapper:LicenseKey"];
    cfg.AddProfile<CategoryProfile>();
    cfg.AddProfile<AdvertProfile>();
    cfg.AddProfile<FileProfile>();
});

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        // Ошибки валидации модели возвращаем в том же формате ErrorDto, что и остальные ошибки.
        options.InvalidModelStateResponseFactory = context => new BadRequestObjectResult(new ErrorDto
        {
            ErrorCode = "validation_error",
            UserMessage = "Модель данных запроса невалидна.",
            InternalErrors = context.ModelState
                .Where(entry => entry.Value!.Errors.Count > 0)
                .SelectMany(entry => entry.Value!.Errors.Select(error => new ErrorDto
                {
                    ErrorCode = entry.Key,
                    UserMessage = string.IsNullOrEmpty(error.ErrorMessage) ? "Некорректное значение." : error.ErrorMessage
                }))
                .ToArray()
        });
    });

// Размер файла проверяется в FileController; лимит multipart — жёсткая граница с запасом на заголовки формы.
builder.Services.AddOptions<FileUploadOptions>().BindConfiguration(FileUploadOptions.SectionName);
builder.Services.AddOptions<FormOptions>().Configure<IOptions<FileUploadOptions>>((options, upload) =>
    options.MultipartBodyLengthLimit = upload.Value.MaxFileSizeBytes + 64 * 1024);

// Защита входа и регистрации от перебора: ограничение числа запросов с одного IP.
builder.Services.AddOptions<AuthRateLimitOptions>().BindConfiguration(AuthRateLimitOptions.SectionName);
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy(AuthRateLimitOptions.PolicyName, httpContext =>
    {
        var limits = httpContext.RequestServices.GetRequiredService<IOptions<AuthRateLimitOptions>>().Value;
        return RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = limits.PermitLimit,
                Window = TimeSpan.FromSeconds(limits.WindowSeconds),
                QueueLimit = 0
            });
    });
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString();
        }
        await context.HttpContext.Response.WriteAsJsonAsync(new ErrorDto
        {
            ErrorCode = "too_many_requests",
            UserMessage = "Слишком много попыток. Повторите позже."
        }, cancellationToken);
    };
});

builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>())
    .AllowAnyMethod()
    .AllowAnyHeader()));

#region Authentication & Authorization

builder.Services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

// Настройки читаются лениво из итоговой конфигурации, чтобы ключ можно было задать
// через переменные окружения, user-secrets или переопределить в тестах.
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IConfiguration>((options, configuration) =>
    {
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = configuration["Jwt:Audience"],
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(GetJwtKey(configuration)))
        };
    });

builder.Services.AddAuthorization();

#endregion

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Advert Api", Version = "V1" });
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, $"{typeof(CreateAdvertDto).Assembly.GetName().Name}.xml"));
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, $"{typeof(Program).Assembly.GetName().Name}.xml"));

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT из ответа POST /Account/login (без префикса 'Bearer').",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
    });
});

var app = builder.Build();

// Падаем при старте, а не на первом запросе, если ключ подписи JWT не задан или маппинги некорректны.
GetJwtKey(app.Configuration);
app.Services.GetRequiredService<IMapper>().ConfigurationProvider.AssertConfigurationIsValid();

// Configure the HTTP request pipeline.
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHsts();
}

app.UseCors();

app.UseRateLimiter();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

static string GetJwtKey(IConfiguration configuration)
{
    var key = configuration["Jwt:Key"];
    if (string.IsNullOrWhiteSpace(key) || Encoding.UTF8.GetByteCount(key) < 32)
    {
        throw new InvalidOperationException(
            "Не задан ключ подписи JWT 'Jwt:Key' (минимум 32 байта). " +
            "Задайте его через user-secrets или переменную окружения Jwt__Key.");
    }
    return key;
}

public partial class Program {}