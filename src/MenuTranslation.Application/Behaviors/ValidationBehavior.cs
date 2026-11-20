using FluentValidation;
using FluentValidation.Results;

using Mediator;

using MenuTranslation.Domain.Common;

namespace MenuTranslation.Application.Behaviors;

/// <summary>
/// Mesajın FluentValidation doğrulayıcılarını çalıştırır. Hata varsa handler'a gitmeden <see cref="ValidationError"/>
/// içeren başarısız bir sonuç döner. Yalnızca <see cref="Result"/> ya da <see cref="Result{TValue}"/> döndüren
/// mesajlara uygulanır.
/// </summary>
public sealed class ValidationBehavior<TMessage, TResponse>(IEnumerable<IValidator<TMessage>> validators)
    : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage
    where TResponse : IFailureFactory<TResponse>
{
    public async ValueTask<TResponse> Handle(
        TMessage message,
        MessageHandlerDelegate<TMessage, TResponse> next,
        CancellationToken cancellationToken)
    {
        IValidator<TMessage>[] messageValidators = validators.ToArray();
        if (messageValidators.Length == 0)
        {
            return await next(message, cancellationToken);
        }

        ValidationResult[] results = await Task.WhenAll(
            messageValidators.Select(validator => validator.ValidateAsync(message, cancellationToken)));

        Dictionary<string, string[]> errors = results
            .SelectMany(result => result.Errors)
            .GroupBy(failure => failure.PropertyName)
            .ToDictionary(group => group.Key, group => group.Select(failure => failure.ErrorMessage).Distinct().ToArray());

        return errors.Count > 0
            ? TResponse.Failure(new ValidationError(errors))
            : await next(message, cancellationToken);
    }
}
