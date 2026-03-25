namespace EverModern.Blazor.Components.Containers;

public abstract record DropdownMenuOption(string Text)
{
    public record Link(string Text, string Href) : DropdownMenuOption(Text);

    public record Button(string Text, Action OnClick) : DropdownMenuOption(Text);
}
