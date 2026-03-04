namespace EverModern.WheelProtection.DataWorks
{
    /// <summary>
    /// Generates limitedly global unique id of long type based on seed date.
    /// </summary>
    public class IdsGenerator
    {
        readonly DateTime _from;
        long _last;
        readonly object _locker = new object();
        /// <summary>
        /// Initializes a new instance with a custom seed time.
        /// </summary>
        /// <param name="from">The seed time.</param>
        public IdsGenerator(DateTime from)
        {
            _from = from;
        }

        /// <summary>
        /// Initializes a new instance using the current UTC time as seed.
        /// </summary>
        public IdsGenerator()
        {
            _from = DateTime.UtcNow;
        }

        /// <summary>
        /// Generates a unique id based on the seed time.
        /// </summary>
        public long Generate()
        {
            var result = (DateTime.UtcNow - _from).Ticks;

            lock (_locker)
            {
                if (result == _last)
                {
                    result = _last + 1;
                }
                _last = result;
            }
            return result;
        }
    }
}