namespace Application.Common.Exceptions;

public sealed class PersistedJsonDeserializationException : Exception
{
    public PersistedJsonDeserializationException(string dataType, string rawJson, Exception innerException)
        : base($"Persisted JSON for {dataType} could not be deserialized.", innerException)
    {
        DataType = dataType;
        RawJson = rawJson;
    }

    public string DataType { get; }
    public string RawJson { get; }
}
