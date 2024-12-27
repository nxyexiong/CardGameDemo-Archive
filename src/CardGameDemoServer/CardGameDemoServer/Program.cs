using System;

namespace CardGameDemoServer
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var server = new GameServer(
                port: 8800,
                isIpv6: false,
                profileIds: ["aaa", "bbb"],
                initNetWorth: 500,
                turnTimeMs: 30 * 1000,
                maxBet: 50,
                deckCount: 2);
            server.Start();

            var cancelled = false;
            while (!cancelled)
                Thread.Sleep(100);
            Console.CancelKeyPress += (_, _) => { cancelled = true; };

            server.Stop();
        }
    }
}