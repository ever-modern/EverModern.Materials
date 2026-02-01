namespace EverModern.WheelProtection.DataWorks
{
    public static class IndexCreation
    {
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
