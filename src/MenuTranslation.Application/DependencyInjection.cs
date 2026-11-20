using FluentValidation;

using Mediator;

using MenuTranslation.Application.Behaviors;
using MenuTranslation.Application.Translations;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MenuTranslation.Application;

/// <summary>Uygulama katmanının servis kayıtları.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        // Handler'lar DbContext kullandığı için Mediator scoped çalışır.
        services.AddMediator((MediatorOptions options) =>
        {
            options.ServiceLifetime = ServiceLifetime.Scoped;
            options.PipelineBehaviors = [typeof(ValidationBehavior<,>)];
        });

        services.Configure<TranslationOptions>(configuration.GetSection(TranslationOptions.SectionName));
        services.AddScoped<TextTranslator>();
        services.AddSingleton(TimeProvider.System);
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);

        return services;
    }
}
