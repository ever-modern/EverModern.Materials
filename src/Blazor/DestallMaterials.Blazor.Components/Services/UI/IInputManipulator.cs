namespace DestallMaterials.Blazor.Components.Services.UI;

public interface IInputManipulator
{
    Task BlurAsync(string inputId, CancellationToken cancellationToken = default);
    Task<uint> GetCarretPositionAsync(
        string inputId,
        CancellationToken cancellationToken = default
    );
    Task SetCaretPositionAsync(
        string inputId,
        uint position,
        CancellationToken cancellationToken = default
    );
    Task SetSelectionRangeAsync(
        string inputId,
        uint start,
        uint end,
        CancellationToken cancellationToken = default
    );

    Task SetInputValueAsync(
        string inputId,
        string value,
        CancellationToken cancellationToken = default
    );
    Task<Subscription> OnChange(
        string inputId,
        Func<JsInputManipulator.TextInputState, JsInputManipulator.TextInputState> processState
    );
}
