using System.Diagnostics;
using LastLight.Server;

var server = new ServerNetworking(5000);
server.Start();

Console.WriteLine("[Server] Press Enter to stop.");

Globals.Stopwatch.Start();

while (!Console.KeyAvailable || Console.ReadKey(true).Key != ConsoleKey.Enter)
{
    float dt = (float)Globals.Stopwatch.Elapsed.TotalSeconds;
    server.PollEvents();
    server.Update(dt - Globals.LastTime);
    Globals.LastTime = dt;
    Thread.Sleep(5); 
}

server.Stop();
Console.WriteLine("[Server] Stopped.");

namespace LastLight.Server {
    public static class Globals {
        public static Stopwatch Stopwatch = new Stopwatch();
        public static float LastTime = 0f;
    }
}
