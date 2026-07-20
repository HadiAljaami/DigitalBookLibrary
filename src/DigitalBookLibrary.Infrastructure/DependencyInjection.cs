using DigitalBookLibrary.Application.Interfaces;
using DigitalBookLibrary.Domain.Interfaces;
using DigitalBookLibrary.Infrastructure.Auditing;
using DigitalBookLibrary.Infrastructure.Files;
using DigitalBookLibrary.Infrastructure.Identity;
using DigitalBookLibrary.Infrastructure.Persistence;
using DigitalBookLibrary.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DigitalBookLibrary.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            // The audit interceptor is scoped (it reads the current user), so the DbContext is built
            // from the request's service provider.
            services.AddScoped<AuditSaveChangesInterceptor>();
            services.AddDbContext<AppDbContext>((serviceProvider, options) => options
                .UseSqlServer(connectionString)
                .AddInterceptors(serviceProvider.GetRequiredService<AuditSaveChangesInterceptor>()));

            // Data access
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IBookRepository, BookRepository>();
            services.AddScoped<IAuthorRepository, AuthorRepository>();
            services.AddScoped<ICommentRepository, CommentRepository>();
            services.AddScoped<ISavedBookRepository, SavedBookRepository>();
            services.AddScoped<IBookActivityRepository, BookActivityRepository>();
            services.AddScoped<IDashboardRepository, DashboardRepository>();

            // Identity / auth (bound manually to avoid a config-binder dependency in this layer)
            var jwtOptions = new JwtOptions
            {
                Issuer = configuration[$"{JwtOptions.SectionName}:Issuer"] ?? string.Empty,
                Audience = configuration[$"{JwtOptions.SectionName}:Audience"] ?? string.Empty,
                SecretKey = configuration[$"{JwtOptions.SectionName}:SecretKey"] ?? string.Empty,
                AccessTokenMinutes = int.TryParse(configuration[$"{JwtOptions.SectionName}:AccessTokenMinutes"], out var m) ? m : 15,
                RefreshTokenDays = int.TryParse(configuration[$"{JwtOptions.SectionName}:RefreshTokenDays"], out var d) ? d : 7
            };
            services.AddSingleton(Options.Create(jwtOptions));
            services.AddSingleton<IPasswordHasher, PasswordHasher>();
            services.AddSingleton<IJwtProvider, JwtProvider>();

            // File storage
            var fileOptions = new FileStorageOptions
            {
                RootPath = configuration[$"{FileStorageOptions.SectionName}:RootPath"] ?? "App_Data/uploads"
            };
            services.AddSingleton(Options.Create(fileOptions));
            services.AddSingleton<IFileStorageService, LocalFileStorageService>();

            // Seeding
            var seedOptions = new SeedOptions
            {
                AdminUsername = configuration[$"{SeedOptions.SectionName}:AdminUsername"] ?? "admin",
                AdminEmail = configuration[$"{SeedOptions.SectionName}:AdminEmail"] ?? "admin@digitalbooklibrary.local",
                AdminPassword = configuration[$"{SeedOptions.SectionName}:AdminPassword"] ?? "Admin#12345"
            };
            services.AddSingleton(Options.Create(seedOptions));
            services.AddScoped<DatabaseSeeder>();

            return services;
        }
    }
}
