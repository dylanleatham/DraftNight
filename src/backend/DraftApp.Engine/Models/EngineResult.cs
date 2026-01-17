using DraftApp.Engine.Errors;

namespace DraftApp.Engine.Models;

/// <summary>
/// Discriminated union representing either a successful result or an error.
/// </summary>
public abstract record EngineResult<T>
{
    private EngineResult()
    {
    }

    /// <summary>
    /// Gets a value indicating whether this is a successful result.
    /// </summary>
    public bool IsSuccess => this is Success;

    /// <summary>
    /// Gets a value indicating whether this is a failed result.
    /// </summary>
    public bool IsFailure => this is Failure;

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static EngineResult<T> Ok(T value) => new Success(value);

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static EngineResult<T> Fail(EngineError error) => new Failure(error);

    /// <summary>
    /// Maps a successful result to a new type.
    /// </summary>
    public EngineResult<TNew> Map<TNew>(Func<T, TNew> mapper) => this switch
    {
        Success s => EngineResult<TNew>.Ok(mapper(s.Value)),
        Failure f => EngineResult<TNew>.Fail(f.Error),
        _ => throw new InvalidOperationException("Unknown result type")
    };

    /// <summary>
    /// Chains another operation that returns a result.
    /// </summary>
    public EngineResult<TNew> Bind<TNew>(Func<T, EngineResult<TNew>> binder) => this switch
    {
        Success s => binder(s.Value),
        Failure f => EngineResult<TNew>.Fail(f.Error),
        _ => throw new InvalidOperationException("Unknown result type")
    };

    /// <summary>
    /// Gets the value or throws if this is a failure.
    /// </summary>
    public T GetValueOrThrow() => this switch
    {
        Success s => s.Value,
        Failure f => throw new InvalidOperationException($"Result is a failure: {f.Error.Message}"),
        _ => throw new InvalidOperationException("Unknown result type")
    };

    /// <summary>
    /// Gets the value or returns a default value.
    /// </summary>
    public T GetValueOrDefault(T defaultValue) => this switch
    {
        Success s => s.Value,
        _ => defaultValue
    };

    /// <summary>
    /// Performs pattern matching on the result.
    /// </summary>
    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<EngineError, TResult> onFailure) => this switch
    {
        Success s => onSuccess(s.Value),
        Failure f => onFailure(f.Error),
        _ => throw new InvalidOperationException("Unknown result type")
    };

    /// <summary>
    /// Successful result containing a value.
    /// </summary>
    public sealed record Success(T Value) : EngineResult<T>;

    /// <summary>
    /// Failed result containing an error.
    /// </summary>
    public sealed record Failure(EngineError Error) : EngineResult<T>;
}
