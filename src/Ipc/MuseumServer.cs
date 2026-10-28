using System.Linq;
using NetMQ;
using Newtonsoft.Json;

public class MuseumServer
{
    public MuseumRoot Root { get; }

    public MuseumServer(MuseumRoot root)
    {
        Root = root;
        using var server = SocketProvider.ConnectToFirstMatchingPort(
            Config.Instance.ServerConfig.PortSeed
        );
        server.ReceiveReady += (sernder, args) =>
        {
            var json = server.ReceiveFrameString();
            if (json == "minerologist")
            {
                server.SendFrame("Ok");
                return;
            }
            var value = JsonConvert.DeserializeObject<IcpMessage>(
                json,
                IcpMessage.SerializerSettings
            );
            if (value is IcpMessageAskSkeleton askSkeleton)
            {
                server.SendFrame(Root.State.CurrentSkeleton);
            }
            else if (value is IcpMessageSendDiggable sendDiggable)
            {
                Root.DiggablesReceiver.AddDiggable(sendDiggable);
                server.SendFrame("Ok");
            }
            else if (value is IcpMessageSendRemainingPieceIds sendRemainingPieceIds)
            {
                server.SendFrame(
                    JsonConvert.SerializeObject(
                        Root.State.CurrentSkeletonPiecesAmounts.Where(p => p.Value == 0)
                            .Select(p => p.Key)
                    )
                );
            }
            else
            {
                server.SendFrame($"Request `{json}` is not supported");
            }
        };
    }
}
