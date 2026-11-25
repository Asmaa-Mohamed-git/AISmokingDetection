namespace AISmokingDetection.Common
{
    /// <summary>Payload failed contract validation (mapped to HTTP 422).</summary>
    public class ValidationFailedException : Exception
    {
        public IReadOnlyList<string> Errors { get; }

        public ValidationFailedException(IEnumerable<string> errors)
            : base("The payload failed validation.")
        {
            Errors = errors.ToList();
        }

        public ValidationFailedException(string error) : this(new[] { error }) { }
    }
}
