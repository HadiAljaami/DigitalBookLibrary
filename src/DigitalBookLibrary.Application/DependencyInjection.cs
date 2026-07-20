using DigitalBookLibrary.Application.Services;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace DigitalBookLibrary.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            // No AutoMapper — mapping is manual via extension methods (Mapping/*.cs).
            // Register all FluentValidation validators found in this assembly.
            services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

            // Concrete services (no IService interfaces — single implementation each).
            services.AddScoped<AuthService>();
            services.AddScoped<AuthorService>();
            services.AddScoped<CategoryService>();
            services.AddScoped<BookService>();
            services.AddScoped<RatingService>();
            services.AddScoped<SavedBookService>();
            services.AddScoped<CommentService>();
            services.AddScoped<BookActivityService>();
            services.AddScoped<DashboardService>();

            return services;
        }
    }
}
