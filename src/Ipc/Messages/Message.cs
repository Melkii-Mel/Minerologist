using Newtonsoft.Json;

public record IcpMessage
{
    public static JsonSerializerSettings SerializerSettings =>
        new() { TypeNameHandling = TypeNameHandling.Auto };
}

public record IcpMessageSendRemainingPieceIds : IcpMessage;

public record IcpMessageAskSkeleton : IcpMessage;

public record IcpMessageSendDiggable(string Type, string Id) : IcpMessage;
