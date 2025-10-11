using System;

namespace DestallMaterials.Chronos
{
    class ChronosCallback
    {
        public DateTimeOffset AssociatedTime { get; }
        public Func<bool> Callback { get; }

        public ChronosCallback(DateTimeOffset associatedTime, Func<bool> callback)
        {
            AssociatedTime = associatedTime;
            Callback = callback;
        }
    }
}
