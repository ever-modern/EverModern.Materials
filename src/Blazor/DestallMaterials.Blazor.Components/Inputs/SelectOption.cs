namespace DestallMaterials.Blazor.Components.Inputs;

public record struct SelectOption<T>(T Value, bool Disabled)
{
    public static implicit operator SelectOption<T>(T value) => new SelectOption<T>(value, false);
    public static implicit operator T(SelectOption<T> option) => option.Value;
}
