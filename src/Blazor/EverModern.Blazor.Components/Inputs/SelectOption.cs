namespace EverModern.Blazor.Components.Inputs;

public record struct SelectOption<T>(T Value, string Name, bool Disabled)
{
    public static implicit operator SelectOption<T>(T value) =>
        new(value, value?.ToString() ?? "", false);

    public static implicit operator T(SelectOption<T> option) => option.Value;
}
