using System;
using System.Collections.Generic;
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

    public static ResponseSocket GetFirstFreePort(uint seed)
    {
        foreach (var port in Generate(seed))
        {
            try
            {
                return new ResponseSocket($"@tcp://localhost:{port}");
            }
            catch (AddressAlreadyInUseException) { }
        }

        throw new Exception();
    }

    public static RequestSocket ConnectToFirstMatchingPort(uint seed, int timeoutMs = 300)
    {
        foreach (var port in Generate(seed))
        {
            var req = new RequestSocket($">tcp://localhost:{port}");

            if (!req.TrySendFrame(TimeSpan.FromMilliseconds(timeoutMs), "minerologist"))
            {
                continue;
            }

            if (req.ReceiveFrameString() != "Ok")
            {
                continue;
            }

            return req;
        }

        throw new Exception();
    }
}
