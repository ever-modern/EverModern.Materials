using System.Diagnostics;

namespace EverModern.Chronos
{
    /// <summary>
    /// Chronos implementation with manually controlled time flow.
    /// </summary>
    public class ManualChronos : IChronos, IChronosControll
    {
        void Log(string message)
        {
#if DEBUG
            Console.WriteLine($"{nameof(ManualChronos)}#{GetHashCode()} {Now}: {message}");
#endif
        }

        static long _insignificantTicks = TimeSpan.FromMilliseconds(15).Ticks;
        static ManualChronos()
        {
            Task.Run(async () =>
            {
                var results = new List<TimeSpan>();
                for (int i = 0; i < 50; i++)
                {
                    var stopwatch = Stopwatch.StartNew();

                    await Task.Delay(1);

                    results.Add(stopwatch.Elapsed);
                }

                var average = (long)results.Select(r => r.Ticks).Average();

                if (average > TimeSpan.FromMilliseconds(1).Ticks)
                {
                    _insignificantTicks = average;
                }
                else
                {
                    _insignificantTicks = TimeSpan.FromMilliseconds(1).Ticks;
                }
            });
        }

        decimal _relativeSpeed = 0;
        readonly List<ChronosCallback> _onTimeChanged
            = new List<ChronosCallback>();

        Func<DateTimeOffset> _getNow;

        CancellationTokenSource _timeFlowStopping;

        TimeSpan _realTimeOffset;
        DateTimeOffset _baseTime;

        void AttuneTimeFlow()
        {
            if (_relativeSpeed == 0)
            {
                return;
            }

            var oldTimeFlowStopping = _timeFlowStopping;

            var shortestCallback = _onTimeChanged
                .OrderBy(tc => tc.AssociatedTime)
                .FirstOrDefault();

            var timePace = shortestCallback != null ?
                TimeSpan.FromTicks((long)((shortestCallback.AssociatedTime - Now).Ticks / 10 / _relativeSpeed)) :
                TimeSpan.Zero;

            if (timePace == TimeSpan.Zero)
            {
                return;
            }

            var newTimeFlowStopping = new CancellationTokenSource();

            Action newTracker = () =>
                Task.Run(async () =>
                {
                    while (newTimeFlowStopping.IsCancellationRequested == false)
                    {
                        await Task.Delay(timePace, newTimeFlowStopping.Token);
                        CheckCallbacks();

                        if (_onTimeChanged.Count == 0)
                        {
                            break;
                        }
                    }
                }
             );

            if (oldTimeFlowStopping != null)
            {
                oldTimeFlowStopping.Token.Register(newTracker);
                oldTimeFlowStopping?.Cancel();
            }
            else
            {
                newTracker();
            }

            _timeFlowStopping = newTimeFlowStopping;

        }

        /// <summary>
        /// Initializes a new instance with default speed.
        /// </summary>
        public ManualChronos() : this(0)
        {
        }

        /// <summary>
        /// Initializes a new instance with a custom time source.
        /// </summary>
        /// <param name="realTimeSource">The real time source.</param>
        public ManualChronos(Func<DateTimeOffset> realTimeSource)
            : this(realTimeSource(), 0, realTimeSource)
        {
        }

        /// <summary>
        /// Initializes a new instance with custom parameters.
        /// </summary>
        /// <param name="initialTime">The initial time.</param>
        /// <param name="relativeSpeed">The relative speed multiplier.</param>
        /// <param name="realTimeSource">The real time source.</param>
        public ManualChronos(
            DateTimeOffset initialTime,
            decimal relativeSpeed,
            Func<DateTimeOffset> realTimeSource)
        {
            var realNow = realTimeSource();
            _relativeSpeed = relativeSpeed;
            _baseTime = realNow;
            _realTimeOffset = initialTime - _baseTime;
            _getNow = realTimeSource;
        }

        static Func<DateTimeOffset> CreateStopwatchSource()
        {
            var baseTime = DateTimeOffset.Now;
            var stopwatch = Stopwatch.StartNew();

            return () => baseTime + stopwatch.Elapsed;
        }

