using System.Security.Claims;
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
var forwardedHeadersEnabled = builder.Configuration.GetValue<bool>("ForwardedHeaders:Enabled");

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
builder.Services.AddScoped<IQuoteService, QuoteService>();
builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.AddScoped<ITeamService, TeamService>();
builder.Services.AddScoped<IUserAdminService, UserAdminService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IChapterVersionService, ChapterVersionService>();
builder.Services.AddScoped<IAchievementService, AchievementService>();
builder.Services.AddScoped<IQuestService, QuestService>();
builder.Services.AddSingleton<IStorageService, StorageService>();
builder.Services.AddSingleton<IImageService, ImageService>();
builder.Services.AddSingleton<IEmailSender, NoopEmailSender>();
builder.Services.AddScoped<ILeaderboardService, LeaderboardService>();
builder.Services.AddScoped<IFollowService, FollowService>();
builder.Services.AddScoped<IRatingService, RatingService>();
builder.Services.AddScoped<IShelfService, ShelfService>();
builder.Services.AddScoped<IRecommendationService, RecommendationService>();
builder.Services.AddScoped<IRankingService, RankingService>();
builder.Services.AddScoped<IGamificationService, GamificationService>();
builder.Services.AddScoped<IAdminMetricsService, AdminMetricsService>();

// ── JWT Authentication ──────────────────────────────────────────────────────

var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var jwtKey = jwtSettings["Key"];

if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException(
        "JWT signing key is not configured. Set it via configuration or the JwtSettings__Key environment variable.");
}

var key = Encoding.UTF8.GetBytes(jwtKey);

if (key.Length < 32)
{
    throw new InvalidOperationException(
        "JWT signing key is too weak: it must be at least 32 bytes (256 bits) for HMAC-SHA256. " +
        "Set a stronger JwtSettings__Key.");
}

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
    // Revocation: проверяем актуальность пользователя на каждом валидном токене.
    // Закрывает окна для забаненных и разжалованных пользователей, а также
    // инвалидирует токены после смены пароля (через SecurityStamp).
    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = async ctx =>
        {
            var principal = ctx.Principal;
            var rawUserId = principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(rawUserId, out var userId))
            {
                ctx.Fail("Invalid token subject.");
                return;
            }

            var db = ctx.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
            var user = await db.Users
                .AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => new { u.IsBanned, u.Role, u.SecurityStamp })
                .FirstOrDefaultAsync(ctx.HttpContext.RequestAborted);

            if (user is null || user.IsBanned)
            {
                ctx.Fail("User is not allowed.");
                return;
            }

            var tokenRole = principal?.FindFirst(ClaimTypes.Role)?.Value;
            if (!string.Equals(tokenRole, user.Role, StringComparison.Ordinal))
            {
                ctx.Fail("Token role no longer matches user role.");
                return;
            }

            var tokenStamp = principal?.FindFirst(AuthService.SecurityStampClaimType)?.Value;
            if (!string.Equals(tokenStamp, user.SecurityStamp, StringComparison.Ordinal))
            {
                ctx.Fail("Security stamp mismatch.");
                return;
            }
        }
    };
})
    .AddScheme<TranslatorApiKeyAuthenticationOptions, TranslatorApiKeyAuthenticationHandler>(
    TranslatorApiKeyAuthenticationDefaults.SchemeName,
    options =>
    {
        options.ApiKey = builder.Configuration["IntegrationAuth:TranslatorApiKey"] ?? string.Empty;
    });

// ── ForwardedHeaders (Docker/Nginx reverse proxy) ─────────────────────────
if (forwardedHeadersEnabled)
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
        // Auth: лимит ПО IP (форвард-заголовки уже настроены), а не единый глобальный bucket.
        options.AddPolicy("auth", httpContext =>
        {
            var key = httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous";
            return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = builder.Configuration.GetValue<int>("RateLimiting:Auth:PermitLimit"),
                Window = TimeSpan.FromMinutes(
                    builder.Configuration.GetValue<int>("RateLimiting:Auth:WindowMinutes")),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0,
            });
        });
        // Ingest: лимит ПО IP, а не единый глобальный bucket.
        options.AddPolicy("ingest", httpContext =>
        {
            var key = httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous";
            return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = builder.Configuration.GetValue<int>("RateLimiting:Ingest:PermitLimit"),
                Window = TimeSpan.FromMinutes(
                    builder.Configuration.GetValue<int>("RateLimiting:Ingest:WindowMinutes")),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0,
            });
        });
        // Загрузка медиа: лимит ПО ПОЛЬЗОВАТЕЛЮ (партиционирование по NameIdentifier / IP).
        options.AddPolicy("media", httpContext =>
        {
            var key = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? httpContext.Connection.RemoteIpAddress?.ToString()
                ?? "anonymous";
            return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = builder.Configuration.GetValue<int?>("RateLimiting:Media:PermitLimit") ?? 20,
                Window = TimeSpan.FromMinutes(builder.Configuration.GetValue<int?>("RateLimiting:Media:WindowMinutes") ?? 1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0,
            });
        });
        options.AddPolicy("comments", httpContext =>
        {
            var key = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? httpContext.Connection.RemoteIpAddress?.ToString()
                ?? "anonymous";
            return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = builder.Configuration.GetValue<int?>("RateLimiting:Comments:PermitLimit") ?? 30,
                Window = TimeSpan.FromMinutes(builder.Configuration.GetValue<int?>("RateLimiting:Comments:WindowMinutes") ?? 1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0,
            });
        });
        // Голосования/оценки/подписки: лимит ПО ПОЛЬЗОВАТЕЛЮ (партиционирование по NameIdentifier / IP).
        options.AddPolicy("interactions", httpContext =>
        {
            var key = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? httpContext.Connection.RemoteIpAddress?.ToString()
                ?? "anonymous";
            return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = builder.Configuration.GetValue<int?>("RateLimiting:Interactions:PermitLimit") ?? 60,
                Window = TimeSpan.FromMinutes(builder.Configuration.GetValue<int?>("RateLimiting:Interactions:WindowMinutes") ?? 1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0,
            });
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

if (forwardedHeadersEnabled)
{
    app.UseForwardedHeaders();
}
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
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

public partial class Program { }
