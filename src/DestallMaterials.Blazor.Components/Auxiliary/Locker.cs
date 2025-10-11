namespace DestallMaterials.Blazor.Auxiliary;

public static class Locker
{
    static readonly bool _useLock = Environment.ProcessorCount > 1; 
    public static void Lock(object locker, Action @do)
    {
        if (_useLock)
        {
            lock (locker)
            {
                @do();
            }
        }
        else 
        {
            @do();
        }
        
    }
}
