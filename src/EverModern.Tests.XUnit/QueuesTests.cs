using EverModern.Chronos;
using EverModern.Threading.Queues;
using EverModern.WheelProtection.Extensions.Tasks;

namespace EverModern.Tests.XUnit;

public class QueuesTests
{
    LoadEmulator _serverEmulator = new LoadEmulator(10);

    [Fact]
    public async Task Simple()
    {
        var rand = new Random(10);

        await Task.WhenAll([.. Enumerable.Range(0, 10).Select(i => Task.Run(async () =>
        {
            var payload = TimeSpan.FromMilliseconds(rand.Next(100) * 10);
            var response = await _serverEmulator.ProcessRequestAsync(payload);
            Console.WriteLine($"{DateTime.Now} ===> Request processed with response {response}.");
        }))]);
    }

    class TestedRecycler : Recycler<object>
    {
        int _itemNumber;
        public TestedRecycler(int maxPoolSize) : base(maxPoolSize)
        {
        }

        protected override void Discard(object item)
        {
        }

        protected override bool IsWell(object item)
            => true;

        protected override bool TryCreateNew(out object item)
        {
            item = _itemNumber++;
            return true;
        }
    }

    [Fact]
    public async Task Recycler()
    {
        var testedRecycler = new TestedRecycler(5);

        var rand = new Random(10);

        await Task.WhenAll([.. Enumerable.Range(0, 10).Select(i => Task.Run(async () =>
        {
            var payload = TimeSpan.FromMilliseconds(rand.Next(100) * 10);
            var response = await _serverEmulator.ProcessRequestAsync(payload);
            Console.WriteLine($"{DateTime.Now} ===> Request processed with response {response}.");
        }))]);
    }

    [Fact]
    public async Task Distributor_OrderlyTest()
    {
        var log = (string m) => Console.WriteLine(m);
        CallConstraint[] samples =
        [
            new(TimeSpan.FromSeconds(5), 1),
            new(TimeSpan.FromSeconds(10), 1),
            new(TimeSpan.FromSeconds(20), 2),
            new(TimeSpan.FromSeconds(100), 5)
        ];

        foreach (var constraint in samples.Skip(2).Take(1))
        {
            log($"Processing constraint {constraint.Period}:{constraint.MaxCallsCount}");
            
            var timePace = TimeSpan.FromMilliseconds(constraint.Period.TotalMilliseconds - 1);

            var nowProvider = new ManualChronos(
                relativeSpeed: 0,
                initialTime: new DateTime(2000, 1, 1));

            var moveTimeForward = (TimeSpan add)
                => nowProvider.MoveTime(add);

            var agents = Enumerable.Range(1, 5);

            CallConstraint[] commonConstraints = [constraint];

            var distributor = new RatesDistributor<int>(
                agents.ToDictionary(a => a, a => commonConstraints.AsEnumerable()), nowProvider);

            List<(int, DateTime)> calls = [];

            for (int j = 0; j < 10; j++)
            {
                {
                    using var locker = await distributor
                        .AwaitAnotherAsync(default)
                        .AsTask()
                        .WithinDeadline(TimeSpan.FromMilliseconds(500));

                    calls.Add((locker.Item, nowProvider.Now.UtcDateTime));
                }

                moveTimeForward(timePace);
            }

            var violatedConstraints = calls.GroupBy(c => c.Item1)
                .Select(agentCalls => commonConstraints.FindViolatedConstraints(agentCalls.Select(c => c.Item2)).ToArray())
                .Where(c => c.Any())
                .ToArray();

            Assert.Empty(violatedConstraints);
        }
    }

    [Fact]
    public async Task RateControllerLengthyOperation()
    {
        CancellationToken cancellationToken = default;
        var nowProvider = new ManualChronos(
                relativeSpeed: 0,
                initialTime: new DateTime(2000, 1, 1));


        var rateController = new CompletionRateController([new(TimeSpan.FromSeconds(2), 1)], nowProvider);

        ValueTask<ContinuationToken> secondRequest;
        {
            using var _ = await rateController.WhenAllowed(cancellationToken);

            var longOperation = nowProvider.WhenPasses(TimeSpan.FromSeconds(10));

            secondRequest = rateController.WhenAllowed(cancellationToken);

            Assert.False(secondRequest.IsCompleted);

            nowProvider.MoveTime(TimeSpan.FromSeconds(5));

            Assert.False(secondRequest.IsCompleted);

            nowProvider.MoveTime(TimeSpan.FromSeconds(5.1));

            await longOperation;
        }

        {
            Assert.False(secondRequest.IsCompleted);

            nowProvider.MoveTime(TimeSpan.FromSeconds(1));

            Assert.False(secondRequest.IsCompleted);

            nowProvider.MoveTime(TimeSpan.FromSeconds(1));

            await secondRequest.AsTask().WithinDeadline(TimeSpan.FromMilliseconds(1));

            Assert.True(secondRequest.IsCompleted);

            nowProvider.MoveTime(TimeSpan.FromSeconds(1));

            Assert.True(secondRequest.IsCompleted);
        }
    }
}
