using Hangfire;
using Hangfire.PostgreSql;
using WebTemplate.Domain.Interfaces;
using WebTemplate.Domain.Interfaces.RepositoryContracts;
using WebTemplate.Infrastructure.Configuration;
using WebTemplate.Infrastructure.Persistence;
using WebTemplate.Infrastructure.Repository;
using WebTemplate.Infrastructure.Services;
using WebTemplate.Infrastructure.Services.Caching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace WebTemplate.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING")
            ?? throw new InvalidOperationException("CONNECTION_STRING not found in environment variables");

        // Database - using environment variable
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
            });
        });

        // JWT Settings - using environment variables
        services.Configure<JwtSettings>(options =>
        {
            options.SecretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY")
                ?? throw new InvalidOperationException("JWT_SECRET_KEY not found");
            options.Issuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "WebTemplate.Api";
            options.Audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "WebTemplate.Client";
            options.ExpiryMinutes = int.TryParse(Environment.GetEnvironmentVariable("JWT_EXPIRY_MINUTES"), out var expiry)
                ? expiry : 60;
            options.RefreshTokenExpiryDays = int.TryParse(Environment.GetEnvironmentVariable("JWT_REFRESH_TOKEN_EXPIRY_DAYS"), out var refreshExpiry)
                ? refreshExpiry : 7;
        });

        // Email Settings - using environment variables
        services.Configure<EmailSettings>(options =>
        {
            options.SmtpServer = Environment.GetEnvironmentVariable("SMTP_SERVER") ?? "smtp.gmail.com";
            options.SmtpPort = int.TryParse(Environment.GetEnvironmentVariable("SMTP_PORT"), out var port) ? port : 587;
            options.SenderEmail = Environment.GetEnvironmentVariable("SMTP_SENDER_EMAIL") ?? "";
            options.SenderName = Environment.GetEnvironmentVariable("SMTP_SENDER_NAME") ?? "WebTemplate";
            options.Username = Environment.GetEnvironmentVariable("SMTP_USERNAME") ?? "";
            options.Password = Environment.GetEnvironmentVariable("SMTP_PASSWORD") ?? "";
            options.EnableSsl = bool.TryParse(Environment.GetEnvironmentVariable("SMTP_ENABLE_SSL"), out var enableSsl) ? enableSsl : true;
        });

        // Caching - Redis or InMemory based on environment variable
        var redisConnectionString = Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING");
        if (!string.IsNullOrEmpty(redisConnectionString))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnectionString;
                options.InstanceName = "WebTemplate:";
            });
            services.AddSingleton<ICacheService, RedisCacheService>();
        }
        else
        {
            services.AddMemoryCache();
            services.AddSingleton<ICacheService, MemoryCacheService>();
        }

        // Hangfire for background jobs
        var enableHangfire = bool.TryParse(Environment.GetEnvironmentVariable("ENABLE_HANGFIRE"), out var hangfire) && hangfire;
        if (enableHangfire)
        {
            services.AddHangfire(config => config
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UsePostgreSqlStorage(options => options.UseNpgsqlConnection(connectionString)));

            services.AddHangfireServer(options =>
            {
                options.WorkerCount = Environment.ProcessorCount;
            });
        }

        // Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();

        // Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Services
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IEmailService, EmailService>();

        return services;
    }
}
