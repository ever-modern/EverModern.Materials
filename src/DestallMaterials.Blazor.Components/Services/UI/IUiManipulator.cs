namespace DestallMaterials.Blazor.Services.UI;

public struct ElementBoungingRectangle
{
    public double Top { get; init; }
    public double Left { get; init; }
    public double Right { get; init; }
    public double Bottom { get; init; }

    public double Width { get; init; }
    public double Height { get; init; }
}
public interface IUiManipulator
{
    Task<uint> Y_Deviation(string itemId, string containerId);
    Task<uint> X_Deviation(string itemId, string containerId);
    Task ScrollItem_X(string id, double XPosition);
    Task ScrollItem_Y(string id, double YPosition);
    Task ScrollToFit_Y(string itemId, string containerId);

    Task SetCssVariableValue(string elementId, string variableName, string value);
    Task<double> GetItemScroll_Y(string elementId);
    Task DisableDefaultEventHandling(string elementId, string eventType);

    Task<ElementBoungingRectangle?> GetElementBoungingRectangle(string elementId);
}
