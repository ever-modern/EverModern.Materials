namespace DestallMaterials.WheelProtection.DataStructures;

public readonly struct Nothing
{
    public static Nothing Instance { get; } = default;

    [Obsolete("Don't create new instances. Use the static 'Instance' property.", true)]
    public Nothing()
    {
    }
}

public readonly struct Nothing<T>
{
    public static Nothing<T> Instance { get; } = default;

    [Obsolete("Don't create new instances. Use the static 'Instance' property.", true)]
    public Nothing()
    {
    }
}