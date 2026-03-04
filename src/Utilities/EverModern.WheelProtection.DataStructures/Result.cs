namespace EverModern.WheelProtection.DataStructures;

/// <summary>
/// Represents a result that may contain a value or an exception.
/// </summary>
/// <typeparam name="T">The value type.</typeparam>
public readonly struct Result<T>
{
    readonly T _value;
    readonly Exception? _exception;

    /// <summary>
    /// Initializes a result with an exception.
    /// </summary>
    /// <param name="exception">The exception.</param>
    public Result(Exception exception) : this()
    {
        _exception = exception;
    }

    /// <summary>
    /// Initializes a result with a value.
    /// </summary>
    /// <param name="value">The value.</param>
    public Result(T value)
    {
        _value = value;
    }

    /// <summary>
    /// Converts a value to a result.
    /// </summary>
    /// <param name="value">The value.</param>
    public static implicit operator Result<T>(T value)
        => new(value);

    /// <summary>
    /// Converts an exception to a result.
    /// </summary>
    /// <param name="exception">The exception.</param>
    public static implicit operator Result<T>(Exception exception)
        => new(exception);

    /// <summary>
    /// Deconstructs the result into value and error.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="error">The exception.</param>
    public void Deconstruct(out T value, out Exception error)
    {
        value = _value;
        error = _exception;
    }
}

/// <summary>
/// Represents a result that may contain a value or a specific exception type.
/// </summary>
/// <typeparam name="T">The value type.</typeparam>
/// <typeparam name="TException">The exception type.</typeparam>
public readonly struct Result<T, TException>
    where TException : Exception
{
    readonly T _value;
    readonly TException? _exception;

    /// <summary>
    /// Initializes a result with an exception.
    /// </summary>
    /// <param name="exception">The exception.</param>
    public Result(TException exception) : this()
    {
        _exception = exception;
    }

    /// <summary>
    /// Initializes a result with a value.
    /// </summary>
    /// <param name="value">The value.</param>
    public Result(T value)
    {
        _value = value;
    }

    /// <summary>
    /// Converts a value to a result.
    /// </summary>
    /// <param name="value">The value.</param>
    public static implicit operator Result<T, TException>(T value)
        => new(value);

    /// <summary>
    /// Converts an exception to a result.
    /// </summary>
    /// <param name="exception">The exception.</param>
    public static implicit operator Result<T, TException>(TException exception)
        => new(exception);

    /// <summary>
    /// Deconstructs the result into value and error.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="error">The exception.</param>
    public void Deconstruct(out T value, out TException error)
    {
        value = _value;
        error = _exception;
    }
}
