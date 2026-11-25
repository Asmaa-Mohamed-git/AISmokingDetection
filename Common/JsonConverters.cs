using System.Text.Json;
using System.Text.Json.Serialization;

namespace AISmokingDetection.Common
{
    /// <summary>Serializes an enum as its PascalCase name (e.g. "Processing"). Integers are rejected.</summary>
    public class StringEnumConverter<T> : JsonStringEnumConverter<T> where T : struct, Enum
    {
        public StringEnumConverter() : base(namingPolicy: null, allowIntegerValues: false) { }
    }

    /// <summary>Serializes an enum as snake_case (e.g. "matched_candidate"). Integers are rejected.</summary>
    public class SnakeCaseLowerEnumConverter<T> : JsonStringEnumConverter<T> where T : struct, Enum
    {
        public SnakeCaseLowerEnumConverter()
            : base(JsonNamingPolicy.SnakeCaseLower, allowIntegerValues: false) { }
    }
}
