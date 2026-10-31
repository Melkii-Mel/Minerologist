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

    public static ResponseSocket CreateSocket(uint seed)
    {
        foreach (var port in Generate(seed))
        {
            Console.WriteLine($"Trying port {port} ...");
            try
            {
                var socket = new ResponseSocket($"@tcp://localhost:{port}");
                Console.WriteLine("Socket established");
                return socket;
            }
            catch (AddressAlreadyInUseException) { }
        }

        throw new Exception();
    }

    public static RequestSocket ConnectToFirstMatchingPort(
        uint seed,
        int timeoutMs = 300,
        int attempts = -1
    )
    {
        foreach (var port in attempts < 1 ? Generate(seed) : Generate(seed).Take(attempts))
        {
            Console.WriteLine($"Trying connecting to port {port} ...");
            var req = new RequestSocket($">tcp://localhost:{port}");

            if (!req.TrySendFrame(TimeSpan.FromMilliseconds(timeoutMs), "minerologist"))
            {
                continue;
            }

            if (req.ReceiveFrameString() != "Ok")
            {
                continue;
            }

            Console.WriteLine("Connected successfully");

            return req;
        }

        throw new Exception();
    }
}
