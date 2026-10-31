using System;
using System.Linq;
using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;

public class MuseumServer
{
    public MuseumRoot Root { get; }

    public ResponseSocket Socket { get; }
    public NetMQPoller Poller { get; }

    public MuseumServer(MuseumRoot root)
    {
        Root = root;
        Socket = SocketProvider.CreateSocket(Config.Instance.ServerConfig.PortSeed);
        Socket.ReceiveReady += (sernder, args) =>
        {
            var json = Socket.ReceiveFrameString();
            if (json == "minerologist")
            {
                Console.WriteLine("Received connection request");
                Socket.SendFrame("Ok");
                Console.WriteLine("Responded");
                return;
            }
            var value = JsonConvert.DeserializeObject<IpcMessage>(
                json,
                IpcMessage.SerializerSettings
            );
            if (value is IpcMessageRequestSkeleton askSkeleton)
            {
                Socket.SendFrame(Root.State.CurrentSkeleton);
            }
            else if (value is IpcMessageSendDiggable sendDiggable)
            {
                Root.DiggablesReceiver.AddDiggable(sendDiggable);
                Socket.SendFrame("Ok");
            }
            else if (value is IpcMessageRequestRemainingPieceIds sendRemainingPieceIds)
            {
                Console.WriteLine("This ");

                Socket.SendFrame(
                    JsonConvert.SerializeObject(
                        Root.State.CurrentSkeletonPiecesAmounts.Where(p => p.Value == 0)
                            .Select(p => p.Key),
                        IpcMessage.SerializerSettings
                    )
                );
            }
            else
            {
                Socket.SendFrame($"Request `{json}` is not supported");
            }
        };

        Poller = [Socket];
        Poller.RunAsync();
    }
}
