using System.Text;
using System.Threading.RateLimiting;
using LibNode.Api.Authentication;
using Microsoft.AspNetCore.RateLimiting;
using LibNode.Api.Data;
using LibNode.Api.Middlewares;
using LibNode.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Connection string 'DefaultConnection' is not configured. Set it via configuration or the ConnectionStrings__DefaultConnection environment variable.");
}

var allowedOrigins = builder.Configuration
    .GetSection("Cors:Origins")
    .Get<string[]>()?
    .Select(origin => origin.Trim().TrimEnd('/'))
    .Where(origin => !string.IsNullOrWhiteSpace(origin))
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray();

if (allowedOrigins is null || allowedOrigins.Length == 0)
{
    allowedOrigins = ["http://localhost:3000"];
}

// ── Services ────────────────────────────────────────────────────────────────

// CORS (разрешаем запросы только с доверенных frontend origins)
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendOrigins", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});
// DbContext → PostgreSQL
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// Бизнес-логика (Scoped = по одному экземпляру на HTTP-запрос)
builder.Services.AddScoped<IBookService, BookService>();
builder.Services.AddScoped<IChapterService, ChapterService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICollectionService, CollectionService>();
builder.Services.AddScoped<IReadingProgressService, ReadingProgressService>();
builder.Services.AddScoped<ITagService, TagService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IReaderIngestService, ReaderIngestService>();

// ── JWT Authentication ──────────────────────────────────────────────────────

var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var jwtKey = jwtSettings["Key"];

if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException(
        "JWT signing key is not configured. Set it via configuration or the JwtSettings__Key environment variable.");
}

var key = Encoding.UTF8.GetBytes(jwtKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ClockSkew = TimeSpan.Zero // Без допуска по времени
    };
})
    .AddScheme<TranslatorApiKeyAuthenticationOptions, TranslatorApiKeyAuthenticationHandler>(
    TranslatorApiKeyAuthenticationDefaults.SchemeName,
    options =>
    {
        options.ApiKey = builder.Configuration["IntegrationAuth:TranslatorApiKey"] ?? string.Empty;
    });

// ── ForwardedHeaders (Docker/Nginx reverse proxy) ─────────────────────────
if (builder.Configuration.GetValue<bool>("ForwardedHeaders:Enabled"))
{
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor |
            ForwardedHeaders.XForwardedProto;
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
    });
}

// ── Rate Limiting (auth + translator ingest endpoints) ────────────────────
if (builder.Configuration.GetValue<bool>("RateLimiting:Enabled"))
{
    builder.Services.AddRateLimiter(options =>
    {
        options.AddFixedWindowLimiter("auth", opt =>
        {
            opt.PermitLimit = builder.Configuration.GetValue<int>("RateLimiting:Auth:PermitLimit");
            opt.Window = TimeSpan.FromMinutes(
                builder.Configuration.GetValue<int>("RateLimiting:Auth:WindowMinutes"));
            opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            opt.QueueLimit = 0;
        });
        options.AddFixedWindowLimiter("ingest", opt =>
        {
            opt.PermitLimit = builder.Configuration.GetValue<int>("RateLimiting:Ingest:PermitLimit");
            opt.Window = TimeSpan.FromMinutes(
                builder.Configuration.GetValue<int>("RateLimiting:Ingest:WindowMinutes"));
            opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            opt.QueueLimit = 0;
        });
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    });
}

builder.Services.AddAuthorization();

// Controllers + JSON
builder.Services.AddControllers();

// Swagger / OpenAPI (с поддержкой JWT Bearer)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "LibNode API",
        Version = "v1",
        Description = "REST API для читалки ранобэ"
    });

    // Схема авторизации Bearer для Swagger UI
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Введите JWT токен: Bearer {token}"
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
                }
            },
            Array.Empty<string>()
        }
    });

    options.AddSecurityDefinition("TranslatorApiKey", new OpenApiSecurityScheme
    {
        Name = "x-api-key",
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Description = "API key for libnode-translator publishing integration."
    });
});

var app = builder.Build();

// ── Middleware pipeline ─────────────────────────────────────────────────────

if (builder.Configuration.GetValue<bool>("Swagger:Enabled"))
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "LibNode API v1");
    });
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseForwardedHeaders();
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseCors("FrontendOrigins");
app.UseAuthentication(); // ← ПЕРЕД UseAuthorization
app.UseAuthorization();
if (builder.Configuration.GetValue<bool>("RateLimiting:Enabled"))
{
    app.UseRateLimiter();
}
app.MapControllers();

app.Run();
