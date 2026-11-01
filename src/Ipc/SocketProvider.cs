using System;
using System.Collections.Generic;
using System.Linq;
using NetMQ;
using NetMQ.Sockets;

public static class SocketProvider
{
    public static IEnumerable<int> Generate(uint seed)
    {
        uint x = seed == 0 ? 1 : seed;
        while (true)
        {
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            yield return 49152 + (int)(x % (65536 - 49152));
        }
    }

    public static RouterSocket CreateRouter(uint seed)
    {
        foreach (var port in Generate(seed))
        {
            Console.WriteLine($"Trying port {port} ...");
            try
            {
                var socket = new RouterSocket($"@tcp://*:{port}");
                Console.WriteLine("Socket established");
                return socket;
            }
            catch (AddressAlreadyInUseException) { }
        }

        throw new Exception();
    }

    public static DealerSocket CreateDealer(uint seed, int timeoutMs = 300, int attempts = -1)
    {
        foreach (var port in attempts < 1 ? Generate(seed) : Generate(seed).Take(attempts))
        {
            var dealer = new DealerSocket();
            dealer.Connect($"tcp://localhost:{port}");

            dealer.SendFrame("minerologist");

            if (
                dealer.TryReceiveFrameString(TimeSpan.FromMilliseconds(timeoutMs), out var response)
                && response == "Ok"
            )
            {
                return dealer;
            }

            dealer.Dispose();
        }

        throw new Exception();
    }
}
