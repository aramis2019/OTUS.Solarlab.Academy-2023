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
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

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

builder.Services.AddSingleton<IMapper>(new Mapper(GetMapperConfiguration()));

builder.Services.AddControllers(options =>
    {
        // Newtonsoft нужен только для JsonPatchDocument, остальное сериализуется System.Text.Json.
        options.InputFormatters.Insert(0, GetJsonPatchInputFormatter());
    })
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
    options.IncludeXmlComments(Path.Combine(Path.Combine(AppContext.BaseDirectory,
        $"{typeof(CreateAdvertDto).Assembly.GetName().Name}.xml")));
    options.IncludeXmlComments(Path.Combine(Path.Combine(AppContext.BaseDirectory, "Documentation.xml")));

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = @"JWT Authorization header using the Bearer scheme.  
                        Enter 'Bearer' [space] and then your token in the text input below.
                        Example: 'Bearer secretKey'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = JwtBearerDefaults.AuthenticationScheme
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            { 
                Reference = new OpenApiReference
                { 
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme="oauth2",
                Name= "Bearer",
                In = ParameterLocation.Header,
            },
            new List<string>()
        }
    });
});

var app = builder.Build();

// Падаем при старте, а не на первом запросе, если ключ подписи JWT не задан.
GetJwtKey(app.Configuration);

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

static NewtonsoftJsonPatchInputFormatter GetJsonPatchInputFormatter()
{
    return new ServiceCollection()
        .AddLogging()
        .AddMvc()
        .AddNewtonsoftJson()
        .Services.BuildServiceProvider()
        .GetRequiredService<IOptions<MvcOptions>>()
        .Value.InputFormatters
        .OfType<NewtonsoftJsonPatchInputFormatter>()
        .First();
}

static MapperConfiguration GetMapperConfiguration()
{
    var configuration = new MapperConfiguration(cfg => 
    {
        cfg.AddProfile<CategoryProfile>();
        cfg.AddProfile<AdvertProfile>();
        cfg.AddProfile<FileProfile>();
    });
    configuration.AssertConfigurationIsValid();
    return configuration;
}

public partial class Program {}