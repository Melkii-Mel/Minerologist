using Newtonsoft.Json;

public record IpcMessage
{
    public static JsonSerializerSettings SerializerSettings =>
        new() { TypeNameHandling = TypeNameHandling.Objects };
}

public record IpcMessageRequestCurrentSkeleton : IpcMessage;

public record IpcMessageRequestRemainingPieceIds : IpcMessage;

public record IpcMessageSubmitDiggable(string Type, string Id) : IpcMessage;

public record IpcMessageRespondCurrentSkeleton(string SkeletonName) : IpcMessage;

public record IpcMessageRespondRemainingPieceIds(string[] Ids) : IpcMessage;
