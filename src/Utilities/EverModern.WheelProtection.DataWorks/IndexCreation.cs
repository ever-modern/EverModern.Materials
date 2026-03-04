namespace EverModern.WheelProtection.DataWorks
{
    /// <summary>
    /// Provides helpers for generating search index fragments.
    /// </summary>
    public static class IndexCreation
    {
        /// <summary>
        /// Enumerates all contiguous substrings of the input.
        /// </summary>
        /// <param name="inputString">The input string.</param>
        public static IEnumerable<string> GetAllChunks(this string inputString)
        {
            var len = inputString.Length;

            for (int chunkLength = 1; chunkLength <= len; chunkLength++)
            {
                for (int chunkStartPosition = 0; chunkStartPosition <= len - chunkLength; chunkStartPosition++)
                {
                    var chunk = inputString[chunkStartPosition..(chunkStartPosition + chunkLength)];
                    yield return new string(chunk);
                }
            }
        }

    }
}
