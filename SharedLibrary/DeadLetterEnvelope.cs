using System;

namespace SharedLibrary.Models;

public class DeadLetterEnvelope<T>
{
    public string ImportId { get; set; }
    public DateTime Timestamp { get; set; }
    public string Reason { get; set; }
    public string EventType { get; set; }
    public T FailedPayload { get; set; }
}
