using System.Diagnostics.CodeAnalysis;

namespace MenuTranslation.Domain.Common;

/// <summary>Hatanın türü; Api katmanı bunu HTTP durum koduna çevirir.</summary>
public enum ErrorType
{
    Validation,
    NotFound,
    Conflict
}

/// <summary>Beklenen bir hata. İstisna yerine sonuç olarak döner.</summary>
public record Error(string Code, string Message, ErrorType Type);

/// <summary>Doğrulama hataları: alan adı → mesajlar.</summary>
public sealed record ValidationError(IReadOnlyDictionary<string, string[]> Errors)
    : Error("Validation", "Bir veya daha fazla alan geçersiz.", ErrorType.Validation);

/// <summary>Pipeline davranışlarının (ör. doğrulama) sonuç tipinden bağımsız olarak hata üretebilmesi için.</summary>
public interface IFailureFactory<TSelf>
    where TSelf : IFailureFactory<TSelf>
{
    static abstract TSelf Failure(Error error);
}

/// <summary>Değer taşımayan işlem sonucu.</summary>
public sealed class Result : IFailureFactory<Result>
{
    private Result(Error? error) => Error = error;

    public Error? Error { get; }

    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccess => Error is null;

    public static Result Success() => new(null);

    public static Result Failure(Error error) => new(error);

    public static implicit operator Result(Error error) => Failure(error);
}

/// <summary>Başarılıysa <typeparamref name="TValue"/> taşıyan işlem sonucu.</summary>
public sealed class Result<TValue> : IFailureFactory<Result<TValue>>
{
    private readonly TValue? _value;

    private Result(TValue? value, Error? error)
    {
        _value = value;
        Error = error;
    }

    public Error? Error { get; }

    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccess => Error is null;

    public TValue Value => IsSuccess ? _value! : throw new InvalidOperationException("Başarısız sonucun değeri okunamaz.");

    public static Result<TValue> Success(TValue value) => new(value, null);

    public static Result<TValue> Failure(Error error) => new(default, error);

    public static implicit operator Result<TValue>(TValue value) => Success(value);

    public static implicit operator Result<TValue>(Error error) => Failure(error);
}
