using MenuTranslation.Domain.Common;

namespace MenuTranslation.Api.Endpoints;

/// <summary>Uygulama sonuçlarını HTTP yanıtına çevirir; hatalar ProblemDetails olarak döner.</summary>
internal static class ResultExtensions
{
    public static IResult ToHttpResult(this Result result) =>
        result.IsSuccess ? Results.NoContent() : Problem(result.Error);

    public static IResult ToHttpResult<T>(this Result<T> result) =>
        result.IsSuccess ? Results.Ok(result.Value) : Problem(result.Error);

    private static IResult Problem(Error error) => error switch
    {
        ValidationError validation => Results.ValidationProblem(validation.Errors.ToDictionary(e => e.Key, e => e.Value)),
        _ => Results.Problem(
            statusCode: error.Type switch
            {
                ErrorType.NotFound => StatusCodes.Status404NotFound,
                ErrorType.Conflict => StatusCodes.Status409Conflict,
                _ => StatusCodes.Status400BadRequest
            },
            title: error.Code,
            detail: error.Message)
    };
}
