using Newtonsoft.Json;

public record IpcMessage
{
    public static JsonSerializerSettings SerializerSettings =>
        new() { TypeNameHandling = TypeNameHandling.Objects };
}

public record IpcMessageRequestRemainingPieceIds : IpcMessage;

public record IpcMessageRequestSkeleton : IpcMessage;

public record IpcMessageSendDiggable(string Type, string Id) : IpcMessage;
