namespace DestallMaterials.WheelProtection.DataStructures;

public struct Result<T>
{
    readonly T _value;
    readonly Exception? _exception;

    public Result(Exception exception) : this()
    {
        _exception = exception;
    }

    public Result(T value)
    {
        _value = value;
    }

    public static implicit operator Result<T>(T value)
        => new(value);

    public static implicit operator Result<T>(Exception exception)
        => new(exception);

    public void Deconstruct(out T value, out Exception error)
    {
        value = _value;
        error = _exception;
    }
}

public struct Result<T, TException>
    where TException : Exception
{
    readonly T _value;
    readonly TException? _exception;

    public Result(TException exception) : this()
    {
        _exception = exception;
    }

    public Result(T value)
    {
        _value = value;
    }

    public static implicit operator Result<T, TException>(T value)
        => new(value);

    public static implicit operator Result<T, TException>(TException exception)
        => new(exception);

    public void Deconstruct(out T value, out TException error)
    {
        value = _value;
        error = _exception;
    }
}
