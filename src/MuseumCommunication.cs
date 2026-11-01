using System;
using System.Collections.Generic;
using System.Text;
using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;

public static class MuseumCommunication
{
    public static string CurrentSkeleton { get; internal set; } = null!;
    public static HashSet<string> RemainingSkeletonPieces { get; internal set; } = [];
    public static Queue<EntityPrefab> ItemsToSend { get; } = [];

    public static void SendItem(EntityPrefab entityPrefab)
    {
        ItemsToSend.Enqueue(entityPrefab);
    }
}

public class MuseumCommunicator
{
    private readonly DealerSocket _socket;
    private const float DELAY = 0.1f;
    private float _timer = 0;

    public MuseumCommunicator(GameRoot root)
    {
        _socket = SocketProvider.CreateDealer(Config.Instance.ServerConfig.PortSeed);
        root.Root.Update += Update;

        PollSocket(false);
        ReceiveOne(true);
        ReceiveOne(true);
    }

    private void Update(float delta)
    {
        _timer += delta;
        if (_timer < DELAY)
        {
            return;
        }
        _timer -= DELAY;
        PollSocket();
    }

    private void PollSocket(bool receive = true)
    {
        _socket.SendFrame(ToJson(new IpcMessageRequestCurrentSkeleton()));
        _socket.SendFrame(ToJson(new IpcMessageRequestRemainingPieceIds()));

        foreach (var item in MuseumCommunication.ItemsToSend)
        {
            _socket.SendFrame(ToJson(new IpcMessageSubmitDiggable(item.Type, item.Id)));
        }
        MuseumCommunication.ItemsToSend.Clear();

        if (receive)
        {
            while (ReceiveOne()) { }
        }
    }

    private bool ReceiveOne(bool blocking = false)
    {
        var response = new NetMQMessage();

        Func<bool> receive = blocking
            ? () => _socket.TryReceiveMultipartMessage(TimeSpan.FromDays(365), ref response, 1)
            : () => _socket.TryReceiveMultipartMessage(ref response, 1);

        var received = receive();

        if (received)
        {
            if (response.FrameCount != 1)
            {
                return true;
            }

            var json = Encoding.UTF8.GetString(response.First.ToByteArray());
            var message = FromJson<IpcMessage>(json);

            if (message is IpcMessageRespondCurrentSkeleton skeleton)
            {
                MuseumCommunication.CurrentSkeleton = skeleton.SkeletonName;
            }
            if (message is IpcMessageRespondRemainingPieceIds pieceIds)
            {
                MuseumCommunication.RemainingSkeletonPieces = [.. pieceIds.Ids];
            }
        }

        return received;
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
