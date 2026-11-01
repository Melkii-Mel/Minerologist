using System;
using System.Linq;
using System.Text;
using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;

public class MuseumServer : IDisposable
{
    public MuseumRoot Root { get; }
    public RouterSocket Socket { get; }

    public MuseumServer(MuseumRoot root)
    {
        Root = root;
        Socket = SocketProvider.CreateRouter(Config.Instance.ServerConfig.PortSeed);

        Root.Root.Update += HandleMessages;
    }

    private void HandleMessages(float delta)
    {
        var request = new NetMQMessage();

        while (Socket.TryReceiveMultipartMessage(ref request, 2))
        {
            if (request.FrameCount != 2)
            {
                continue;
            }

            var identity = request[0].ToByteArray();
            var json = Encoding.UTF8.GetString(request[1].ToByteArray());

            if (json == "minerologist")
            {
                Send(identity, "Ok");
                continue;
            }

            IpcMessage? message;
            try
            {
                message = JsonConvert.DeserializeObject<IpcMessage>(
                    json,
                    IpcMessage.SerializerSettings
                );
            }
            catch (JsonException)
            {
                Send(identity, $"Invalid message: `{json}`");
                continue;
            }

            if (message is IpcMessageRequestCurrentSkeleton)
            {
                Send(identity, new IpcMessageRespondCurrentSkeleton(Root.State.CurrentSkeleton));
            }
            else if (message is IpcMessageSubmitDiggable diggable)
            {
                Root.DiggablesReceiver.AddDiggable(diggable);
            }
            else if (message is IpcMessageRequestRemainingPieceIds)
            {
                var ids = Root
                    .State.CurrentSkeletonPiecesAmounts.Where(p => p.Value == 0)
                    .Select(p => p.Key)
                    .ToArray();

                Send(identity, new IpcMessageRespondRemainingPieceIds(ids));
            }
            else
            {
                Send(identity, $"Request `{json}` is not supported");
            }
        }
    }

    private void Send(byte[] identity, IpcMessage message)
    {
        Send(identity, JsonConvert.SerializeObject(message, IpcMessage.SerializerSettings));
    }

    private void Send(byte[] identity, string message)
    {
        Socket.SendMoreFrame(identity);
        Socket.SendFrame(message);
    }

    public void Dispose()
    {
        Root.Root.Update -= HandleMessages;
        Socket.Dispose();
    }
}
