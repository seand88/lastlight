using System.Diagnostics;
using LastLight.Server;

var server = new ServerNetworking(5000);
server.Start();

Console.WriteLine("[Server] Running... (Press Ctrl+C to stop)");

bool isRunning = true;
Console.CancelKeyPress += (s, e) => { e.Cancel = true; isRunning = false; };
AppDomain.CurrentDomain.ProcessExit += (s, e) => isRunning = false;

Globals.Stopwatch.Start();

while (isRunning)
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
