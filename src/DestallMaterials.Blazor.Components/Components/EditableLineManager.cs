namespace DestallMaterials.Blazor.Components;

internal class EditableLineManager<TLineModel>
{
    public EditableLineManager(TLineModel currentValue, int checkSum)
    {
        CurrentValue = currentValue;
        PreviousValue = currentValue;
        CheckSum = checkSum;
    }

    public TLineModel CurrentValue { get; set; }

    public TLineModel PreviousValue { get; set; }

    public Func<CancellationToken, Task> Save { get; set; } = (ct) => Task.CompletedTask;

    public Func<CancellationToken, Task> Delete { get; set; } = (ct) => Task.CompletedTask;

    public Action RevertChanges { get; set; } = () => { };

    public Action Refresh { get; set; } = () => { };

    public int CheckSum { get; set; }

    public bool IsBeingEdited { get; set; }

    public bool NewlyAdded { get; set; }
}