        /// <summary>
        /// Initializes a new instance with a relative speed.
        /// </summary>
        /// <param name="relativeSpeed">The relative speed multiplier.</param>
        public ManualChronos(decimal relativeSpeed)
            : this(DateTimeOffset.Now, relativeSpeed, CreateStopwatchSource())
        {
        }

        /// <summary>
        /// Initializes a new instance with an initial time and speed.
        /// </summary>
        /// <param name="initialTime">The initial time.</param>
        /// <param name="relativeSpeed">The relative speed multiplier.</param>
        public ManualChronos(DateTimeOffset initialTime, decimal relativeSpeed)
            : this(initialTime, relativeSpeed, CreateStopwatchSource())
        {
        }

        /// <inheritdoc />
        public DateTimeOffset Now
        {
            get
            {
                var timezoneOffset = _baseTime.Offset;
                var realNow = _getNow();

                var passedTime = TimeSpan.FromTicks((long)((realNow - _baseTime).Ticks * _relativeSpeed));

                try
                {
                    var result = _baseTime + passedTime + _realTimeOffset;

                    return result;
                }
                catch (Exception ex)
                {
                    throw;
                }
            }
        }

        /// <inheritdoc />
        public Task WhenComes(DateTimeOffset targetTime, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            lock (this)
            {
                var difference = (targetTime - Now);
                if (_relativeSpeed != 0 && difference.Ticks / _relativeSpeed <= _insignificantTicks)
                {
                    return Task.CompletedTask;
                }

                var awaiting = new TaskCompletionSource<bool>();

                Subscribe(targetTime, () =>
                {
                    var timeReached = Now >= targetTime;
#if DEBUG
                    if (timeReached)
                    {
                        Log($"Time {targetTime} reached. Awaiting task will be completed.");
                    }
                    else 
                    {
                        Log($"Time {targetTime} isn't reached yet. Task completion will happen later.");
                    }
#endif
                    if (timeReached)
                    {
                        awaiting.SetResult(true);
                        return true;
                    }

                    return false;
                });

                return awaiting.Task;
            }
        }

        /// <inheritdoc />
        public Task WhenPasses(TimeSpan time, CancellationToken cancellationToken = default)
        {
            if (_relativeSpeed != 0 && time.Ticks / _relativeSpeed < _insignificantTicks)
            {
                return Task.CompletedTask;
            }
            return WhenComes(Now + time, cancellationToken); ;
        }

        void CheckCallbacks()
        {
            lock (this)
            {
                for (int i = 0; i < _onTimeChanged.Count; i++)
                {
                    var c = _onTimeChanged[i];
                    var fired = c.Callback();
                    Log($"Callback awaiting {c.AssociatedTime} is checked");
                    if (fired)
                    {
                        Log($"Callback awaiting {c.AssociatedTime} has been fired and removed from the queue.");
                        _onTimeChanged.RemoveAt(i--);
                    }
                }
            }
        }

        void Subscribe(DateTimeOffset targetTime, Func<bool> callback)
        {
            _onTimeChanged.Add(new ChronosCallback(targetTime, callback));
            AttuneTimeFlow();
        }

        /// <inheritdoc />
        public void SetTime(DateTimeOffset newNow)
        {
            _baseTime = newNow;
            _realTimeOffset = default;


            if (_relativeSpeed != 0)
            {
                AttuneTimeFlow();
            }
            else
            {
                CheckCallbacks();
            }

        }

        /// <inheritdoc />
        public void MoveTime(TimeSpan moveForward)
        {
            _realTimeOffset += moveForward;
            if (_relativeSpeed != 0)
            {
                AttuneTimeFlow();
            }
            else
            {
                CheckCallbacks();
            }
        }

        /// <inheritdoc />
        public Task WhenComes(DateTime targetTimeUtc, CancellationToken cancellationToken = default)
        {
            var resultTime = new DateTimeOffset(targetTimeUtc);
            resultTime = resultTime - resultTime.Offset;
            return WhenComes(resultTime, cancellationToken);
        }
    }
}
