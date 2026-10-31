using System;
using System.Collections.Generic;
using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;

public static class MuseumCommunication
{
    public static string CurrentSkeleton { get; internal set; } = "Мамонт";
    public static HashSet<string> RemainingSkeletonPieces { get; internal set; } = [];
    public static Queue<EntityPrefab> ItemsToSend { get; } = [];

    public static void SendItem(EntityPrefab entityPrefab)
    {
        ItemsToSend.Enqueue(entityPrefab);
    }
}

public class MuseumCommunicator
{
    private readonly RequestSocket _socket;

    public MuseumCommunicator(GameRoot root)
    {
        _socket = SocketProvider.ConnectToFirstMatchingPort(Config.Instance.ServerConfig.PortSeed);
        root.Root.Update += Update;
    }

    private void Update(float delta)
    {
        _socket.SendFrame(ToJson(new IpcMessageRequestSkeleton()));
        MuseumCommunication.CurrentSkeleton = _socket.ReceiveFrameString();

        _socket.SendFrame(ToJson(new IpcMessageRequestRemainingPieceIds()));
        MuseumCommunication.RemainingSkeletonPieces =
        [
            .. FromJson<string[]>(_socket.ReceiveFrameString())!,
        ];

        foreach (var item in MuseumCommunication.ItemsToSend)
        {
            _socket.SendFrame(ToJson(new IpcMessageSendDiggable(item.Type, item.Id)));
            _socket.ReceiveFrameString();
        }
        MuseumCommunication.ItemsToSend.Clear();
    }

    private string ToJson<T>(T message)
        where T : IpcMessage
    {
        return JsonConvert.SerializeObject(message, IpcMessage.SerializerSettings);
    }

    private T? FromJson<T>(string json)
    {
        return JsonConvert.DeserializeObject<T>(json, IpcMessage.SerializerSettings);
    }
}
