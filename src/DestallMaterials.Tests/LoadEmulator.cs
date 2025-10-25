using DestallMaterials.WheelProtection.Queues;

namespace DestallMaterials.Tests;



partial class LoadEmulator
{
    Recycler<RequestProcessor> _recycler;

    public LoadEmulator(int capacity = 1)
    {
        _recycler = new RequestsProcessorRecycler(capacity);
    }

    public async Task<long> ProcessRequestAsync(TimeSpan operationExecutionLength)
    {
        using var processorLocker = await _recycler.Another();
        await processorLocker.Item.ProcessRequestAsync(operationExecutionLength);
        return operationExecutionLength.Ticks;
    }

    class RequestProcessor : IDisposable
    {
        bool _isBusy = false;

        public void Cancel()
        {

        }

        public async Task ProcessRequestAsync(TimeSpan operationExecutionLength)
        {
            if (_isBusy)
            {
                throw new InvalidOperationException();
            }
            _isBusy = true;
            await Task.Delay(operationExecutionLength);
            _isBusy = false;
        }
    }

    class RequestsProcessorRecycler : Recycler<RequestProcessor>
    {
        public RequestsProcessorRecycler(int capacity = 1) : base(capacity)
        {
        }

        protected override bool TryCreateNew(out RequestProcessor requestProcessor)
        {
            requestProcessor = new();
            return true;
        }

        protected override void Discard(RequestProcessor item)
        => item.Cancel();

        protected override bool IsWell(RequestProcessor item)
        => true;
    }
}
