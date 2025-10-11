using MudBlazor;

namespace DestallMaterials.Blazor.Components.Universal;

public class ButtonConfiguration
{
    public ButtonConfiguration()
    {
    }

    public bool Disabled { get; set; } = true;

    public Func<CancellationToken, Task> Callback { get; init; } = (ct) => Task.CompletedTask;

    public string ActionName { get; init; } = "";

    public string Icon { get; init; } = "";

    public Color Color { get; init; } = Color.Info;

    public bool Hidden { get; set; }

    public TimeSpan ShowStateFor { get; set; } = TimeSpan.FromSeconds(1);

    public Action AfterSuccess { get; set; } = () => { };

    public string Style { get; set; } = "";

    public string CssClass { get; set; } = "";
}
